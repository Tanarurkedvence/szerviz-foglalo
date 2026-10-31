namespace ServiceBooking.E2E.Pages;

/// <summary>The staff view ("Műhely"): login and the daily board of lifts.</summary>
public class WorkshopPage
{
    private readonly IPage _page;

    public WorkshopPage(IPage page)
    {
        _page = page;
    }

    public ILocator LoginHeading => _page.GetByRole(AriaRole.Heading, new() { Name = "Belépés a műhely nézetbe" });
    public ILocator BoardHeading => _page.GetByRole(AriaRole.Heading, new() { Name = "Napi beosztás" });
    public ILocator ErrorAlert => _page.GetByRole(AriaRole.Alert);

    public Task OpenAsync() => _page.GotoAsync("/#/muhely");

    public async Task LogInAsync(string adminKey)
    {
        await _page.GetByLabel("Admin kulcs").FillAsync(adminKey);
        await _page.GetByRole(AriaRole.Button, new() { Name = "Belépés" }).ClickAsync();
    }

    public Task ShowDayAsync(DateOnly day) => _page.GetByLabel("Nap", new() { Exact = true }).FillAsync($"{day:yyyy-MM-dd}");

    /// <summary>The card of one booking on the board.</summary>
    public ILocator Job(int bookingId) => _page.GetByTestId($"job-{bookingId}");

    public Task StartWorkAsync(int bookingId) =>
        Job(bookingId).GetByRole(AriaRole.Button, new() { Name = "Szerelés indítása" }).ClickAsync();

    public Task FinishWorkAsync(int bookingId) =>
        Job(bookingId).GetByRole(AriaRole.Button, new() { Name = "Kész", Exact = true }).ClickAsync();
}
