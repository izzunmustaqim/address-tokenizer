namespace AddressTokenizer.Domain.Rules;

/// <summary>
/// FR-2.5 — token beginning with "Jalan ", "Jln ", "Lorong ", or "Persiaran".
/// "Persiaran" matches only at end-of-token or when followed by a space
/// (so "Persiaran Utama" matches; "PersiaranFoo" does not).
/// </summary>
public sealed class StreetRule : IParserRule
{
    private const string Persiaran = "Persiaran";

    public string ComponentName => "street";

    public bool TryParse(TokenSpan span, ParsedAddress result)
    {
        if (result.Street is not null)
            return false; // singleton (FR-3.3)

        for (var i = 0; i < span.Count; i++)
        {
            if (!IsStreet(span[i]))
                continue;

            result.Street = span[i];
            span.Consume(i);
            return true;
        }

        return false;
    }

    private static bool IsStreet(string token) =>
        token.StartsWith("Jalan ", StringComparison.OrdinalIgnoreCase)
        || token.StartsWith("Jln ", StringComparison.OrdinalIgnoreCase)
        || token.StartsWith("Lorong ", StringComparison.OrdinalIgnoreCase)
        || IsPersiaran(token);

    private static bool IsPersiaran(string token)
    {
        if (!token.StartsWith(Persiaran, StringComparison.OrdinalIgnoreCase))
            return false;

        // End-of-token ("Persiaran") or followed by a space ("Persiaran Utama").
        return token.Length == Persiaran.Length
            || char.IsWhiteSpace(token[Persiaran.Length]);
    }
}
