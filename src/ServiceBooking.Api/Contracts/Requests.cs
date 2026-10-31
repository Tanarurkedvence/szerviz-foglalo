using ServiceBooking.Api.Domain;

namespace ServiceBooking.Api.Contracts;

/// <summary>
/// The booking form. Strings are nullable on purpose: BookingService validates them
/// and returns every problem at once, in Hungarian.
/// </summary>
public class CreateBookingRequest
{
    public string? CustomerName { get; set; }
    public string? CustomerEmail { get; set; }
    public string? CustomerPhone { get; set; }
    public string? LicensePlate { get; set; }
    public int ServiceTypeId { get; set; }

    /// <summary>Local workshop time, e.g. "2026-11-03T09:00:00".</summary>
    public DateTime StartTime { get; set; }

    public string? Note { get; set; }
}

/// <summary>The customer proves the booking is theirs with the e-mail address they booked with.</summary>
public class CancelBookingRequest
{
    public string? Email { get; set; }
}

public class ChangeStatusRequest
{
    public BookingStatus Status { get; set; }
}
