namespace ServiceBooking.E2E.Support;

public record Customer(string Name, string Phone, string Email, string Plate);

/// <summary>
/// Fresh data for every test, so tests never depend on each other or on earlier runs.
/// </summary>
public static class TestData
{
    private const string Letters = "ABCDEFGHIJKLMNOPRSTUVZ";

    /// <summary>A random old-format plate, e.g. "KRT-482".</summary>
    public static string RandomPlate()
    {
        var letters = new string(Enumerable.Range(0, 3).Select(_ => Letters[Random.Shared.Next(Letters.Length)]).ToArray());
        return $"{letters}-{Random.Shared.Next(100, 1000)}";
    }

    public static string UniqueEmail() => $"e2e.{Guid.NewGuid():N}@example.com";

    public static Customer NewCustomer() => new("Teszt Elek", "+36 30 123 4567", UniqueEmail(), RandomPlate());

    /// <summary>
    /// The date `workdaysAhead` weekdays after today (0 = today if it is a weekday).
    /// The booking page shows today plus the next 14 weekdays.
    /// </summary>
    public static DateOnly Workday(int workdaysAhead)
    {
        var day = DateOnly.FromDateTime(DateTime.Today);
        var counted = -1;
        while (true)
        {
            if (day.DayOfWeek is not (DayOfWeek.Saturday or DayOfWeek.Sunday))
            {
                counted++;
                if (counted == workdaysAhead)
                {
                    return day;
                }
            }

            day = day.AddDays(1);
        }
    }

    /// <summary>The next Saturday that is at least 2 days away.</summary>
    public static DateOnly NextSaturday()
    {
        var day = DateOnly.FromDateTime(DateTime.Today).AddDays(2);
        while (day.DayOfWeek != DayOfWeek.Saturday)
        {
            day = day.AddDays(1);
        }

        return day;
    }
}
