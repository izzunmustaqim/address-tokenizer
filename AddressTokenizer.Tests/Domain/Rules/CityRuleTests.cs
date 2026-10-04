using AddressTokenizer.Domain;
using AddressTokenizer.Domain.Rules;

namespace AddressTokenizer.Tests.Domain.Rules;

public class CityRuleTests
{
    private readonly CityRule _rule = new();

    [Theory]
    [InlineData("Kuala Terengganu")] // multi-word (T2.4)
    [InlineData("Petaling Jaya")]     // multi-word (T2.4)
    [InlineData("Kajang")]
    [InlineData("KUALA TERENGGANU")]   // case-insensitive (FR-2.2)
    [InlineData("kajang")]
    public void Matches_city_case_insensitively(string token)
    {
        var span = new TokenSpan(new[] { token });
        var result = new ParsedAddress();

        Assert.True(_rule.TryParse(span, result));
        Assert.Equal(token, result.City);
        Assert.Equal(0, span.Count);
    }

    [Theory]
    [InlineData("Chendering")]
    [InlineData("Kuala")]
    [InlineData("Kuala Terengganu City")]
    [InlineData("Singapore")]
    [InlineData("")]
    public void Rejects_non_city(string token)
    {
        var span = new TokenSpan(new[] { token });
        var result = new ParsedAddress();

        Assert.False(_rule.TryParse(span, result));
        Assert.Null(result.City);
    }

    [Fact]
    public void Scans_all_tokens_finds_late_city() // FR-3.2a — AS-2: section text before city
    {
        var span = new TokenSpan(new[] { "Chendering", "Kuala Terengganu" });
        var result = new ParsedAddress();

        Assert.True(_rule.TryParse(span, result));
        Assert.Equal("Kuala Terengganu", result.City);
        Assert.Equal(new[] { "Chendering" }, span.Remaining);
    }

    [Fact]
    public void Second_city_stays_in_pool_for_section() // FR-3.3
    {
        var span = new TokenSpan(new[] { "Kajang", "Puchong" });
        var result = new ParsedAddress();

        Assert.True(_rule.TryParse(span, result));
        Assert.False(_rule.TryParse(span, result));
        Assert.Equal("Kajang", result.City);
        Assert.Equal(new[] { "Puchong" }, span.Remaining);
    }
}
