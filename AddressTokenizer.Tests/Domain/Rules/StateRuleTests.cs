using AddressTokenizer.Domain;
using AddressTokenizer.Domain.Rules;

namespace AddressTokenizer.Tests.Domain.Rules;

public class StateRuleTests
{
    private readonly StateRule _rule = new();

    [Theory]
    [InlineData("Terengganu")]
    [InlineData("Pulau Pinang")] // multi-word (T2.5)
    [InlineData("SELANGOR")]     // case-insensitive (FR-2.3)
    [InlineData("johor")]
    public void Matches_state_case_insensitively(string token)
    {
        var span = new TokenSpan(new[] { token });
        var result = new ParsedAddress();

        Assert.True(_rule.TryParse(span, result));
        Assert.Equal(token, result.State);
        Assert.Equal(0, span.Count);
    }

    [Theory]
    [InlineData("Chendering")]
    [InlineData("Pulau")]
    [InlineData("Terengganu.")]
    [InlineData("Kuala Terengganu")] // a city, not a state
    public void Rejects_non_state(string token)
    {
        var span = new TokenSpan(new[] { token });
        var result = new ParsedAddress();

        Assert.False(_rule.TryParse(span, result));
        Assert.Null(result.State);
    }

    [Fact]
    public void Scans_all_tokens_finds_late_state() // FR-3.2a
    {
        var span = new TokenSpan(new[] { "No 11", "Chendering", "Terengganu" });
        var result = new ParsedAddress();

        Assert.True(_rule.TryParse(span, result));
        Assert.Equal("Terengganu", result.State);
        Assert.Equal(new[] { "No 11", "Chendering" }, span.Remaining);
    }
}
