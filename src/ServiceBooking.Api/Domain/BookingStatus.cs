namespace ServiceBooking.Api.Domain;

/// <summary>
/// The life cycle of a booking:
/// Booked -> InProgress -> Completed, or Booked -> Cancelled.
/// </summary>
public enum BookingStatus
{
    Booked,
    InProgress,
    Completed,
    Cancelled
}
