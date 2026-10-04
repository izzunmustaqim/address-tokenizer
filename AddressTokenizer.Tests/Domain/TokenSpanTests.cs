using AddressTokenizer.Domain;

namespace AddressTokenizer.Tests.Domain;

public class TokenSpanTests
{
    [Fact]
    public void Null_input_yields_empty_span()
    {
        var span = new TokenSpan(null);
        Assert.Equal(0, span.Count);
        Assert.Equal(string.Empty, span.Remainder());
    }

    [Fact]
    public void Whitespace_only_tokens_are_dropped()
    {
        var span = new TokenSpan(new[] { "  ", "No 11", "", "\t" });
        Assert.Equal(new[] { "No 11" }, span.Remaining);
    }

    [Fact]
    public void Consume_by_index_removes_token_from_pool()
    {
        var span = new TokenSpan(new[] { "No 11", "Chendering" });
        span.Consume(0);
        Assert.Equal(new[] { "Chendering" }, span.Remaining);
    }

    [Fact]
    public void Consumed_token_is_not_in_Remainder_so_section_cannot_recapture_it()
    {
        var span = new TokenSpan(new[] { "No 11", "Chendering", "21080" });
        span.Consume("21080");
        Assert.Equal("No 11 Chendering", span.Remainder());
    }

    [Fact]
    public void Consume_missing_token_returns_false()
    {
        var span = new TokenSpan(new[] { "Chendering" });
        Assert.False(span.Consume("21080"));
        Assert.Equal(new[] { "Chendering" }, span.Remaining);
    }

    [Fact]
    public void Remainder_joins_with_single_space()
    {
        var span = new TokenSpan(new[] { "Somewhere", "Nice", "Elsewhere" });
        Assert.Equal("Somewhere Nice Elsewhere", span.Remainder());
    }

    [Fact]
    public void Drain_returns_remaining_in_order_and_empties_the_pool()
    {
        var span = new TokenSpan(new[] { "a", "b", "c" });
        var drained = span.Drain();
        Assert.Equal(new[] { "a", "b", "c" }, drained);
        Assert.Equal(0, span.Count);
        Assert.Equal(string.Empty, span.Remainder());
    }
}
