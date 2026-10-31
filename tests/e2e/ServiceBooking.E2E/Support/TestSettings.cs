namespace ServiceBooking.E2E.Support;

/// <summary>
/// Where the app under test runs. Set the environment variables to test another
/// environment (e.g. the deployed demo) without changing code.
/// </summary>
public static class TestSettings
{
    /// <summary>E2E_BASE_URL, default: the API serving the built React app locally.</summary>
    public static string BaseUrl =>
        (Environment.GetEnvironmentVariable("E2E_BASE_URL") is { Length: > 0 } url ? url : "http://localhost:5080").TrimEnd('/');

    /// <summary>E2E_ADMIN_KEY, default: the development key from appsettings.Development.json.</summary>
    public static string AdminKey =>
        Environment.GetEnvironmentVariable("E2E_ADMIN_KEY") is { Length: > 0 } key ? key : "dev-admin-key";
}
