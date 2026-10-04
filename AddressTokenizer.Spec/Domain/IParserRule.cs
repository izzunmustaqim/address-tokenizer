namespace AddressTokenizer.Domain;

/// <summary>
/// One link in the Chain of Responsibility (design.md §3.1). A rule scans
/// <b>all</b> remaining unconsumed tokens in the span (FR-3.2a), and on success
/// writes into <paramref name="result"/> and consumes the matched token so it
/// cannot be re-captured (FR-3.2).
/// Contract: implementations must never throw (design.md §6).
/// </summary>
public interface IParserRule
{
    /// <summary>JSON key this rule produces, e.g. "apt" (FR-4.2).</summary>
    string ComponentName { get; }

    /// <summary>
    /// Attempts to populate this rule's component from the span.
    /// Returns <see langword="true"/> if a value was assigned (singleton rules:
    /// first match wins, FR-3.3).
    /// </summary>
    bool TryParse(TokenSpan span, ParsedAddress result);
}
