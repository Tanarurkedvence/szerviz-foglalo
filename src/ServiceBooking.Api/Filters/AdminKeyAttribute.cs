using System.Security.Cryptography;
using System.Text;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Filters;
using ServiceBooking.Api.Contracts;

namespace ServiceBooking.Api.Filters;

/// <summary>
/// Protects workshop-only endpoints: the request must send the X-Admin-Key header
/// with the value configured as "AdminKey". If no key is configured, nobody gets in.
/// </summary>
[AttributeUsage(AttributeTargets.Class | AttributeTargets.Method)]
public class AdminKeyAttribute : Attribute, IAuthorizationFilter
{
    public const string HeaderName = "X-Admin-Key";

    public void OnAuthorization(AuthorizationFilterContext context)
    {
        var configuration = context.HttpContext.RequestServices.GetRequiredService<IConfiguration>();
        var expected = configuration["AdminKey"];
        var provided = context.HttpContext.Request.Headers[HeaderName].ToString();

        if (string.IsNullOrEmpty(expected) || !KeysMatch(provided, expected))
        {
            context.Result = new UnauthorizedObjectResult(new ErrorResponse(new[] { "Hiányzó vagy hibás admin kulcs." }));
        }
    }

    // Constant-time comparison, so the key cannot be guessed by measuring response times.
    private static bool KeysMatch(string provided, string expected)
    {
        return CryptographicOperations.FixedTimeEquals(
            Encoding.UTF8.GetBytes(provided),
            Encoding.UTF8.GetBytes(expected));
    }
}
