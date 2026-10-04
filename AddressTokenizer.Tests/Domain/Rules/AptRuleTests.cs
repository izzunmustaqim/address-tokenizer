using AddressTokenizer.Domain;
using AddressTokenizer.Domain.Rules;

namespace AddressTokenizer.Tests.Domain.Rules;

public class AptRuleTests
{
    private readonly AptRule _rule = new();

    [Theory]
    [InlineData("No 11")]
    [InlineData("No 1")]
    [InlineData("No 123456")]
    [InlineData("No  11")] // multiple spaces still ^No\s+\d+
    public void Matches_No_followed_by_digits(string token)
    {
        var span = new TokenSpan(new[] { token });
        var result = new ParsedAddress();

        Assert.True(_rule.TryParse(span, result));
        Assert.Equal(token, result.Apt);
        Assert.Equal(0, span.Count); // consumed (FR-3.2)
    }

    [Theory]
    [InlineData("No abc")]   // T2.2 ✗
    [InlineData("No11")]     // T2.2 ✗ — no space
    [InlineData("No ")]      // no digits
    [InlineData("No")]       // too short
    [InlineData("Numero 11")]// not the "No " prefix
    [InlineData("no 11")]    // case-sensitive per design regex ^No
    public void Does_not_match_malformed_apt(string token)
    {
        var span = new TokenSpan(new[] { token });
        var result = new ParsedAddress();

        Assert.False(_rule.TryParse(span, result));
        Assert.Null(result.Apt);
        Assert.Equal(1, span.Count); // not consumed → falls through to section
    }

    [Fact]
    public void Scans_all_tokens_not_just_the_head() // FR-3.2a
    {
        var span = new TokenSpan(new[] { "Chendering", "21080", "No 11" });
        var result = new ParsedAddress();

        Assert.True(_rule.TryParse(span, result));
        Assert.Equal("No 11", result.Apt);
        Assert.Equal(new[] { "Chendering", "21080" }, span.Remaining);
    }

    [Fact]
    public void Second_apt_is_ignored_once_first_is_set() // FR-3.3
    {
        var span = new TokenSpan(new[] { "No 22" });
        var result = new ParsedAddress { Apt = "No 11" };

        Assert.False(_rule.TryParse(span, result));
        Assert.Equal("No 11", result.Apt);
        Assert.Equal(1, span.Count); // duplicate left for section
    }
}
