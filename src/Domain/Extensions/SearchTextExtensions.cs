using System.Text;

namespace Domain.Extensions;

/// <summary>
/// Turkish-aware text folding for name search.
///
/// Names are stored the way people wrote them — "Şefer", "İBRAHİM", "Gülşen" — and staff type them
/// without the diacritics and in whatever case is quickest. Folding both sides to plain lowercase
/// ASCII is what makes "sefer" find "Şefer".
///
/// The Turkish letters are mapped by hand rather than through a culture: the service runs in a
/// Lambda whose globalization support is not something this code should depend on, and the mapping
/// is short enough to state outright.
/// </summary>
public static class SearchTextExtensions
{
    /// <summary>Folds a string to lowercase ASCII with single spaces between words.</summary>
    public static string ToSearchText(this string? value)
    {
        if (string.IsNullOrWhiteSpace(value))
            return string.Empty;

        var builder = new StringBuilder(value.Length);
        var pendingSpace = false;

        foreach (var character in value)
        {
            if (char.IsWhiteSpace(character))
            {
                // Held back rather than written, so trailing space never reaches the result.
                pendingSpace = builder.Length > 0;
                continue;
            }

            if (pendingSpace)
            {
                builder.Append(' ');
                pendingSpace = false;
            }

            builder.Append(Fold(character));
        }

        return builder.ToString();
    }

    /// <summary>
    /// The words of a search term, folded. Each one has to appear somewhere in the text for a
    /// match, so "yılmaz ahmet" finds Ahmet Yılmaz just as "ahmet yilmaz" does.
    /// </summary>
    public static IReadOnlyList<string> ToSearchTerms(this string? value)
    {
        var normalized = value.ToSearchText();

        return normalized.Length == 0
            ? Array.Empty<string>()
            : normalized.Split(' ', StringSplitOptions.RemoveEmptyEntries);
    }

    /// <summary>Whether every term appears in the text. An empty term list matches nothing.</summary>
    public static bool MatchesSearchTerms(this string? value, IReadOnlyList<string> terms)
    {
        if (terms.Count == 0)
            return false;

        var haystack = value.ToSearchText();

        return terms.All(term => haystack.Contains(term, StringComparison.Ordinal));
    }

    private static char Fold(char character) => character switch
    {
        'ı' or 'I' or 'İ' or 'î' or 'Î' => 'i',
        'ş' or 'Ş' => 's',
        'ğ' or 'Ğ' => 'g',
        'ü' or 'Ü' or 'û' or 'Û' => 'u',
        'ö' or 'Ö' => 'o',
        'ç' or 'Ç' => 'c',
        'â' or 'Â' => 'a',
        _ => char.ToLowerInvariant(character)
    };
}
