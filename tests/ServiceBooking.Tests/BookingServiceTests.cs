using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;
using ServiceBooking.Api.Data;
using ServiceBooking.Api.Domain;
using ServiceBooking.Api.Services;
using ServiceBooking.Tests.Helpers;
using static ServiceBooking.Tests.Helpers.TestData;

namespace ServiceBooking.Tests;

/// <summary>
/// Tests for the business rules. Each test gets its own fresh in-memory SQLite database
/// (xUnit creates a new instance of this class for every test).
/// The workshop has 2 lifts, is open 08:00-17:00 on weekdays, and "now" is Monday 10:00.
/// </summary>
public class BookingServiceTests : IDisposable
{
    private readonly SqliteConnection _connection;
    private readonly AppDbContext _db;
    private readonly FakeClock _clock = new(Now);
    private readonly BookingService _service;
    private readonly int _oilChange;   // 60 minutes
    private readonly int _brakes;      // 90 minutes

    public BookingServiceTests()
    {
        // An in-memory SQLite database lives as long as its connection is open.
        _connection = new SqliteConnection("DataSource=:memory:");
        _connection.Open();

        var options = new DbContextOptionsBuilder<AppDbContext>().UseSqlite(_connection).Options;
        _db = new AppDbContext(options);
        _db.Database.EnsureCreated();
        SeedData.Seed(_db);

        _service = new BookingService(_db, _clock, Options.Create(new WorkshopOptions()));
        _oilChange = _db.ServiceTypes.Single(s => s.Name == "Olajcsere").Id;
        _brakes = _db.ServiceTypes.Single(s => s.Name == "Fékbetét csere").Id;
    }

    public void Dispose()
    {
        _db.Dispose();
        _connection.Dispose();
    }

    // ---------- Creating bookings ----------

    [Fact]
    public async Task Create_ValidRequest_SavesBookingOnFirstLift()
    {
        var booking = await _service.CreateAsync(BookingRequest(Tuesday.AddHours(9), _oilChange, plate: "abc 123"));

        Assert.True(booking.Id > 0);
        Assert.Equal(Tuesday.AddHours(10), booking.EndTime); // oil change takes 60 minutes
        Assert.Equal(1, booking.BayNumber);
        Assert.Equal("ABC123", booking.LicensePlate);
        Assert.Equal(BookingStatus.Booked, booking.Status);
        Assert.Equal(Now, booking.CreatedAt);
    }

    [Fact]
    public async Task Create_OverlappingBooking_GoesOnSecondLift()
    {
        await _service.CreateAsync(BookingRequest(Tuesday.AddHours(9), _oilChange, plate: "AAA-111"));

        var second = await _service.CreateAsync(BookingRequest(Tuesday.AddHours(9).AddMinutes(30), _oilChange, plate: "BBB-222"));

        Assert.Equal(2, second.BayNumber);
    }

    [Fact]
    public async Task Create_WhenBothLiftsAreBusy_ThrowsConflict()
    {
        await _service.CreateAsync(BookingRequest(Tuesday.AddHours(9), _oilChange, plate: "AAA-111"));
        await _service.CreateAsync(BookingRequest(Tuesday.AddHours(9), _brakes, plate: "BBB-222"));

        var ex = await Assert.ThrowsAsync<BookingException>(
            () => _service.CreateAsync(BookingRequest(Tuesday.AddHours(9).AddMinutes(30), _oilChange, plate: "CCC-333")));

        Assert.Equal(BookingError.Conflict, ex.Error);
    }

    [Fact]
    public async Task Create_BackToBackBookings_DoNotConflict()
    {
        // Boundary value: 09:00-10:00 is full on both lifts, 10:00 is free again.
        await _service.CreateAsync(BookingRequest(Tuesday.AddHours(9), _oilChange, plate: "AAA-111"));
        await _service.CreateAsync(BookingRequest(Tuesday.AddHours(9), _oilChange, plate: "BBB-222"));

        var booking = await _service.CreateAsync(BookingRequest(Tuesday.AddHours(10), _oilChange, plate: "CCC-333"));

        Assert.Equal(1, booking.BayNumber);
    }

    [Fact]
    public async Task Create_SamePlateAtOverlappingTime_ThrowsConflict()
    {
        await _service.CreateAsync(BookingRequest(Tuesday.AddHours(9), _oilChange, plate: "ABC-123"));

        var ex = await Assert.ThrowsAsync<BookingException>(
            () => _service.CreateAsync(BookingRequest(Tuesday.AddHours(9).AddMinutes(30), _oilChange, plate: "abc123")));

        Assert.Equal(BookingError.Conflict, ex.Error);
    }

    [Theory]
    [InlineData(2026, 11, 7, 9, 0)]    // Saturday
    [InlineData(2026, 11, 8, 9, 0)]    // Sunday
    [InlineData(2026, 11, 3, 7, 45)]   // before opening
    [InlineData(2026, 11, 3, 16, 15)]  // a 60-minute job would end after 17:00
    [InlineData(2026, 11, 3, 9, 10)]   // not on a 15-minute step
    [InlineData(2026, 11, 2, 11, 45)]  // less than 2 hours from "now" (10:00)
    [InlineData(2027, 1, 4, 9, 0)]     // more than 60 days ahead
    public async Task Create_InvalidTime_ThrowsValidation(int year, int month, int day, int hour, int minute)
    {
        var start = new DateTime(year, month, day, hour, minute, 0);

        var ex = await Assert.ThrowsAsync<BookingException>(
            () => _service.CreateAsync(BookingRequest(start, _oilChange)));

        Assert.Equal(BookingError.Validation, ex.Error);
    }

    [Fact]
    public async Task Create_ExactlyTwoHoursAhead_IsAllowed()
    {
        // Boundary value for the "at least 2 hours ahead" rule.
        var booking = await _service.CreateAsync(BookingRequest(Now.AddHours(2), _oilChange));

        Assert.Equal(Now.AddHours(2), booking.StartTime);
    }

    [Fact]
    public async Task Create_EndingExactlyAtClosing_IsAllowed()
    {
        // Boundary value: 16:00 + 60 minutes = 17:00, still OK.
        var booking = await _service.CreateAsync(BookingRequest(Tuesday.AddHours(16), _oilChange));

        Assert.Equal(Tuesday.AddHours(17), booking.EndTime);
    }

    [Fact]
    public async Task Create_InvalidCustomerData_ReturnsEveryErrorAtOnce()
    {
        var request = BookingRequest(Tuesday.AddHours(9), _oilChange, plate: "AB-12", email: "nem-email");
        request.CustomerName = "   ";

        var ex = await Assert.ThrowsAsync<BookingException>(() => _service.CreateAsync(request));

        Assert.Equal(BookingError.Validation, ex.Error);
        Assert.Equal(3, ex.Messages.Count); // name + e-mail + plate
    }

    [Fact]
    public async Task Create_UnknownService_ThrowsValidation()
    {
        var ex = await Assert.ThrowsAsync<BookingException>(
            () => _service.CreateAsync(BookingRequest(Tuesday.AddHours(9), serviceTypeId: 999)));

        Assert.Equal(BookingError.Validation, ex.Error);
    }

    // ---------- Cancelling ----------

    [Fact]
    public async Task Cancel_MoreThan24HoursBefore_CancelsAndFreesTheLift()
    {
        // Tuesday 14:00 is 28 hours after "now".
        var first = await _service.CreateAsync(BookingRequest(Tuesday.AddHours(14), _oilChange, plate: "AAA-111"));
        await _service.CreateAsync(BookingRequest(Tuesday.AddHours(14), _oilChange, plate: "BBB-222"));

        var cancelled = await _service.CancelAsync(first.Id, " TESZT.ELEK@example.com ");
        var newBooking = await _service.CreateAsync(BookingRequest(Tuesday.AddHours(14), _oilChange, plate: "CCC-333"));

        Assert.Equal(BookingStatus.Cancelled, cancelled.Status);
        Assert.Equal(first.BayNumber, newBooking.BayNumber);
    }

    [Fact]
    public async Task Cancel_Within24Hours_ThrowsInvalidState()
    {
        // Tuesday 09:00 is only 23 hours after "now".
        var booking = await _service.CreateAsync(BookingRequest(Tuesday.AddHours(9), _oilChange));

        var ex = await Assert.ThrowsAsync<BookingException>(
            () => _service.CancelAsync(booking.Id, "teszt.elek@example.com"));

        Assert.Equal(BookingError.InvalidState, ex.Error);
    }

    [Fact]
    public async Task Cancel_WithSomeoneElsesEmail_ThrowsNotFound()
    {
        var booking = await _service.CreateAsync(BookingRequest(Tuesday.AddHours(14), _oilChange));

        var ex = await Assert.ThrowsAsync<BookingException>(
            () => _service.CancelAsync(booking.Id, "valaki.mas@example.com"));

        Assert.Equal(BookingError.NotFound, ex.Error);
    }

    [Fact]
    public async Task Cancel_Twice_ThrowsInvalidState()
    {
        var booking = await _service.CreateAsync(BookingRequest(Tuesday.AddHours(14), _oilChange));
        await _service.CancelAsync(booking.Id, "teszt.elek@example.com");

        var ex = await Assert.ThrowsAsync<BookingException>(
            () => _service.CancelAsync(booking.Id, "teszt.elek@example.com"));

        Assert.Equal(BookingError.InvalidState, ex.Error);
    }

    // ---------- Status changes ----------

    [Fact]
    public async Task ChangeStatus_FollowsTheWorkflow()
    {
        var booking = await _service.CreateAsync(BookingRequest(Tuesday.AddHours(9), _oilChange));

        await _service.ChangeStatusAsync(booking.Id, BookingStatus.InProgress);
        var done = await _service.ChangeStatusAsync(booking.Id, BookingStatus.Completed);

        Assert.Equal(BookingStatus.Completed, done.Status);
    }

    [Theory]
    [InlineData(BookingStatus.Completed)] // cannot skip "InProgress"
    [InlineData(BookingStatus.Booked)]    // already booked
    public async Task ChangeStatus_InvalidTransition_ThrowsInvalidState(BookingStatus target)
    {
        var booking = await _service.CreateAsync(BookingRequest(Tuesday.AddHours(9), _oilChange));

        var ex = await Assert.ThrowsAsync<BookingException>(() => _service.ChangeStatusAsync(booking.Id, target));

        Assert.Equal(BookingError.InvalidState, ex.Error);
    }

    [Fact]
    public async Task ChangeStatus_UnknownBooking_ThrowsNotFound()
    {
        var ex = await Assert.ThrowsAsync<BookingException>(
            () => _service.ChangeStatusAsync(12345, BookingStatus.InProgress));

        Assert.Equal(BookingError.NotFound, ex.Error);
    }

    // ---------- Free time slots ----------

    [Fact]
    public async Task GetAvailableSlots_EmptyDay_OffersEveryQuarterHourThatFits()
    {
        var slots = await _service.GetAvailableSlotsAsync(_oilChange, Tuesday);

        Assert.Equal(Tuesday.AddHours(8), slots.First());
        Assert.Equal(Tuesday.AddHours(16), slots.Last()); // last start that still ends by 17:00
        Assert.Equal(33, slots.Count);                    // 08:00, 08:15 ... 16:00
    }

    [Fact]
    public async Task GetAvailableSlots_SkipsTimesWhenBothLiftsAreBusy()
    {
        await _service.CreateAsync(BookingRequest(Tuesday.AddHours(10), _oilChange, plate: "AAA-111"));
        await _service.CreateAsync(BookingRequest(Tuesday.AddHours(10), _oilChange, plate: "BBB-222"));

        var slots = await _service.GetAvailableSlotsAsync(_oilChange, Tuesday);

        Assert.Contains(Tuesday.AddHours(9), slots);                       // 09:00-10:00 ends right when they start
        Assert.DoesNotContain(Tuesday.AddHours(9).AddMinutes(15), slots);  // 09:15-10:15 overlaps
        Assert.DoesNotContain(Tuesday.AddHours(10).AddMinutes(45), slots); // 10:45 is during their work
        Assert.Contains(Tuesday.AddHours(11), slots);                      // free again from 11:00
    }

    [Fact]
    public async Task GetAvailableSlots_OnWeekend_ReturnsNothing()
    {
        var saturday = new DateTime(2026, 11, 7);

        var slots = await _service.GetAvailableSlotsAsync(_oilChange, saturday);

        Assert.Empty(slots);
    }

    [Fact]
    public async Task GetAvailableSlots_Today_StartsTwoHoursFromNow()
    {
        var slots = await _service.GetAvailableSlotsAsync(_oilChange, Now.Date);

        Assert.Equal(Now.AddHours(2), slots.First());
    }
}
