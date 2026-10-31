using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace ServiceBooking.E2E.Support;

// The E2E project does not reference the app's code: it only knows the public API,
// like any other client would. These records describe that contract.
public record ServiceTypeDto(int Id, string Name, int DurationMinutes, int PriceHuf);

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
    string Status,
    string? Note);

public record ErrorResponse(List<string> Errors);

/// <summary>
/// Talks to the API directly. UI tests use it to prepare data quickly
/// (arrange through the API, act and assert through the browser).
/// </summary>
public sealed class ApiClient : IDisposable
{
    public static readonly JsonSerializerOptions Json = new(JsonSerializerDefaults.Web)
    {
        Converters = { new JsonStringEnumConverter() }
    };

    public ApiClient(string baseUrl, string? adminKey = null)
    {
        Http = new HttpClient { BaseAddress = new Uri(baseUrl + "/"), Timeout = TimeSpan.FromSeconds(90) };
        if (adminKey is not null)
        {
            Http.DefaultRequestHeaders.Add("X-Admin-Key", adminKey);
        }
    }

    public HttpClient Http { get; }

    public async Task<List<ServiceTypeDto>> GetServicesAsync()
    {
        return await Http.GetFromJsonAsync<List<ServiceTypeDto>>("api/services", Json) ?? new List<ServiceTypeDto>();
    }

    public async Task<int> ServiceIdAsync(string name)
    {
        var services = await GetServicesAsync();
        return services.Single(s => s.Name == name).Id;
    }

    public async Task<List<DateTime>> GetAvailabilityAsync(int serviceTypeId, DateOnly day)
    {
        var url = $"api/availability?serviceTypeId={serviceTypeId}&date={day:yyyy-MM-dd}";
        return await Http.GetFromJsonAsync<List<DateTime>>(url, Json) ?? new List<DateTime>();
    }

    /// <summary>The first free start time, looking from `minWorkdaysAhead` up to the last day the UI shows.</summary>
    public async Task<(DateOnly Day, DateTime Start)> FirstFreeSlotAsync(string serviceName, int minWorkdaysAhead)
    {
        var serviceId = await ServiceIdAsync(serviceName);
        for (var ahead = minWorkdaysAhead; ahead <= 14; ahead++)
        {
            var day = TestData.Workday(ahead);
            var slots = await GetAvailabilityAsync(serviceId, day);
            if (slots.Count > 0)
            {
                return (day, slots[0]);
            }
        }

        throw new InvalidOperationException($"No free time found for {serviceName} in the next 14 workdays.");
    }

    public Task<HttpResponseMessage> PostBookingAsync(object request)
    {
        return Http.PostAsJsonAsync("api/bookings", request, Json);
    }

    public static object BookingRequest(int serviceTypeId, DateTime start, Customer customer)
    {
        return new
        {
            customerName = customer.Name,
            customerEmail = customer.Email,
            customerPhone = customer.Phone,
            licensePlate = customer.Plate,
            serviceTypeId,
            startTime = start.ToString("yyyy-MM-dd'T'HH:mm:ss"),
        };
    }

    /// <summary>Creates a booking and fails the test if the API refuses it.</summary>
    public async Task<BookingDto> CreateBookingAsync(string serviceName, DateTime start, Customer customer)
    {
        var serviceId = await ServiceIdAsync(serviceName);
        var response = await PostBookingAsync(BookingRequest(serviceId, start, customer));
        if (response.StatusCode != HttpStatusCode.Created)
        {
            Assert.Fail($"Could not create test booking: {(int)response.StatusCode} {await response.Content.ReadAsStringAsync()}");
        }

        return (await response.Content.ReadFromJsonAsync<BookingDto>(Json))!;
    }

    /// <summary>Books the same start time with new customers until the workshop is full (409).</summary>
    public async Task FillSlotAsync(string serviceName, DateTime start)
    {
        var serviceId = await ServiceIdAsync(serviceName);
        for (var attempt = 0; attempt < 10; attempt++)
        {
            var response = await PostBookingAsync(BookingRequest(serviceId, start, TestData.NewCustomer()));
            if (response.StatusCode == HttpStatusCode.Conflict)
            {
                return;
            }

            response.EnsureSuccessStatusCode();
        }

        Assert.Fail("The slot never became full.");
    }

    public void Dispose() => Http.Dispose();
}
