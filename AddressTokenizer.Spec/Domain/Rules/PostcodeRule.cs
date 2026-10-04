namespace AddressTokenizer.Domain.Rules;

using static System.Char;

/// <summary>
/// FR-2.4 — exactly 5 digits, integer value in inclusive range 01000–98859,
/// preserved as the original 5-char string.
/// </summary>
public sealed class PostcodeRule : IParserRule
{
    private const int Min = 1000;    // value of "01000"
    private const int Max = 98859;   // value of "98859"

    public string ComponentName => "postcode";

    public bool TryParse(TokenSpan span, ParsedAddress result)
    {
        if (result.Postcode is not null)
            return false; // singleton (FR-3.3)

        for (var i = 0; i < span.Count; i++)
        {
            var token = span[i];
            if (!IsPostcode(token))
                continue;

            result.Postcode = token; // preserve original string ("01000" stays 5 chars)
            span.Consume(i);
            return true;
        }

        return false;
    }

    private static bool IsPostcode(string token)
    {
        if (token.Length != 5)
            return false; // "1000" (4) and "000001" (6) do not match

        foreach (var c in token)
        {
            if (!IsAsciiDigit(c))
                return false;
        }

        // Safe to parse: 5 ASCII digits → max 99999 fits in int.
        var value = int.Parse(token, System.Globalization.CultureInfo.InvariantCulture);
        return value is >= Min and <= Max; // "00999" and "98860" excluded
    }
}
