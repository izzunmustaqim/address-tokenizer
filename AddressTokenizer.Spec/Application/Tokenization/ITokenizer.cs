namespace AddressTokenizer.Application.Tokenization;

/// <summary>
/// Strategy for turning raw address text into an ordered token list
/// (design.md §2, Strategy). The rule chain that consumes the tokens is
/// independent of <i>how</i> they were split, so tokenization modes can be
/// added or swapped without touching the rules (OCP, DIP).
/// </summary>
/// <remarks>
/// Implementations encode the mode differences from design.md §3.3:
/// <list type="bullet">
///   <item><c>CommaTokenizer</c> — split on <c>,</c> <c>;</c> newline into
///   hard segments, then longest-match scan each segment.</item>
///   <item>Comma-less mode — the same longest-match scanner over the whole
///   input with no segment delimiters.</item>
/// </list>
/// Contract: implementations must never throw (design.md §6).
/// </remarks>
public interface ITokenizer
{
    /// <summary>
    /// Splits <paramref name="input"/> into ordered tokens, normalized per
    /// FR-1.5: whitespace collapsed and trailing punctuation
    /// (<c>.</c> <c>,</c> <c>;</c> <c>:</c>) stripped from every token before
    /// rule matching, in both modes.
    /// </summary>
    /// <param name="input">Raw address line; may be <see langword="null"/>, empty, or whitespace-only.</param>
    /// <returns>
    /// Ordered tokens; an empty list when <paramref name="input"/> has no
    /// tokenizable content (FR-1.3 — never <see langword="null"/>).
    /// </returns>
    IReadOnlyList<string> Tokenize(string? input);
}
