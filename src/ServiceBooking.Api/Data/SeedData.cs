using ServiceBooking.Api.Domain;

namespace ServiceBooking.Api.Data;

/// <summary>Fills an empty database with the workshop's services.</summary>
public static class SeedData
{
    public static void Seed(AppDbContext db)
    {
        if (db.ServiceTypes.Any())
        {
            return;
        }

        db.ServiceTypes.AddRange(
            new ServiceType { Name = "Olajcsere", DurationMinutes = 60, PriceHuf = 18000 },
            new ServiceType { Name = "Fékbetét csere", DurationMinutes = 90, PriceHuf = 32000 },
            new ServiceType { Name = "Gumicsere", DurationMinutes = 45, PriceHuf = 12000 },
            new ServiceType { Name = "Klímatisztítás", DurationMinutes = 60, PriceHuf = 15000 },
            new ServiceType { Name = "Műszaki vizsga felkészítés", DurationMinutes = 120, PriceHuf = 25000 });

        db.SaveChanges();
    }
}
