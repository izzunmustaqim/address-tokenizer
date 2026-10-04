namespace AddressTokenizer.Domain.Rules;

/// <summary>FR-2.2 — case-insensitive exact match against ReferenceData.Cities.</summary>
public sealed class CityRule : IParserRule
{
    public string ComponentName => "city";

    public bool TryParse(TokenSpan span, ParsedAddress result)
    {
        if (result.City is not null)
            return false; // singleton (FR-3.3)

        for (var i = 0; i < span.Count; i++)
        {
            if (!ReferenceData.IsCity(span[i]))
                continue;

            result.City = span[i];
            span.Consume(i);
            return true;
        }

        return false;
    }
}
