using ServiceBooking.E2E.Support;

namespace ServiceBooking.E2E.Pages;

/// <summary>
/// Page Object for the booking page: the tests describe *what* the user does,
/// this class knows *how* to find things on the page. If the page changes,
/// only this file needs updating.
/// Locators use roles and labels (what the user sees), not CSS classes.
/// </summary>
public class BookingPage
{
    private readonly IPage _page;

    public BookingPage(IPage page)
    {
        _page = page;
    }

    public ILocator SubmitButton => _page.GetByRole(AriaRole.Button, new() { Name = "Időpont foglalása" });
    public ILocator ErrorAlert => _page.GetByRole(AriaRole.Alert);
    public ILocator WorkOrder => _page.GetByRole(AriaRole.Complementary, new() { Name = "Munkalap" });
    public ILocator ConfirmationHeading => _page.GetByRole(AriaRole.Heading, new() { Name = "Lefoglaltuk az időpontot" });
    public ILocator BookingNumber => _page.GetByTestId("booking-number");
    public ILocator PlateField => _page.GetByLabel("Rendszám", new() { Exact = true });
    public ILocator PlateHint => _page.GetByText("Ez nem magyar rendszám");
    public ILocator LoadingSlots => _page.GetByText("Szabad időpontok betöltése");

    public async Task OpenAsync()
    {
        await _page.GotoAsync("/#/foglalas");
        await _page.GetByRole(AriaRole.Radio).First.WaitForAsync();
    }

    public Task ChooseServiceAsync(string name)
    {
        return _page.GetByRole(AriaRole.Radio, new() { Name = name }).CheckAsync();
    }

    /// <summary>
    /// Picks a day and waits until that day's free times have arrived.
    /// Call it after choosing a service (the times are only loaded then).
    /// Waiting for the response avoids a flaky test that looks at the previous day's times.
    /// </summary>
    public async Task ChooseDayAsync(DateOnly day)
    {
        await _page.RunAndWaitForResponseAsync(
            () => _page.GetByTestId($"day-{day:yyyy-MM-dd}").ClickAsync(),
            response => response.Url.Contains("/api/availability") && response.Url.Contains($"date={day:yyyy-MM-dd}"));
        await Assertions.Expect(LoadingSlots).ToBeHiddenAsync();
    }

    public ILocator Slot(DateTime start) => _page.GetByTestId($"slot-{start:HH:mm}");

    public Task ChooseTimeAsync(DateTime start) => Slot(start).ClickAsync();

    public async Task FillCustomerAsync(Customer customer)
    {
        await _page.GetByLabel("Név", new() { Exact = true }).FillAsync(customer.Name);
        await _page.GetByLabel("Telefonszám", new() { Exact = true }).FillAsync(customer.Phone);
        await _page.GetByLabel("E-mail cím", new() { Exact = true }).FillAsync(customer.Email);
        await PlateField.FillAsync(customer.Plate);
    }

    public Task SubmitAsync() => SubmitButton.ClickAsync();

    /// <summary>The whole happy path in one call, for tests that only need "a booking made in the UI".</summary>
    public async Task BookAsync(string service, DateOnly day, DateTime start, Customer customer)
    {
        await ChooseServiceAsync(service);
        await ChooseDayAsync(day);
        await ChooseTimeAsync(start);
        await FillCustomerAsync(customer);
        await SubmitAsync();
    }
}
