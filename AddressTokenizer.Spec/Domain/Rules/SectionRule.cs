namespace AddressTokenizer.Domain.Rules;

/// <summary>
/// FR-2.6 — catch-all: every remaining unconsumed token, joined with a single space.
/// Not a singleton; runs last in the chain (FR-3.1) and absorbs all leftovers (FR-3.3).
/// </summary>
public sealed class SectionRule : IParserRule
{
    public string ComponentName => "section";

    public bool TryParse(TokenSpan span, ParsedAddress result)
    {
        if (span.Count == 0)
            return false; // nothing left → no section key (FR-4.1)

        result.Section = span.Remainder();
        span.Drain();
        return true;
    }
}
