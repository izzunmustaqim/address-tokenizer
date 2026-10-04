namespace AddressTokenizer.Domain;

/// <summary>
/// Fixed reference lists from spec.md §5. Read-only and case-insensitive (FR-2.2, FR-2.3).
/// </summary>
public static class ReferenceData
{
    /// <summary>spec.md §5.1 — 13 cities.</summary>
    public static readonly IReadOnlySet<string> Cities =
        new HashSet<string>(StringComparer.OrdinalIgnoreCase)
        {
            "Kuala Terengganu",
            "Kuala Lumpur",
            "Kajang",
            "Bangi",
            "Damansara",
            "Petaling Jaya",
            "Puchong",
            "Subang Jaya",
            "Cyberjaya",
            "Putrajaya",
            "Mantin",
            "Kuching",
            "Seremban",
        };

    /// <summary>spec.md §5.2 — 11 states.</summary>
    public static readonly IReadOnlySet<string> States =
        new HashSet<string>(StringComparer.OrdinalIgnoreCase)
        {
            "Selangor",
            "Terengganu",
            "Pahang",
            "Kelantan",
            "Melaka",
            "Pulau Pinang",
            "Kedah",
            "Johor",
            "Perlis",
            "Sabah",
            "Sarawak",
        };

    /// <summary>
    /// spec.md §5.3 — street prefixes. The first three require the trailing space;
    /// <c>Persiaran</c> also matches at end-of-token (FR-2.5) — that nuance lives in StreetRule.
    /// </summary>
    public static readonly IReadOnlyList<string> StreetPrefixes =
        new[]
        {
            "Jalan ",
            "Jln ",
            "Lorong ",
            "Persiaran",
        };

    public static bool IsCity(string? token) =>
        token is not null && Cities.Contains(token);

    public static bool IsState(string? token) =>
        token is not null && States.Contains(token);
}
