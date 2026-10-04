using AddressTokenizer.Domain;

namespace AddressTokenizer.Tests.Domain;

/// <summary>Reference data must match spec.md §5 exactly (Phase 1 exit criteria).</summary>
public class ReferenceDataTests
{
    private static readonly string[] SpecCities =
    {
        "Kuala Terengganu", "Kuala Lumpur", "Kajang", "Bangi", "Damansara",
        "Petaling Jaya", "Puchong", "Subang Jaya", "Cyberjaya", "Putrajaya",
        "Mantin", "Kuching", "Seremban",
    };

    private static readonly string[] SpecStates =
    {
        "Selangor", "Terengganu", "Pahang", "Kelantan", "Melaka",
        "Pulau Pinang", "Kedah", "Johor", "Perlis", "Sabah", "Sarawak",
    };

    [Fact]
    public void Cities_contain_exactly_the_13_spec_cities()
    {
        Assert.Equal(13, ReferenceData.Cities.Count);
        foreach (var city in SpecCities)
            Assert.Contains(city, ReferenceData.Cities);
    }

    [Fact]
    public void States_contain_exactly_the_11_spec_states()
    {
        Assert.Equal(11, ReferenceData.States.Count);
        foreach (var state in SpecStates)
            Assert.Contains(state, ReferenceData.States);
    }

    [Fact]
    public void Every_spec_city_resolves_via_IsCity()
    {
        foreach (var city in SpecCities)
            Assert.True(ReferenceData.IsCity(city), $"expected city: {city}");
    }

    [Fact]
    public void Every_spec_state_resolves_via_IsState()
    {
        foreach (var state in SpecStates)
            Assert.True(ReferenceData.IsState(state), $"expected state: {state}");
    }

    [Theory]
    [InlineData("KUALA TERENGGANU")]
    [InlineData("kuala terengganu")]
    [InlineData("PetAlInG jAyA")]
    public void City_lookup_is_case_insensitive(string input) =>
        Assert.True(ReferenceData.IsCity(input));

    [Theory]
    [InlineData("PULAU PINANG")]
    [InlineData("pulau pinang")]
    public void State_lookup_is_case_insensitive(string input) =>
        Assert.True(ReferenceData.IsState(input));

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("Chendering")]
    [InlineData("Kuala Terengganu City")]
    [InlineData("Singapore")]
    public void Unknown_value_does_not_resolve_as_city(string? input) =>
        Assert.False(ReferenceData.IsCity(input));

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("Chendering")]
    [InlineData("Terengganu Darul Iman")]
    public void Unknown_value_does_not_resolve_as_state(string? input) =>
        Assert.False(ReferenceData.IsState(input));

    [Fact]
    public void Street_prefixes_are_the_four_spec_prefixes()
    {
        Assert.Equal(new[] { "Jalan ", "Jln ", "Lorong ", "Persiaran" },
            ReferenceData.StreetPrefixes);
    }
}
