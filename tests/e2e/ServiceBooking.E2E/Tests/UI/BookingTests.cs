using System.Text.RegularExpressions;
using ServiceBooking.E2E.Pages;
using ServiceBooking.E2E.Support;

namespace ServiceBooking.E2E.Tests.UI;

[TestFixture]
[Category("UI")]
public class BookingTests : E2ETestBase
{
    [Test]
    [Category("Smoke")]
    [Description("TC-01: a customer books an oil change and gets a booking number.")]
    public async Task Customer_can_book_an_oil_change()
    {
        var (day, start) = await Api.FirstFreeSlotAsync("Olajcsere", minWorkdaysAhead: 2);
        var customer = TestData.NewCustomer();
        var booking = new BookingPage(Page);
        await booking.OpenAsync();

        await booking.ChooseServiceAsync("Olajcsere");
        await booking.ChooseDayAsync(day);
        await booking.ChooseTimeAsync(start);
        await booking.FillCustomerAsync(customer);

        await Expect(booking.WorkOrder).ToContainTextAsync("Olajcsere");
        await Expect(booking.WorkOrder).ToContainTextAsync(start.ToString("HH:mm"));

        await booking.SubmitAsync();

        await Expect(booking.ConfirmationHeading).ToBeVisibleAsync();
        await Expect(booking.BookingNumber).ToHaveTextAsync(new Regex(@"^\d+$"));
    }

    [Test]
    [Description("TC-05: a time that is full on both lifts is not offered.")]
    public async Task Full_time_is_not_offered()
    {
        var (day, start) = await Api.FirstFreeSlotAsync("Olajcsere", minWorkdaysAhead: 3);
        await Api.FillSlotAsync("Olajcsere", start);
        var booking = new BookingPage(Page);
        await booking.OpenAsync();

        await booking.ChooseServiceAsync("Olajcsere");
        await booking.ChooseDayAsync(day);

        await Expect(booking.Slot(start)).ToHaveCountAsync(0);
    }

    [Test]
    [Description("TC-08: submitting the empty form lists every missing detail and sends nothing.")]
    public async Task Empty_form_lists_every_problem()
    {
        var booking = new BookingPage(Page);
        await booking.OpenAsync();

        await booking.SubmitAsync();

        await Expect(booking.ErrorAlert.GetByRole(AriaRole.Listitem)).ToHaveCountAsync(6);
        await Expect(booking.ErrorAlert).ToContainTextAsync("Válassz időpontot.");
    }

    [Test]
    [Description("TC-10: an invalid plate is flagged while typing.")]
    public async Task Invalid_plate_is_flagged_while_typing()
    {
        var booking = new BookingPage(Page);
        await booking.OpenAsync();

        await booking.PlateField.FillAsync("AB-12345");

        await Expect(booking.PlateHint).ToBeVisibleAsync();
        await Expect(booking.PlateField).ToHaveAttributeAsync("aria-invalid", "true");
    }

    [TestCase("abc123", "ABC-123")]
    [TestCase("aaaa123", "AA AA-123")]
    [Description("TC-11: the plate is formatted the official way when leaving the field.")]
    public async Task Plate_is_formatted_when_leaving_the_field(string typed, string expected)
    {
        var booking = new BookingPage(Page);
        await booking.OpenAsync();

        await booking.PlateField.FillAsync(typed);
        await booking.PlateField.PressAsync("Tab");

        await Expect(booking.PlateField).ToHaveValueAsync(expected);
    }

    [Test]
    [Description("TC-14: the service can be chosen with the keyboard only.")]
    public async Task Service_can_be_chosen_with_the_keyboard()
    {
        var booking = new BookingPage(Page);
        await booking.OpenAsync();
        var oilChange = Page.GetByRole(AriaRole.Radio, new() { Name = "Olajcsere" });
        var brakes = Page.GetByRole(AriaRole.Radio, new() { Name = "Fékbetét csere" });

        await oilChange.FocusAsync();
        await Page.Keyboard.PressAsync("Space");
        await Expect(oilChange).ToBeCheckedAsync();

        await Page.Keyboard.PressAsync("ArrowDown");
        await Expect(brakes).ToBeCheckedAsync();
        await Expect(booking.WorkOrder).ToContainTextAsync("Fékbetét csere");
    }

    [Test]
    [Category("Smoke")]
    [Description("TC-16: on a phone-sized screen the page does not scroll sideways.")]
    public async Task Page_fits_a_phone_screen()
    {
        await Page.SetViewportSizeAsync(390, 844);
        var booking = new BookingPage(Page);
        await booking.OpenAsync();
        await booking.ChooseServiceAsync("Olajcsere");

        var overflow = await Page.EvaluateAsync<bool>("() => document.documentElement.scrollWidth > window.innerWidth");

        Assert.That(overflow, Is.False, "The page is wider than the screen.");
    }
}
