using AddressTokenizer.Domain;
using AddressTokenizer.Domain.Rules;

namespace AddressTokenizer.Application;

/// <summary>
/// Builds the Chain of Responsibility with the precedence from FR-3.1:
/// <c>apt &gt; postcode &gt; city &gt; state &gt; street &gt; section</c>.
/// Centralizes chain ordering (design.md §2) so consumers never wire rules directly.
/// </summary>
public static class ParserRuleFactory
{
    public static IReadOnlyList<IParserRule> BuildChain() => new IParserRule[]
    {
        new AptRule(),      // 1
        new PostcodeRule(), // 2
        new CityRule(),     // 3
        new StateRule(),    // 4
        new StreetRule(),   // 5
        new SectionRule(),  // 6 — catch-all, always last
    };
}
