using ServiceBooking.Api.Contracts;

namespace ServiceBooking.Tests.Helpers;

public static class TestData
{
    /// <summary>"Now" in every test: Monday, 2 November 2026, 10:00.</summary>
    public static readonly DateTime Now = new(2026, 11, 2, 10, 0, 0);

    /// <summary>The next day, a normal working day.</summary>
    public static readonly DateTime Tuesday = new(2026, 11, 3);

    public static CreateBookingRequest BookingRequest(
        DateTime start,
        int serviceTypeId,
        string plate = "ABC-123",
        string email = "teszt.elek@example.com")
    {
        return new CreateBookingRequest
        {
            CustomerName = "Teszt Elek",
            CustomerEmail = email,
            CustomerPhone = "+36 30 123 4567",
            LicensePlate = plate,
            ServiceTypeId = serviceTypeId,
            StartTime = start
        };
    }
}
