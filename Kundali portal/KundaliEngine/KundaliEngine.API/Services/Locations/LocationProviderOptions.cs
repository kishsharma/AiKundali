namespace KundaliEngine.API.Services.Locations;

public sealed class LocationProviderOptions
{
    public string Provider { get; set; } = "Nominatim";
    public string BaseUrl { get; set; } = "https://nominatim.openstreetmap.org";
    public string DefaultCountryCode { get; set; } = "IN";
    public int ResultLimit { get; set; } = 5;
    public int TimeoutSeconds { get; set; } = 8;
    public int CacheMinutes { get; set; } = 60;
    public string UserAgent { get; set; } = "HoraLife/1.0";
}
