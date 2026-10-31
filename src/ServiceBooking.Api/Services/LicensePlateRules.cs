using System.Text.RegularExpressions;

namespace ServiceBooking.Api.Services;

/// <summary>
/// Hungarian licence plates: old format "ABC-123", new format (since 2022) "AA AA-123".
/// </summary>
public static class LicensePlateRules
{
    private static readonly Regex OldFormat = new(@"^[A-Z]{3}[0-9]{3}$", RegexOptions.Compiled);
    private static readonly Regex NewFormat = new(@"^[A-Z]{4}[0-9]{3}$", RegexOptions.Compiled);

    /// <summary>"abc-123 " -> "ABC123": drops spaces and dashes, upper-cases.</summary>
    public static string Normalize(string? input)
    {
        var lettersAndDigits = (input ?? "").Where(char.IsLetterOrDigit).ToArray();
        return new string(lettersAndDigits).ToUpperInvariant();
    }

    public static bool IsValid(string? input)
    {
        var plate = Normalize(input);
        return OldFormat.IsMatch(plate) || NewFormat.IsMatch(plate);
    }

    /// <summary>"ABC123" -> "ABC-123", "AAAA123" -> "AA AA-123".</summary>
    public static string Format(string? input)
    {
        var plate = Normalize(input);

        if (OldFormat.IsMatch(plate))
        {
            return $"{plate[..3]}-{plate[3..]}";
        }

        if (NewFormat.IsMatch(plate))
        {
            return $"{plate[..2]} {plate[2..4]}-{plate[4..]}";
        }

        return plate;
    }
}
