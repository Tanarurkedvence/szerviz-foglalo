using ServiceBooking.E2E.Pages;
using ServiceBooking.E2E.Support;

namespace ServiceBooking.E2E.Tests.UI;

[TestFixture]
[Category("UI")]
public class CancelTests : E2ETestBase
{
    [Test]
    [Category("Smoke")]
    [Description("TC-20: the customer cancels with the booking number and e-mail address.")]
    public async Task Customer_can_cancel_their_booking()
    {
        // Arrange through the API: faster and independent of the booking page.
        var (_, start) = await Api.FirstFreeSlotAsync("Gumicsere", minWorkdaysAhead: 3);
        var customer = TestData.NewCustomer();
        var booking = await Api.CreateBookingAsync("Gumicsere", start, customer);
        var cancelPage = new CancelPage(Page);
        await cancelPage.OpenAsync();

        await cancelPage.CancelAsync(booking.Id, customer.Email);

        await Expect(cancelPage.SuccessHeading).ToBeVisibleAsync();
    }

    [Test]
    [Description("TC-22: someone else's e-mail address cannot cancel a booking.")]
    public async Task Wrong_email_cannot_cancel()
    {
        var (_, start) = await Api.FirstFreeSlotAsync("Gumicsere", minWorkdaysAhead: 3);
        var booking = await Api.CreateBookingAsync("Gumicsere", start, TestData.NewCustomer());
        var cancelPage = new CancelPage(Page);
        await cancelPage.OpenAsync();

        await cancelPage.CancelAsync(booking.Id, "valaki.mas@example.com");

        await Expect(cancelPage.ErrorAlert).ToHaveTextAsync("Nincs ilyen foglalás ezzel az e-mail címmel.");
        await Expect(cancelPage.SuccessHeading).Not.ToBeVisibleAsync();
    }

    [Test]
    [Description("TC-21: a booking that starts within 24 hours cannot be cancelled online.")]
    public async Task Booking_within_24_hours_cannot_be_cancelled_online()
    {
        // The latest time today or on the next workday that is still bookable but less than 24 hours away.
        var start = await LatestBookableTimeWithin24HoursAsync();
        if (start is null)
        {
            Assert.Ignore("No bookable time within the next 24 hours right now (e.g. weekend or late evening).");
            return;
        }

        var customer = TestData.NewCustomer();
        var booking = await Api.CreateBookingAsync("Gumicsere", start.Value, customer);
        var cancelPage = new CancelPage(Page);
        await cancelPage.OpenAsync();

        await cancelPage.CancelAsync(booking.Id, customer.Email);

        await Expect(cancelPage.ErrorAlert).ToContainTextAsync("24 órán belül");
    }

    private async Task<DateTime?> LatestBookableTimeWithin24HoursAsync()
    {
        var serviceId = await Api.ServiceIdAsync("Gumicsere");
        var limit = DateTime.Now.AddHours(23);
        foreach (var day in new[] { TestData.Workday(0), TestData.Workday(1) })
        {
            var slots = await Api.GetAvailabilityAsync(serviceId, day);
            var candidate = slots.Where(s => s < limit).Cast<DateTime?>().FirstOrDefault();
            if (candidate is not null)
            {
                return candidate;
            }
        }

        return null;
    }
}
