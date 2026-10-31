using NUnit.Framework.Interfaces;

namespace ServiceBooking.E2E.Support;

/// <summary>
/// Base class for browser tests. Playwright's PageTest gives every test a fresh
/// browser context and Page. On top of that:
///  - the app's address, Hungarian locale and Budapest time zone,
///  - an API client for preparing test data,
///  - a Playwright trace saved for every failed test (open it with "playwright show-trace").
/// </summary>
public abstract class E2ETestBase : PageTest
{
    protected ApiClient Api { get; private set; } = null!;

    public override BrowserNewContextOptions ContextOptions()
    {
        return new BrowserNewContextOptions
        {
            BaseURL = TestSettings.BaseUrl,
            Locale = "hu-HU",
            TimezoneId = "Europe/Budapest",
            ViewportSize = new ViewportSize { Width = 1280, Height = 900 },
        };
    }

    [SetUp]
    public async Task BeforeEachTest()
    {
        Api = new ApiClient(TestSettings.BaseUrl, TestSettings.AdminKey);

        await Context.Tracing.StartAsync(new TracingStartOptions
        {
            Title = TestContext.CurrentContext.Test.FullName,
            Screenshots = true,
            Snapshots = true,
            Sources = true,
        });
    }

    [TearDown]
    public async Task AfterEachTest()
    {
        var failed = TestContext.CurrentContext.Result.Outcome.Status == TestStatus.Failed;
        string? tracePath = null;

        if (failed)
        {
            var safeName = string.Concat(TestContext.CurrentContext.Test.Name.Split(Path.GetInvalidFileNameChars()));
            tracePath = Path.Combine(TestContext.CurrentContext.WorkDirectory, "playwright-traces", $"{safeName}.zip");
            TestContext.Out.WriteLine($"Trace saved: {tracePath}");
        }

        await Context.Tracing.StopAsync(new TracingStopOptions { Path = tracePath });
        Api.Dispose();
    }
}
