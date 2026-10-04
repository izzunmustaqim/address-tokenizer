using AddressTokenizer.Domain;
using AddressTokenizer.Domain.Rules;

namespace AddressTokenizer.Tests.Domain.Rules;

public class StreetRuleTests
{
    private readonly StreetRule _rule = new();

    [Theory]
    [InlineData("Jalan Chendering")]
    [InlineData("Jln Chendering")]
    [InlineData("Lorong 3")]
    [InlineData("Persiaran")]       // end-of-token ✓ (T2.6)
    [InlineData("Persiaran Utama")] // followed by space ✓ (T2.6)
    [InlineData("jalan chendering")]// case variants (T2.6)
    [InlineData("LORONG 1")]
    public void Matches_street_prefixes(string token)
    {
        var span = new TokenSpan(new[] { token });
        var result = new ParsedAddress();

        Assert.True(_rule.TryParse(span, result));
        Assert.Equal(token, result.Street);
        Assert.Equal(0, span.Count);
    }

    [Theory]
    [InlineData("PersiaranFoo")] // ✗ not end-of-token nor space (T2.6)
    [InlineData("PersiaranFoo Bar")]
    [InlineData("Jalan")]        // missing trailing space
    [InlineData("Jalanx Chendering")] // prefix must be "Jalan "
    [InlineData("Jln")]          // missing trailing space
    [InlineData("Highway 1")]
    [InlineData("Chendering")]
    public void Rejects_non_street(string token)
    {
        var span = new TokenSpan(new[] { token });
        var result = new ParsedAddress();

        Assert.False(_rule.TryParse(span, result));
        Assert.Null(result.Street);
        Assert.Equal(1, span.Count); // left for section
    }

    [Fact]
    public void Scans_all_tokens_finds_street_after_section_text() // FR-3.2a
    {
        var span = new TokenSpan(new[] { "Taman Impian", "Jalan Chendering" });
        var result = new ParsedAddress();

        Assert.True(_rule.TryParse(span, result));
        Assert.Equal("Jalan Chendering", result.Street);
        Assert.Equal(new[] { "Taman Impian" }, span.Remaining);
    }

    [Fact]
    public void Duplicate_street_left_in_pool() // FR-3.3
    {
        var span = new TokenSpan(new[] { "Jalan A", "Jalan B" });
        var result = new ParsedAddress();

        Assert.True(_rule.TryParse(span, result));
        Assert.False(_rule.TryParse(span, result));
        Assert.Equal(new[] { "Jalan B" }, span.Remaining);
    }
}
