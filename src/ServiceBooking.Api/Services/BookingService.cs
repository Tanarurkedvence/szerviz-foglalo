using System.Text.RegularExpressions;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;
using ServiceBooking.Api.Contracts;
using ServiceBooking.Api.Data;
using ServiceBooking.Api.Domain;

namespace ServiceBooking.Api.Services;

/// <summary>
/// All booking rules live here, so they can be unit tested without HTTP.
/// </summary>
public class BookingService
{
    private static readonly Regex EmailPattern = new(@"^[^@\s]+@[^@\s]+\.[^@\s]+$", RegexOptions.Compiled);
    private static readonly Regex PhonePattern = new(@"^\+?[0-9 ()/\-]{6,20}$", RegexOptions.Compiled);

    // Which status can follow which. Anything not listed is rejected.
    private static readonly Dictionary<BookingStatus, BookingStatus[]> AllowedTransitions = new()
    {
        [BookingStatus.Booked] = new[] { BookingStatus.InProgress, BookingStatus.Cancelled },
        [BookingStatus.InProgress] = new[] { BookingStatus.Completed },
        [BookingStatus.Completed] = Array.Empty<BookingStatus>(),
        [BookingStatus.Cancelled] = Array.Empty<BookingStatus>(),
    };

    private readonly AppDbContext _db;
    private readonly IClock _clock;
    private readonly WorkshopOptions _options;

    public BookingService(AppDbContext db, IClock clock, IOptions<WorkshopOptions> options)
    {
        _db = db;
        _clock = clock;
        _options = options.Value;
    }

    /// <summary>Start times on the given day when the service still fits on a free lift.</summary>
    public async Task<IReadOnlyList<DateTime>> GetAvailableSlotsAsync(int serviceTypeId, DateTime date, CancellationToken ct = default)
    {
        var serviceType = await FindServiceTypeAsync(serviceTypeId, ct);

        var day = date.Date;
        var opening = day.AddHours(_options.OpeningHour);
        var closing = day.AddHours(_options.ClosingHour);

        // One query for the whole day, then the slots are checked in memory.
        var bookingsThatDay = await ActiveBookingsBetween(opening, closing).ToListAsync(ct);

        var slots = new List<DateTime>();
        for (var start = opening; start.AddMinutes(serviceType.DurationMinutes) <= closing; start = start.AddMinutes(_options.SlotMinutes))
        {
            var end = start.AddMinutes(serviceType.DurationMinutes);

            if (CheckTimeRules(start, end) is not null)
            {
                continue;
            }

            var overlapping = bookingsThatDay.Where(b => Overlaps(b, start, end));
            if (FindFreeBay(overlapping) is not null)
            {
                slots.Add(start);
            }
        }

        return slots;
    }

    public async Task<Booking> CreateAsync(CreateBookingRequest request, CancellationToken ct = default)
    {
        var errors = ValidateCustomerData(request);
        if (errors.Count > 0)
        {
            throw new BookingException(BookingError.Validation, errors);
        }

        var serviceType = await FindServiceTypeAsync(request.ServiceTypeId, ct);
        var start = request.StartTime;
        var end = start.AddMinutes(serviceType.DurationMinutes);

        var timeError = CheckTimeRules(start, end);
        if (timeError is not null)
        {
            throw new BookingException(BookingError.Validation, timeError);
        }

        var plate = LicensePlateRules.Normalize(request.LicensePlate);
        var overlapping = await ActiveBookingsBetween(start, end).ToListAsync(ct);

        if (overlapping.Any(b => b.LicensePlate == plate))
        {
            throw new BookingException(BookingError.Conflict, "Erre a rendszámra ebben az időpontban már van foglalás.");
        }

        var bay = FindFreeBay(overlapping)
            ?? throw new BookingException(BookingError.Conflict, "Ez az időpont közben betelt, kérlek válassz másikat.");

        var booking = new Booking
        {
            CustomerName = request.CustomerName!.Trim(),
            CustomerEmail = NormalizeEmail(request.CustomerEmail),
            CustomerPhone = request.CustomerPhone!.Trim(),
            LicensePlate = plate,
            ServiceTypeId = serviceType.Id,
            ServiceType = serviceType,
            StartTime = start,
            EndTime = end,
            BayNumber = bay,
            Status = BookingStatus.Booked,
            Note = string.IsNullOrWhiteSpace(request.Note) ? null : request.Note.Trim(),
            CreatedAt = _clock.Now
        };

        _db.Bookings.Add(booking);
        await _db.SaveChangesAsync(ct);
        return booking;
    }

    /// <summary>Customer self-service cancellation.</summary>
    public async Task<Booking> CancelAsync(int id, string? email, CancellationToken ct = default)
    {
        var booking = await _db.Bookings.Include(b => b.ServiceType).FirstOrDefaultAsync(b => b.Id == id, ct);

        // Same answer for "no such id" and "wrong e-mail", so nobody can probe ids.
        if (booking is null || booking.CustomerEmail != NormalizeEmail(email))
        {
            throw new BookingException(BookingError.NotFound, "Nincs ilyen foglalás ezzel az e-mail címmel.");
        }

        if (booking.Status != BookingStatus.Booked)
        {
            throw new BookingException(BookingError.InvalidState, "Ez a foglalás már nem mondható le.");
        }

        if (booking.StartTime < _clock.Now.AddHours(_options.FreeCancellationHours))
        {
            throw new BookingException(
                BookingError.InvalidState,
                $"Kezdés előtt {_options.FreeCancellationHours} órán belül már csak telefonon lehet lemondani.");
        }

        booking.Status = BookingStatus.Cancelled;
        await _db.SaveChangesAsync(ct);
        return booking;
    }

    /// <summary>Workshop staff moves a booking through its life cycle.</summary>
    public async Task<Booking> ChangeStatusAsync(int id, BookingStatus newStatus, CancellationToken ct = default)
    {
        var booking = await GetAsync(id, ct);

        if (!AllowedTransitions[booking.Status].Contains(newStatus))
        {
            throw new BookingException(
                BookingError.InvalidState,
                $"A foglalás nem tehető „{StatusName(booking.Status)}” állapotból „{StatusName(newStatus)}” állapotba.");
        }

        booking.Status = newStatus;
        await _db.SaveChangesAsync(ct);
        return booking;
    }

    public async Task<Booking> GetAsync(int id, CancellationToken ct = default)
    {
        return await _db.Bookings.Include(b => b.ServiceType).FirstOrDefaultAsync(b => b.Id == id, ct)
            ?? throw new BookingException(BookingError.NotFound, "A foglalás nem található.");
    }

    public async Task<IReadOnlyList<Booking>> ListAsync(DateTime? date, CancellationToken ct = default)
    {
        IQueryable<Booking> query = _db.Bookings.Include(b => b.ServiceType);

        if (date is not null)
        {
            var from = date.Value.Date;
            var to = from.AddDays(1);
            query = query.Where(b => b.StartTime >= from && b.StartTime < to);
        }

        return await query.OrderBy(b => b.StartTime).ThenBy(b => b.BayNumber).ToListAsync(ct);
    }

    // ---- helpers ----

    private List<string> ValidateCustomerData(CreateBookingRequest request)
    {
        var errors = new List<string>();

        if (string.IsNullOrWhiteSpace(request.CustomerName))
        {
            errors.Add("A név megadása kötelező.");
        }
        else if (request.CustomerName.Trim().Length > 100)
        {
            errors.Add("A név legfeljebb 100 karakter lehet.");
        }

        if (string.IsNullOrWhiteSpace(request.CustomerEmail)
            || request.CustomerEmail.Length > 200
            || !EmailPattern.IsMatch(request.CustomerEmail.Trim()))
        {
            errors.Add("Érvénytelen e-mail cím.");
        }

        if (string.IsNullOrWhiteSpace(request.CustomerPhone) || !PhonePattern.IsMatch(request.CustomerPhone.Trim()))
        {
            errors.Add("Érvénytelen telefonszám.");
        }

        if (!LicensePlateRules.IsValid(request.LicensePlate))
        {
            errors.Add("Érvénytelen rendszám (pl. ABC-123 vagy AA AA-123).");
        }

        if (request.Note is not null && request.Note.Length > 500)
        {
            errors.Add("A megjegyzés legfeljebb 500 karakter lehet.");
        }

        return errors;
    }

    /// <summary>Returns null if the time is fine, otherwise the reason it is not.</summary>
    private string? CheckTimeRules(DateTime start, DateTime end)
    {
        var now = _clock.Now;

        if (start.TimeOfDay.Ticks % TimeSpan.FromMinutes(_options.SlotMinutes).Ticks != 0)
        {
            return $"Csak {_options.SlotMinutes} perces lépésközzel lehet időpontot választani.";
        }

        if (start.DayOfWeek is DayOfWeek.Saturday or DayOfWeek.Sunday)
        {
            return "Hétvégén zárva vagyunk.";
        }

        var opening = start.Date.AddHours(_options.OpeningHour);
        var closing = start.Date.AddHours(_options.ClosingHour);
        if (start < opening || end > closing)
        {
            return $"Nyitvatartás: {_options.OpeningHour}:00–{_options.ClosingHour}:00, és a munkának zárásig be kell fejeződnie.";
        }

        if (start < now.AddHours(_options.MinHoursInAdvance))
        {
            return $"Legalább {_options.MinHoursInAdvance} órával előre kell foglalni.";
        }

        if (start.Date > now.Date.AddDays(_options.MaxDaysInAdvance))
        {
            return $"Legfeljebb {_options.MaxDaysInAdvance} nappal előre lehet foglalni.";
        }

        return null;
    }

    private async Task<ServiceType> FindServiceTypeAsync(int id, CancellationToken ct)
    {
        return await _db.ServiceTypes.FirstOrDefaultAsync(s => s.Id == id, ct)
            ?? throw new BookingException(BookingError.Validation, "Ismeretlen szolgáltatás.");
    }

    /// <summary>Bookings that are not cancelled and overlap [from, to).</summary>
    private IQueryable<Booking> ActiveBookingsBetween(DateTime from, DateTime to)
    {
        return _db.Bookings.Where(b => b.Status != BookingStatus.Cancelled && b.StartTime < to && b.EndTime > from);
    }

    /// <summary>
    /// Two intervals overlap when each starts before the other ends.
    /// 09:00-10:00 and 10:00-11:00 do NOT overlap (back-to-back is fine).
    /// </summary>
    private static bool Overlaps(Booking booking, DateTime start, DateTime end)
    {
        return booking.StartTime < end && booking.EndTime > start;
    }

    /// <summary>The lowest-numbered lift that none of the overlapping bookings uses.</summary>
    private int? FindFreeBay(IEnumerable<Booking> overlapping)
    {
        var busyBays = overlapping.Select(b => b.BayNumber).ToHashSet();

        for (var bay = 1; bay <= _options.BayCount; bay++)
        {
            if (!busyBays.Contains(bay))
            {
                return bay;
            }
        }

        return null;
    }

    private static string NormalizeEmail(string? email) => (email ?? "").Trim().ToLowerInvariant();

    private static string StatusName(BookingStatus status) => status switch
    {
        BookingStatus.Booked => "foglalva",
        BookingStatus.InProgress => "folyamatban",
        BookingStatus.Completed => "kész",
        BookingStatus.Cancelled => "lemondva",
        _ => status.ToString()
    };
}
