namespace ServiceBooking.Api.Services;

/// <summary>
/// "What time is it?" behind an interface, so tests can freeze time.
/// </summary>
public interface IClock
{
    /// <summary>Current local time at the workshop (Budapest).</summary>
    DateTime Now { get; }
}

public class SystemClock : IClock
{
    private static readonly TimeZoneInfo WorkshopTimeZone = FindWorkshopTimeZone();

    public DateTime Now => TimeZoneInfo.ConvertTimeFromUtc(DateTime.UtcNow, WorkshopTimeZone);

    private static TimeZoneInfo FindWorkshopTimeZone()
    {
        // Linux/Docker uses IANA ids, older Windows uses Windows ids.
        foreach (var id in new[] { "Europe/Budapest", "Central European Standard Time" })
        {
            try
            {
                return TimeZoneInfo.FindSystemTimeZoneById(id);
            }
            catch (TimeZoneNotFoundException)
            {
            }
            catch (InvalidTimeZoneException)
            {
            }
        }

        return TimeZoneInfo.Local;
    }
}
