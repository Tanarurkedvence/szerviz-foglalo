using ServiceBooking.Api.Services;

namespace ServiceBooking.Tests.Helpers;

/// <summary>A clock the test controls, so "now" is always the same.</summary>
public class FakeClock : IClock
{
    public FakeClock(DateTime now)
    {
        Now = now;
    }

    public DateTime Now { get; set; }
}
