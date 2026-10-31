using ServiceBooking.E2E.Pages;
using ServiceBooking.E2E.Support;

namespace ServiceBooking.E2E.Tests.UI;

[TestFixture]
[Category("UI")]
public class WorkshopTests : E2ETestBase
{
    [Test]
    [Description("TC-30: the board cannot be opened with a wrong key.")]
    public async Task Wrong_admin_key_is_rejected()
    {
        var workshop = new WorkshopPage(Page);
        await workshop.OpenAsync();

        await workshop.LogInAsync("rossz-kulcs");

        await Expect(workshop.ErrorAlert).ToHaveTextAsync("Hiányzó vagy hibás admin kulcs.");
        await Expect(workshop.LoginHeading).ToBeVisibleAsync();
        await Expect(workshop.BoardHeading).Not.ToBeVisibleAsync();
    }

    [Test]
    [Category("Smoke")]
    [Description("TC-31 + TC-32: a mechanic sees the booking on the board, starts and finishes the job.")]
    public async Task Mechanic_can_start_and_finish_a_job()
    {
        var (day, start) = await Api.FirstFreeSlotAsync("Olajcsere", minWorkdaysAhead: 2);
        var customer = TestData.NewCustomer();
        var booking = await Api.CreateBookingAsync("Olajcsere", start, customer);
        var workshop = new WorkshopPage(Page);
        await workshop.OpenAsync();
        await workshop.LogInAsync(TestSettings.AdminKey);
        await workshop.ShowDayAsync(day);

        var job = workshop.Job(booking.Id);
        await Expect(job).ToContainTextAsync(customer.Plate);
        await Expect(job).ToContainTextAsync("Foglalva");

        await workshop.StartWorkAsync(booking.Id);
        await Expect(job).ToContainTextAsync("Szerelés alatt");

        await workshop.FinishWorkAsync(booking.Id);
        await Expect(job.GetByText("Kész", new() { Exact = true })).ToBeVisibleAsync();
        await Expect(job.GetByRole(AriaRole.Button)).ToHaveCountAsync(0);
    }

    [Test]
    [Description("TC-33: a booking cancelled by the customer shows as cancelled on the board.")]
    public async Task Cancelled_booking_shows_as_cancelled()
    {
        var (day, start) = await Api.FirstFreeSlotAsync("Klímatisztítás", minWorkdaysAhead: 4);
        var customer = TestData.NewCustomer();
        var booking = await Api.CreateBookingAsync("Klímatisztítás", start, customer);
        var cancel = await Api.Http.PostAsync(
            $"api/bookings/{booking.Id}/cancel",
            System.Net.Http.Json.JsonContent.Create(new { email = customer.Email }));
        cancel.EnsureSuccessStatusCode();
        var workshop = new WorkshopPage(Page);
        await workshop.OpenAsync();
        await workshop.LogInAsync(TestSettings.AdminKey);

        await workshop.ShowDayAsync(day);

        await Expect(workshop.Job(booking.Id)).ToContainTextAsync("Lemondva");
    }
}
