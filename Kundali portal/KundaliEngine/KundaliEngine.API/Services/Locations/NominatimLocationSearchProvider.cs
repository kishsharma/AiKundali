using System.Globalization;
using System.Net.Http.Json;
using System.Text.Json.Serialization;

namespace KundaliEngine.API.Services.Locations;

public sealed class NominatimLocationSearchProvider : ILocationSearchProvider
{
    private readonly HttpClient _httpClient;
    private readonly ILogger<NominatimLocationSearchProvider> _logger;

    public NominatimLocationSearchProvider( HttpClient httpClient, ILogger<NominatimLocationSearchProvider> logger)
    {
        _httpClient = httpClient;
        _logger = logger;
    }

    public async Task<IReadOnlyList<LocationSearchResult>> SearchAsync( string query, string countryCode, int limit, CancellationToken cancellationToken)
    {
        var requestUri =
            $"search?q={Uri.EscapeDataString(query)}&countrycodes={Uri.EscapeDataString(countryCode.ToLowerInvariant())}" +
            $"&format=json&addressdetails=1&limit={limit}";

        try
        {
            var response = await _httpClient.GetFromJsonAsync<List<NominatimResult>>(requestUri, cancellationToken);
            return response?
                .Select(Map)
                .Where(result => result != null)
                .Cast<LocationSearchResult>()
                .ToList() ?? [];
        }
        catch (OperationCanceledException) when (!cancellationToken.IsCancellationRequested)
        {
            _logger.LogWarning("Location provider timed out for query {Query}.", query);
            return [];
        }
        catch (HttpRequestException exception)
        {
            _logger.LogWarning(exception, "Location provider failed for query {Query}.", query);
            return [];
        }
    }

    private static LocationSearchResult? Map(NominatimResult source)
    {
        if (!double.TryParse(source.Latitude, NumberStyles.Float, CultureInfo.InvariantCulture, out var latitude) ||
            !double.TryParse(source.Longitude, NumberStyles.Float, CultureInfo.InvariantCulture, out var longitude))
        {
            return null;
        }

        var address = source.Address;
        var city = FirstNotEmpty(address?.City, address?.Town, address?.Village, address?.Municipality, address?.County);
        var name = FirstNotEmpty(source.Name, city, source.DisplayName?.Split(',').FirstOrDefault()) ?? source.DisplayName ?? string.Empty;
        var countryCode = address?.CountryCode?.ToUpperInvariant();

        return new LocationSearchResult
        {
            Id = source.PlaceId?.ToString(CultureInfo.InvariantCulture) ?? $"{latitude:F6},{longitude:F6}",
            Name = name,
            DisplayName = source.DisplayName ?? name,
            Latitude = latitude,
            Longitude = longitude,
            City = city,
            State = address?.State,
            Country = address?.Country,
            CountryCode = countryCode,
            Timezone = string.Equals(countryCode, "IN", StringComparison.OrdinalIgnoreCase) ? "Asia/Kolkata" : null
        };
    }

    private static string? FirstNotEmpty(params string?[] values) =>
        values.FirstOrDefault(value => !string.IsNullOrWhiteSpace(value));

    private sealed class NominatimResult
    {
        [JsonPropertyName("place_id")]
        public long? PlaceId { get; init; }

        [JsonPropertyName("name")]
        public string? Name { get; init; }

        [JsonPropertyName("display_name")]
        public string? DisplayName { get; init; }

        [JsonPropertyName("lat")]
        public string? Latitude { get; init; }

        [JsonPropertyName("lon")]
        public string? Longitude { get; init; }

        [JsonPropertyName("address")]
        public NominatimAddress? Address { get; init; }
    }

    private sealed class NominatimAddress
    {
        [JsonPropertyName("city")]
        public string? City { get; init; }

        [JsonPropertyName("town")]
        public string? Town { get; init; }

        [JsonPropertyName("village")]
        public string? Village { get; init; }

        [JsonPropertyName("municipality")]
        public string? Municipality { get; init; }

        [JsonPropertyName("county")]
        public string? County { get; init; }

        [JsonPropertyName("state")]
        public string? State { get; init; }

        [JsonPropertyName("country")]
        public string? Country { get; init; }

        [JsonPropertyName("country_code")]
        public string? CountryCode { get; init; }
    }
}
