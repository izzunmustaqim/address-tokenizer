namespace AddressTokenizer.Domain.Rules;

/// <summary>FR-2.3 — case-insensitive exact match against ReferenceData.States.</summary>
public sealed class StateRule : IParserRule
{
    public string ComponentName => "state";

    public bool TryParse(TokenSpan span, ParsedAddress result)
    {
        if (result.State is not null)
            return false; // singleton (FR-3.3)

        for (var i = 0; i < span.Count; i++)
        {
            if (!ReferenceData.IsState(span[i]))
                continue;

            result.State = span[i];
            span.Consume(i);
            return true;
        }

        return false;
    }
}
