using AddressTokenizer.Application;
using AddressTokenizer.Domain;
using AddressTokenizer.Domain.Rules;

namespace AddressTokenizer.Tests.Application;

public class ParserRuleFactoryTests
{
    [Fact]
    public void BuildChain_encodes_precedence_from_FR_3_1()
    {
        var chain = ParserRuleFactory.BuildChain();

        Assert.Equal(
            new[] { "apt", "postcode", "city", "state", "street", "section" },
            chain.Select(r => r.ComponentName).ToArray());
        Assert.IsType<AptRule>(chain[0]);
        Assert.IsType<PostcodeRule>(chain[1]);
        Assert.IsType<CityRule>(chain[2]);
        Assert.IsType<StateRule>(chain[3]);
        Assert.IsType<StreetRule>(chain[4]);
        Assert.IsType<SectionRule>(chain[5]); // catch-all always last
    }

    [Fact]
    public void Chain_runs_in_order_consumes_matches_and_leaves_leftovers_for_section()
    {
        var span = new TokenSpan(new[] { "No 11", "Chendering", "21080", "Kuala Terengganu", "Terengganu" });
        var result = new ParsedAddress();

        foreach (var rule in ParserRuleFactory.BuildChain())
            rule.TryParse(span, result);

        Assert.Equal("No 11", result.Apt);
        Assert.Equal("21080", result.Postcode);
        Assert.Equal("Kuala Terengganu", result.City);
        Assert.Equal("Terengganu", result.State);
        Assert.Equal("Chendering", result.Section); // only the leftover
        Assert.Null(result.Street);
        Assert.Equal(0, span.Count);
    }

    [Fact]
    public void BuildChain_returns_fresh_instances_each_call()
    {
        var first = ParserRuleFactory.BuildChain();
        var second = ParserRuleFactory.BuildChain();

        Assert.NotSame(first, second);
        for (var i = 0; i < first.Count; i++)
            Assert.NotSame(first[i], second[i]);
    }
}
