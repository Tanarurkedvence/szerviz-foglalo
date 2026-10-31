using ServiceBooking.Api.Services;

namespace ServiceBooking.Tests;

public class LicensePlateRulesTests
{
    [Theory]
    [InlineData("ABC-123")]
    [InlineData("abc123")]
    [InlineData(" abc 123 ")]
    [InlineData("AA AA-123")]
    [InlineData("aaaa123")]
    public void IsValid_AcceptsOldAndNewHungarianFormats(string plate)
    {
        Assert.True(LicensePlateRules.IsValid(plate));
    }

    [Theory]
    [InlineData((string?)null)]
    [InlineData("")]
    [InlineData("AB-123")]      // too few letters
    [InlineData("ABC-1234")]    // too many digits
    [InlineData("123-ABC")]     // wrong order
    [InlineData("AAAAA-123")]   // too many letters
    [InlineData("ÁBC-123")]     // accented letters are not used on plates
    public void IsValid_RejectsInvalidPlates(string? plate)
    {
        Assert.False(LicensePlateRules.IsValid(plate));
    }

    [Theory]
    [InlineData("abc123", "ABC-123")]
    [InlineData("aaaa123", "AA AA-123")]
    [InlineData("AA AA-123", "AA AA-123")]
    public void Format_ProducesTheOfficialLook(string input, string expected)
    {
        Assert.Equal(expected, LicensePlateRules.Format(input));
    }
}
