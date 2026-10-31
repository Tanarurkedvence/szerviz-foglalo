namespace ServiceBooking.Api.Domain;

/// <summary>A job the workshop offers, e.g. an oil change.</summary>
public class ServiceType
{
    public int Id { get; set; }
    public string Name { get; set; } = "";
    public int DurationMinutes { get; set; }
    public int PriceHuf { get; set; }
}
