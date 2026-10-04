namespace AddressTokenizer.Domain;

/// <summary>
/// Result of parsing one address. A <c>null</c> property means "component not
/// found"; absent components are omitted from the JSON output (FR-4.1).
/// Member order is canonical: apt, section, postcode, city, state, street (FR-4.2).
/// </summary>
public sealed class ParsedAddress
{
    public string? Apt { get; set; }
    public string? Section { get; set; }
    public string? Postcode { get; set; }
    public string? City { get; set; }
    public string? State { get; set; }
    public string? Street { get; set; }

    /// <summary>True when no component was found (serializes as <c>{}</c>, FR-5.1).</summary>
    public bool IsEmpty =>
        Apt is null && Section is null && Postcode is null &&
        City is null && State is null && Street is null;
}
