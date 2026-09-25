namespace NoMoreThaiNums.Conversion;

/// <summary>
/// Converts Thai numerals (U+0E50–U+0E59) into Western/ASCII digits (U+0030–U+0039).
/// Pure logic, no Windows dependencies, unit-testable.
/// </summary>
public static class ThaiDigitConverter
{
    /// <summary>First Thai digit character: ๐ (U+0E50).</summary>
    private const char ThaiZero = '\u0E50';

    /// <summary>Last Thai digit character: ๙ (U+0E59).</summary>
    private const char ThaiNine = '\u0E59';

    /// <summary>
    /// Returns the ASCII digit equivalent of a Thai digit, or null when
    /// <paramref name="c"/> is not a Thai digit.
    /// </summary>
    public static char? TryConvert(char c)
    {
        if (c >= ThaiZero && c <= ThaiNine)
        {
            return (char)(c - ThaiZero + '0');
        }

        return null;
    }

    /// <summary>
    /// Converts every Thai numeral in <paramref name="text"/> to its ASCII digit
    /// equivalent and leaves all other characters (Thai letters, vowels, tone
    /// marks, Western text, punctuation) untouched. Returns the input string
    /// instance itself when no conversion is needed, to avoid allocations.
    /// </summary>
    public static string Convert(string text)
    {
        if (string.IsNullOrEmpty(text) || !ContainsThaiDigit(text))
        {
            return text;
        }

        return string.Create(text.Length, text, static (span, source) =>
        {
            for (int i = 0; i < source.Length; i++)
            {
                char c = source[i];
                span[i] = c >= ThaiZero && c <= ThaiNine
                    ? (char)(c - ThaiZero + '0')
                    : c;
            }
        });
    }

    /// <summary>True when <paramref name="text"/> contains at least one Thai numeral.</summary>
    public static bool ContainsThaiDigit(string text)
    {
        if (string.IsNullOrEmpty(text))
        {
            return false;
        }

        foreach (char c in text)
        {
            if (c >= ThaiZero && c <= ThaiNine)
            {
                return true;
            }
        }

        return false;
    }
}
