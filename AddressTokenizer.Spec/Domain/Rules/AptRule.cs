namespace AddressTokenizer.Domain.Rules;

using static System.Char;

/// <summary>FR-2.1 — token starting with "No " followed by one or more digits (e.g. <c>No 11</c>).</summary>
public sealed class AptRule : IParserRule
{
    public string ComponentName => "apt";

    public bool TryParse(TokenSpan span, ParsedAddress result)
    {
        if (result.Apt is not null)
            return false; // singleton: first occurrence wins (FR-3.3)

        for (var i = 0; i < span.Count; i++)
        {
            if (!MatchesApt(span[i]))
                continue;

            result.Apt = span[i];
            span.Consume(i); // remove from pool (FR-3.2)
            return true;
        }

        return false;
    }

    /// <summary>Equivalent of <c>^No\s+\d+</c> (design.md §3.2).</summary>
    private static bool MatchesApt(string token)
    {
        if (!token.StartsWith("No", StringComparison.Ordinal))
            return false;

        var i = 2;
        if (i >= token.Length || !IsWhiteSpace(token[i]))
            return false; // "No11" does not match

        while (i < token.Length && IsWhiteSpace(token[i]))
            i++;

        var digitsStart = i;
        while (i < token.Length && IsAsciiDigit(token[i]))
            i++;

        return i > digitsStart; // "No abc" does not match
    }
}
