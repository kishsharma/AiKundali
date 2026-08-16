namespace KundaliEngine.API.Services.Locations;

public interface ILocationSearchProvider
{
    Task<IReadOnlyList<LocationSearchResult>> SearchAsync( string query, string countryCode,    int limit,     CancellationToken cancellationToken);
}
