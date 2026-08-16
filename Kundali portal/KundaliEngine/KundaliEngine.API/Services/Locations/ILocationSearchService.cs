namespace KundaliEngine.API.Services.Locations;

public interface ILocationSearchService
{
    Task<IReadOnlyList<LocationSearchResult>> SearchAsync(string query,string? countryCode,  int? limit,  CancellationToken cancellationToken);
}
