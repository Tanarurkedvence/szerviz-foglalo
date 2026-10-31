using System.Net;
using System.Net.Http.Json;
using ServiceBooking.E2E.Support;

namespace ServiceBooking.E2E.Tests.Api;

/// <summary>
/// API tests: plain HTTP, no browser. They check the contract the React app
/// (or any other client) relies on: status codes, error format and business rules.
/// </summary>
[TestFixture]
[Category("API")]
public class BookingApiTests
{
    private ApiClient _api = null!;
    private ApiClient _anonymous = null!;

    [SetUp]
    public void SetUp()
    {
        _api = new ApiClient(TestSettings.BaseUrl, TestSettings.AdminKey);
        _anonymous = new ApiClient(TestSettings.BaseUrl);
    }

    [TearDown]
    public void TearDown()
    {
        _api.Dispose();
        _anonymous.Dispose();
    }

    [Test]
    [Category("Smoke")]
    public async Task Services_are_listed_with_duration_and_price()
    {
        var services = await _anonymous.GetServicesAsync();

        Assert.That(services, Is.Not.Empty);
        Assert.That(services, Has.All.Matches<ServiceTypeDto>(s => s.DurationMinutes > 0 && s.PriceHuf > 0));
        Assert.That(services.Select(s => s.Name), Does.Contain("Olajcsere"));
    }

    [Test]
    public async Task Invalid_booking_returns_400_with_every_problem()
    {
        var oilChange = await _api.ServiceIdAsync("Olajcsere");
        var (_, start) = await _api.FirstFreeSlotAsync("Olajcsere", minWorkdaysAhead: 2);
        var request = ApiClient.BookingRequest(oilChange, start, new Customer("", "abc", "nem-email", "XX-1"));

        var response = await _anonymous.PostBookingAsync(request);
        var error = await response.Content.ReadFromJsonAsync<ErrorResponse>(ApiClient.Json);

        Assert.That(response.StatusCode, Is.EqualTo(HttpStatusCode.BadRequest));
        Assert.That(error!.Errors, Has.Count.EqualTo(4)); // name, e-mail, phone, plate
    }

    [Test]
    public async Task Weekend_booking_is_rejected()
    {
        var oilChange = await _api.ServiceIdAsync("Olajcsere");
        var saturday = TestData.NextSaturday().ToDateTime(new TimeOnly(9, 0));

        var response = await _anonymous.PostBookingAsync(ApiClient.BookingRequest(oilChange, saturday, TestData.NewCustomer()));
        var error = await response.Content.ReadFromJsonAsync<ErrorResponse>(ApiClient.Json);

        Assert.That(response.StatusCode, Is.EqualTo(HttpStatusCode.BadRequest));
        Assert.That(error!.Errors, Does.Contain("Hétvégén zárva vagyunk."));
    }

    // Boundary values around the 15-minute grid.
    [TestCase(0, HttpStatusCode.Created)]
    [TestCase(5, HttpStatusCode.BadRequest)]
    [TestCase(14, HttpStatusCode.BadRequest)]
    public async Task Start_time_must_be_on_the_15_minute_grid(int minutesPastSlot, HttpStatusCode expected)
    {
        var gumicsere = await _api.ServiceIdAsync("Gumicsere");
        var (_, start) = await _api.FirstFreeSlotAsync("Gumicsere", minWorkdaysAhead: 5);

        var response = await _anonymous.PostBookingAsync(
            ApiClient.BookingRequest(gumicsere, start.AddMinutes(minutesPastSlot), TestData.NewCustomer()));

        Assert.That(response.StatusCode, Is.EqualTo(expected));
    }

    [Test]
    public async Task Full_time_returns_409()
    {
        var (_, start) = await _api.FirstFreeSlotAsync("Olajcsere", minWorkdaysAhead: 6);
        await _api.FillSlotAsync("Olajcsere", start);
        var oilChange = await _api.ServiceIdAsync("Olajcsere");

        var response = await _anonymous.PostBookingAsync(ApiClient.BookingRequest(oilChange, start, TestData.NewCustomer()));
        var error = await response.Content.ReadFromJsonAsync<ErrorResponse>(ApiClient.Json);

        Assert.That(response.StatusCode, Is.EqualTo(HttpStatusCode.Conflict));
        Assert.That(error!.Errors.Single(), Does.Contain("betelt"));
    }

    [Test]
    public async Task Admin_endpoints_need_the_right_key()
    {
        using var wrongKey = new ApiClient(TestSettings.BaseUrl, "rossz-kulcs");

        var noKey = await _anonymous.Http.GetAsync("api/bookings");
        var badKey = await wrongKey.Http.GetAsync("api/bookings");
        var goodKey = await _api.Http.GetAsync("api/bookings");

        Assert.Multiple(() =>
        {
            Assert.That(noKey.StatusCode, Is.EqualTo(HttpStatusCode.Unauthorized));
            Assert.That(badKey.StatusCode, Is.EqualTo(HttpStatusCode.Unauthorized));
            Assert.That(goodKey.StatusCode, Is.EqualTo(HttpStatusCode.OK));
        });
    }

    [Test]
    public async Task Status_cannot_skip_from_booked_to_completed()
    {
        var (_, start) = await _api.FirstFreeSlotAsync("Olajcsere", minWorkdaysAhead: 2);
        var booking = await _api.CreateBookingAsync("Olajcsere", start, TestData.NewCustomer());

        var response = await _api.Http.PostAsJsonAsync($"api/bookings/{booking.Id}/status", new { status = "Completed" }, ApiClient.Json);

        Assert.That(response.StatusCode, Is.EqualTo(HttpStatusCode.UnprocessableEntity));
    }

    [Test]
    [Description("Security: 'wrong e-mail' and 'no such booking' must look the same, so ids cannot be probed.")]
    public async Task Cancel_does_not_reveal_whether_a_booking_exists()
    {
        var (_, start) = await _api.FirstFreeSlotAsync("Olajcsere", minWorkdaysAhead: 2);
        var booking = await _api.CreateBookingAsync("Olajcsere", start, TestData.NewCustomer());

        var wrongEmail = await _anonymous.Http.PostAsJsonAsync($"api/bookings/{booking.Id}/cancel", new { email = "valaki@example.com" });
        var noSuchBooking = await _anonymous.Http.PostAsJsonAsync("api/bookings/999999999/cancel", new { email = "valaki@example.com" });

        Assert.That(wrongEmail.StatusCode, Is.EqualTo(HttpStatusCode.NotFound));
        Assert.That(noSuchBooking.StatusCode, Is.EqualTo(HttpStatusCode.NotFound));
        Assert.That(await wrongEmail.Content.ReadAsStringAsync(), Is.EqualTo(await noSuchBooking.Content.ReadAsStringAsync()));
    }

    [Test]
    [Description("BUG-001 regression: unknown API addresses must not return the web page.")]
    public async Task Unknown_api_address_returns_json_404()
    {
        var response = await _anonymous.Http.GetAsync("api/nincs-ilyen");

        Assert.That(response.StatusCode, Is.EqualTo(HttpStatusCode.NotFound));
        Assert.That(response.Content.Headers.ContentType?.MediaType, Is.EqualTo("application/json"));
    }
}
