using Domain.Extensions;
using Xunit;

namespace User.Tests;

/// <summary>
/// Staff type names the fast way — no diacritics, whatever case — while the records hold them the
/// way people wrote them. These pin the folding that has to bridge the two, the Turkish letters
/// above all: matching "Şefer" for "sefer" is the whole point of searching by name.
/// </summary>
public class SearchTextTests
{
    [Theory]
    [InlineData("Şefer", "sefer")]
    [InlineData("İBRAHİM", "ibrahim")]
    [InlineData("Gülşen", "gulsen")]
    [InlineData("Irmak", "irmak")]
    [InlineData("Ismail", "ismail")]
    [InlineData("Çağrı", "cagri")]
    [InlineData("  Ayşe   Öztürk ", "ayse ozturk")]
    public void Folding_reduces_a_name_to_plain_lowercase_ascii(string input, string expected)
    {
        Assert.Equal(expected, input.ToSearchText());
    }

    [Theory]
    [InlineData("sefer")]
    [InlineData("ŞEFER")]
    [InlineData("şef")]
    [InlineData("bulbul")]
    [InlineData("bülbül sefer")]
    public void A_name_is_found_however_the_term_is_typed(string term)
    {
        Assert.True("Şefer Bülbül".MatchesSearchTerms(term.ToSearchTerms()));
    }

    [Fact]
    public void Every_word_of_the_term_has_to_appear()
    {
        Assert.False("Şefer Bülbül".MatchesSearchTerms("sefer yilmaz".ToSearchTerms()));
    }

    [Fact]
    public void An_empty_term_matches_nothing()
    {
        // Otherwise a blank search would quietly return the whole user list as "results".
        Assert.Empty("   ".ToSearchTerms());
        Assert.False("Şefer Bülbül".MatchesSearchTerms("   ".ToSearchTerms()));
    }
}
