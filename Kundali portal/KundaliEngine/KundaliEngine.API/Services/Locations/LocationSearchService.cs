using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.Options;

namespace KundaliEngine.API.Services.Locations;

public sealed class LocationSearchService : ILocationSearchService
{
    private readonly ILocationSearchProvider _provider;
    private readonly IMemoryCache _cache;
    private readonly LocationProviderOptions _options;

    public LocationSearchService( ILocationSearchProvider provider,  IMemoryCache cache, IOptions<LocationProviderOptions> options)
    {
        _provider = provider;
        _cache = cache;
        _options = options.Value;
    }

    public async Task<IReadOnlyList<LocationSearchResult>> SearchAsync( string query, string? countryCode,  int? limit,   CancellationToken cancellationToken)
    {
        var normalizedQuery = query.Trim();
        var normalizedCountry = string.IsNullOrWhiteSpace(countryCode)
            ? _options.DefaultCountryCode.Trim().ToUpperInvariant()
            : countryCode.Trim().ToUpperInvariant();
        var normalizedLimit = Math.Clamp(limit ?? _options.ResultLimit, 1, 10);
        var cacheKey = $"location:{normalizedCountry}:{normalizedLimit}:{normalizedQuery.ToLowerInvariant()}";

        if (_cache.TryGetValue(cacheKey, out IReadOnlyList<LocationSearchResult>? cached) && cached != null)
        {
            return cached;
        }

        var results = await _provider.SearchAsync( normalizedQuery, normalizedCountry,normalizedLimit, cancellationToken);

        _cache.Set(cacheKey, results, TimeSpan.FromMinutes(Math.Max(1, _options.CacheMinutes)));
        return results;
    }
}
