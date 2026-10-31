using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using System.Text.Json.Serialization;
using ServiceBooking.Api.Contracts;
using ServiceBooking.Api.Domain;
using ServiceBooking.Tests.Helpers;

namespace ServiceBooking.Tests;

/// <summary>
/// Integration tests: real HTTP requests against the whole app
/// (routing, JSON, filters, database), started in memory by ApiFactory.
/// The tests share one database, so each uses its own day or licence plate.
/// </summary>
public class ApiTests : IClassFixture<ApiFactory>
{
    private static readonly JsonSerializerOptions Json = new(JsonSerializerDefaults.Web)
    {
        Converters = { new JsonStringEnumConverter() }
    };

    private readonly ApiFactory _factory;
    private readonly HttpClient _client;

    public ApiTests(ApiFactory factory)
    {
        _factory = factory;
        _client = factory.CreateClient();
    }

    [Fact]
    public async Task GetServices_ReturnsTheWorkshopsServices()
    {
        var services = await _client.GetFromJsonAsync<List<ServiceTypeDto>>("/api/services", Json);

        Assert.NotNull(services);
        Assert.Equal(5, services.Count);
        Assert.Contains(services, s => s.Name == "Olajcsere" && s.DurationMinutes == 60);
    }

    [Fact]
    public async Task CreateBooking_ValidRequest_Returns201AndTheBooking()
    {
        var oilChange = await ServiceIdAsync("Olajcsere");
        var request = TestData.BookingRequest(new DateTime(2026, 11, 3, 9, 0, 0), oilChange, plate: "xyz123");

        var response = await _client.PostAsJsonAsync("/api/bookings", request, Json);

        Assert.Equal(HttpStatusCode.Created, response.StatusCode);
        Assert.NotNull(response.Headers.Location);
        var booking = await response.Content.ReadFromJsonAsync<BookingDto>(Json);
        Assert.NotNull(booking);
        Assert.Equal("XYZ-123", booking.LicensePlate);
        Assert.Equal(BookingStatus.Booked, booking.Status);
        Assert.Equal("Olajcsere", booking.ServiceName);
    }

    [Fact]
    public async Task CreateBooking_InvalidData_Returns400WithEveryError()
    {
        var oilChange = await ServiceIdAsync("Olajcsere");
        var request = TestData.BookingRequest(new DateTime(2026, 11, 3, 10, 0, 0), oilChange, plate: "XX", email: "nem-email");

        var response = await _client.PostAsJsonAsync("/api/bookings", request, Json);

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
        var error = await response.Content.ReadFromJsonAsync<ErrorResponse>(Json);
        Assert.NotNull(error);
        Assert.Equal(2, error.Errors.Count); // e-mail + plate
    }

    [Fact]
    public async Task CreateBooking_WhenFull_Returns409()
    {
        var oilChange = await ServiceIdAsync("Olajcsere");
        var start = new DateTime(2026, 11, 6, 11, 0, 0);
        await CreateBookingAsync(start, oilChange, "FUL-001");
        await CreateBookingAsync(start, oilChange, "FUL-002");

        var response = await _client.PostAsJsonAsync("/api/bookings", TestData.BookingRequest(start, oilChange, plate: "FUL-003"), Json);

        Assert.Equal(HttpStatusCode.Conflict, response.StatusCode);
    }

    [Fact]
    public async Task ListBookings_WithoutAdminKey_Returns401()
    {
        var response = await _client.GetAsync("/api/bookings");

        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
    }

    [Fact]
    public async Task ListBookings_WithWrongAdminKey_Returns401()
    {
        using var client = _factory.CreateClient();
        client.DefaultRequestHeaders.Add("X-Admin-Key", "rossz-kulcs");

        var response = await client.GetAsync("/api/bookings");

        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
    }

    [Fact]
    public async Task AdminFlow_StartThenCompleteBooking()
    {
        var oilChange = await ServiceIdAsync("Olajcsere");
        var created = await CreateBookingAsync(new DateTime(2026, 11, 4, 13, 0, 0), oilChange, "ADM-123");
        using var admin = AdminClient();

        var started = await admin.PostAsJsonAsync($"/api/bookings/{created.Id}/status", new ChangeStatusRequest { Status = BookingStatus.InProgress }, Json);
        var completed = await admin.PostAsJsonAsync($"/api/bookings/{created.Id}/status", new ChangeStatusRequest { Status = BookingStatus.Completed }, Json);
        var dayList = await admin.GetFromJsonAsync<List<BookingDto>>("/api/bookings?date=2026-11-04", Json);

        Assert.Equal(HttpStatusCode.OK, started.StatusCode);
        Assert.Equal(HttpStatusCode.OK, completed.StatusCode);
        Assert.NotNull(dayList);
        Assert.Contains(dayList, b => b.Id == created.Id && b.Status == BookingStatus.Completed);
    }

    [Fact]
    public async Task ChangeStatus_SkippingInProgress_Returns422()
    {
        var oilChange = await ServiceIdAsync("Olajcsere");
        var created = await CreateBookingAsync(new DateTime(2026, 11, 4, 15, 0, 0), oilChange, "SKP-123");
        using var admin = AdminClient();

        var response = await admin.PostAsJsonAsync($"/api/bookings/{created.Id}/status", new ChangeStatusRequest { Status = BookingStatus.Completed }, Json);

        Assert.Equal(HttpStatusCode.UnprocessableEntity, response.StatusCode);
    }

    [Fact]
    public async Task Availability_SlotDisappearsWhenBothLiftsAreTaken()
    {
        var oilChange = await ServiceIdAsync("Olajcsere");
        var thursday = new DateTime(2026, 11, 5);
        await CreateBookingAsync(thursday.AddHours(8), oilChange, "AVL-001");
        await CreateBookingAsync(thursday.AddHours(8), oilChange, "AVL-002");

        var slots = await _client.GetFromJsonAsync<List<DateTime>>($"/api/availability?serviceTypeId={oilChange}&date=2026-11-05", Json);

        Assert.NotNull(slots);
        Assert.DoesNotContain(thursday.AddHours(8), slots);
        Assert.Contains(thursday.AddHours(9), slots);
    }

    [Fact]
    public async Task Cancel_ByCustomer_Returns200AndCancelledStatus()
    {
        var oilChange = await ServiceIdAsync("Olajcsere");
        var created = await CreateBookingAsync(new DateTime(2026, 11, 10, 9, 0, 0), oilChange, "CNL-123");

        var response = await _client.PostAsJsonAsync($"/api/bookings/{created.Id}/cancel", new CancelBookingRequest { Email = "teszt.elek@example.com" }, Json);

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        var booking = await response.Content.ReadFromJsonAsync<BookingDto>(Json);
        Assert.Equal(BookingStatus.Cancelled, booking!.Status);
    }

    [Fact]
    public async Task UnknownApiAddress_Returns404AsJson()
    {
        var response = await _client.GetAsync("/api/nincs-ilyen");

        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
        Assert.Equal("application/json", response.Content.Headers.ContentType?.MediaType);
    }

    // ---------- helpers ----------

    private HttpClient AdminClient()
    {
        var client = _factory.CreateClient();
        client.DefaultRequestHeaders.Add("X-Admin-Key", ApiFactory.AdminKey);
        return client;
    }

    private async Task<int> ServiceIdAsync(string name)
    {
        var services = await _client.GetFromJsonAsync<List<ServiceTypeDto>>("/api/services", Json);
        return services!.Single(s => s.Name == name).Id;
    }

    private async Task<BookingDto> CreateBookingAsync(DateTime start, int serviceTypeId, string plate)
    {
        var response = await _client.PostAsJsonAsync("/api/bookings", TestData.BookingRequest(start, serviceTypeId, plate: plate), Json);
        response.EnsureSuccessStatusCode();
        return (await response.Content.ReadFromJsonAsync<BookingDto>(Json))!;
    }
}
