namespace KundaliEngine.API.Services.Locations;

public sealed class LocationSearchResult
{
    public string Id { get; init; } = string.Empty;
    public string Name { get; init; } = string.Empty;
    public string DisplayName { get; init; } = string.Empty;
    public double Latitude { get; init; }
    public double Longitude { get; init; }
    public string? City { get; init; }
    public string? State { get; init; }
    public string? Country { get; init; }
    public string? CountryCode { get; init; }
    public string? Timezone { get; init; }
}
