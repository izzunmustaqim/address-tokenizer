using AddressTokenizer.Domain;
using AddressTokenizer.Domain.Rules;

namespace AddressTokenizer.Tests.Domain.Rules;

public class SectionRuleTests
{
    private readonly SectionRule _rule = new();

    [Fact]
    public void Absorbs_all_remaining_tokens_joined_with_single_space() // FR-2.6
    {
        var span = new TokenSpan(new[] { "Taman", "Impian", "Indah" });
        var result = new ParsedAddress();

        Assert.True(_rule.TryParse(span, result));
        Assert.Equal("Taman Impian Indah", result.Section);
        Assert.Equal(0, span.Count); // pool drained
    }

    [Fact]
    public void Single_token_becomes_section()
    {
        var span = new TokenSpan(new[] { "Chendering" });
        var result = new ParsedAddress();

        Assert.True(_rule.TryParse(span, result));
        Assert.Equal("Chendering", result.Section);
    }

    [Fact]
    public void Empty_pool_returns_false_so_no_section_key() // FR-4.1
    {
        var span = new TokenSpan(Array.Empty<string>());
        var result = new ParsedAddress();

        Assert.False(_rule.TryParse(span, result));
        Assert.Null(result.Section);
        Assert.True(result.IsEmpty);
    }
}
