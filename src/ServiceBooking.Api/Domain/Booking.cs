namespace ServiceBooking.Api.Domain;

/// <summary>A customer's appointment on one of the workshop's lifts (bays).</summary>
public class Booking
{
    public int Id { get; set; }

    public string CustomerName { get; set; } = "";
    public string CustomerEmail { get; set; } = "";
    public string CustomerPhone { get; set; } = "";

    /// <summary>Stored normalised, e.g. "ABC123" or "AAAA123".</summary>
    public string LicensePlate { get; set; } = "";

    public int ServiceTypeId { get; set; }
    public ServiceType? ServiceType { get; set; }

    public DateTime StartTime { get; set; }
    public DateTime EndTime { get; set; }

    /// <summary>Which lift the car goes on (1..BayCount).</summary>
    public int BayNumber { get; set; }

    public BookingStatus Status { get; set; } = BookingStatus.Booked;
    public string? Note { get; set; }
    public DateTime CreatedAt { get; set; }
}
