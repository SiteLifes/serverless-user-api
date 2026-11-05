using Domain.Services;
using Xunit;

namespace User.Tests;

public class TurkishIdentityNumberTests
{
    [Theory]
    [InlineData("10000000146")]
    [InlineData("11111111110")]
    public void Accepts_valid_numbers(string value) => Assert.True(TurkishIdentityNumber.IsValid(value));

    [Theory]
    [InlineData("10000000147")] // son hane yanlış
    [InlineData("10000000156")] // 10. hane yanlış
    [InlineData("01111111110")] // 0 ile başlıyor
    [InlineData("1000000014")] // 10 hane
    [InlineData("1000000014a")]
    [InlineData("")]
    [InlineData(null)]
    public void Rejects_invalid_numbers(string? value) => Assert.False(TurkishIdentityNumber.IsValid(value));

    [Fact]
    public void Normalize_strips_spaces_and_blanks_become_null()
    {
        Assert.Equal("10000000146", TurkishIdentityNumber.Normalize(" 100 000 001 46 "));
        Assert.Null(TurkishIdentityNumber.Normalize("   "));
        Assert.Null(TurkishIdentityNumber.Normalize(null));
    }
}
