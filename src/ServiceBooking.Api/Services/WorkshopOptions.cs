namespace ServiceBooking.Api.Services;

/// <summary>
/// Business settings, bound from the "Workshop" section of appsettings.json.
/// </summary>
public class WorkshopOptions
{
    /// <summary>Number of lifts, i.e. how many cars can be worked on at once.</summary>
    public int BayCount { get; set; } = 2;

    public int OpeningHour { get; set; } = 8;
    public int ClosingHour { get; set; } = 17;

    /// <summary>Bookings can only start on these steps (08:00, 08:15, ...).</summary>
    public int SlotMinutes { get; set; } = 15;

    public int MinHoursInAdvance { get; set; } = 2;
    public int MaxDaysInAdvance { get; set; } = 60;

    /// <summary>Customers can cancel online until this many hours before the start.</summary>
    public int FreeCancellationHours { get; set; } = 24;
}
