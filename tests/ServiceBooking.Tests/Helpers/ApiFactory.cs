using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.AspNetCore.TestHost;
using Microsoft.Data.Sqlite;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using ServiceBooking.Api.Services;

namespace ServiceBooking.Tests.Helpers;

/// <summary>
/// Starts the real API in memory for integration tests, with
/// its own in-memory database, a fixed clock and a known admin key.
/// </summary>
public class ApiFactory : WebApplicationFactory<Program>
{
    public const string AdminKey = "test-admin-key";

    // A named, shared in-memory database. It exists while at least one connection is open,
    // so this factory keeps one open for its whole lifetime.
    private readonly string _connectionString = $"Data Source=servicebooking-tests-{Guid.NewGuid():N};Mode=Memory;Cache=Shared";
    private readonly SqliteConnection _keepAlive;

    public ApiFactory()
    {
        _keepAlive = new SqliteConnection(_connectionString);
        _keepAlive.Open();
    }

    public FakeClock Clock { get; } = new(TestData.Now);

    protected override void ConfigureWebHost(IWebHostBuilder builder)
    {
        builder.UseEnvironment("Testing");

        builder.ConfigureAppConfiguration((_, config) =>
        {
            config.AddInMemoryCollection(new Dictionary<string, string?>
            {
                ["ConnectionStrings:Default"] = _connectionString,
                ["AdminKey"] = AdminKey
            });
        });

        builder.ConfigureTestServices(services => services.AddSingleton<IClock>(Clock));
    }

    protected override void Dispose(bool disposing)
    {
        base.Dispose(disposing);

        if (disposing)
        {
            _keepAlive.Dispose();
        }
    }
}
