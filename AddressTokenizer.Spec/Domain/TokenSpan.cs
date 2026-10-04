namespace AddressTokenizer.Domain;

/// <summary>
/// Mutable pool of candidate tokens for a single parse. Rules scan
/// <see cref="Remaining"/> and <see cref="Consume"/> matched tokens so they are
/// not re-captured by later rules or <c>section</c> (FR-3.2).
/// </summary>
public sealed class TokenSpan
{
    private readonly List<string> _tokens;

    public TokenSpan(IEnumerable<string>? tokens)
    {
        _tokens = tokens is null
            ? new List<string>()
            : tokens.Where(t => !string.IsNullOrWhiteSpace(t)).ToList();
    }

    /// <summary>Unconsumed tokens, in input order.</summary>
    public IReadOnlyList<string> Remaining => _tokens;

    public int Count => _tokens.Count;

    public string this[int index] => _tokens[index];

    /// <summary>Removes the token at <paramref name="index"/> from the pool (FR-3.2).</summary>
    public void Consume(int index) => _tokens.RemoveAt(index);

    /// <summary>
    /// Removes the first occurrence of <paramref name="token"/> (case-sensitive).
    /// Returns <see langword="false"/> if it is no longer in the pool.
    /// </summary>
    public bool Consume(string token) => _tokens.Remove(token);

    /// <summary>Joins all remaining tokens with a single space — the section value (FR-2.6).</summary>
    public string Remainder() => string.Join(" ", _tokens);

    /// <summary>Empties the pool; returns every remaining token in order.</summary>
    public IReadOnlyList<string> Drain()
    {
        var drained = _tokens.ToArray();
        _tokens.Clear();
        return drained;
    }
}
