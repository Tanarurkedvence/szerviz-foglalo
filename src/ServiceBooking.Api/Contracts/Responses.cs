using ServiceBooking.Api.Domain;
using ServiceBooking.Api.Services;

namespace ServiceBooking.Api.Contracts;

public record ServiceTypeDto(int Id, string Name, int DurationMinutes, int PriceHuf)
{
    public static ServiceTypeDto From(ServiceType s) => new(s.Id, s.Name, s.DurationMinutes, s.PriceHuf);
}

public record BookingDto(
    int Id,
    string CustomerName,
    string CustomerEmail,
    string CustomerPhone,
    string LicensePlate,
    int ServiceTypeId,
    string ServiceName,
    DateTime StartTime,
    DateTime EndTime,
    int BayNumber,
    BookingStatus Status,
    string? Note)
{
    public static BookingDto From(Booking b) => new(
        b.Id,
        b.CustomerName,
        b.CustomerEmail,
        b.CustomerPhone,
        LicensePlateRules.Format(b.LicensePlate),
        b.ServiceTypeId,
        b.ServiceType?.Name ?? "",
        b.StartTime,
        b.EndTime,
        b.BayNumber,
        b.Status,
        b.Note);
}

/// <summary>Every error response has this shape: { "errors": ["...", "..."] }</summary>
public record ErrorResponse(IReadOnlyList<string> Errors);
