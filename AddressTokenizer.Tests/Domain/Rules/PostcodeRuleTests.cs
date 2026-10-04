using AddressTokenizer.Domain;
using AddressTokenizer.Domain.Rules;

namespace AddressTokenizer.Tests.Domain.Rules;

public class PostcodeRuleTests
{
    private readonly PostcodeRule _rule = new();

    [Theory]
    [InlineData("01000")] // boundary: min ✓ (T2.3)
    [InlineData("98859")] // boundary: max ✓
    [InlineData("21080")]
    [InlineData("12345")]
    public void Matches_exactly_5_digits_in_range(string token)
    {
        var span = new TokenSpan(new[] { token });
        var result = new ParsedAddress();

        Assert.True(_rule.TryParse(span, result));
        Assert.Equal(token, result.Postcode); // original string preserved (FR-2.4)
        Assert.Equal(0, span.Count);
    }

    [Theory]
    [InlineData("00999")]   // ✗ value < 01000 (T2.3)
    [InlineData("98860")]   // ✗ value > 98859 (T2.3)
    [InlineData("99999")]   // ✗ out of range (spec FR-2.4)
    [InlineData("1000")]    // ✗ 4 digits (T2.3)
    [InlineData("000001")]  // ✗ 6 digits (T2.3)
    [InlineData("999999")]  // ✗ malformed (spec FR-5.3)
    [InlineData("abcde")]   // ✗ non-digits
    [InlineData("2108")]    // ✗ 4 digits
    public void Rejects_out_of_range_or_wrong_length(string token)
    {
        var span = new TokenSpan(new[] { token });
        var result = new ParsedAddress();

        Assert.False(_rule.TryParse(span, result));
        Assert.Null(result.Postcode);
        Assert.Equal(1, span.Count); // falls through to section (FR-5.3)
    }

    [Fact]
    public void Preserves_leading_zero_as_5_char_string()
    {
        var span = new TokenSpan(new[] { "01000" });
        var result = new ParsedAddress();

        Assert.True(_rule.TryParse(span, result));
        Assert.Equal("01000", result.Postcode);
        Assert.Equal(5, result.Postcode!.Length);
    }

    [Fact]
    public void Scans_all_tokens_not_just_the_head() // FR-3.2a
    {
        var span = new TokenSpan(new[] { "Kuala Terengganu", "21080", "Terengganu" });
        var result = new ParsedAddress();

        Assert.True(_rule.TryParse(span, result));
        Assert.Equal("21080", result.Postcode);
        Assert.Equal(new[] { "Kuala Terengganu", "Terengganu" }, span.Remaining);
    }

    [Fact]
    public void First_occurrence_wins_duplicate_left_in_pool() // FR-3.3
    {
        var span = new TokenSpan(new[] { "21080", "43000" });
        var result = new ParsedAddress();

        Assert.True(_rule.TryParse(span, result));
        Assert.Equal("21080", result.Postcode);
        Assert.False(_rule.TryParse(span, result)); // already set
        Assert.Equal(new[] { "43000" }, span.Remaining);
    }
}
