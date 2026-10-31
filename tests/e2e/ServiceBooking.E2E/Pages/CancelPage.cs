namespace ServiceBooking.E2E.Pages;

public class CancelPage
{
    private readonly IPage _page;

    public CancelPage(IPage page)
    {
        _page = page;
    }

    public ILocator SuccessHeading => _page.GetByRole(AriaRole.Heading, new() { Name = "Lemondtuk a foglalást" });
    public ILocator ErrorAlert => _page.GetByRole(AriaRole.Alert);

    public Task OpenAsync() => _page.GotoAsync("/#/lemondas");

    public async Task CancelAsync(int bookingNumber, string email)
    {
        await _page.GetByLabel("Foglalásszám").FillAsync(bookingNumber.ToString());
        await _page.GetByLabel("E-mail cím, amellyel foglaltál").FillAsync(email);
        await _page.GetByRole(AriaRole.Button, new() { Name = "Foglalás lemondása" }).ClickAsync();
    }
}
