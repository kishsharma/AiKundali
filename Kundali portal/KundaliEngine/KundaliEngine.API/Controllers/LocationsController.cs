using KundaliEngine.API.Services.Locations;
using Microsoft.AspNetCore.Mvc;

namespace KundaliEngine.API.Controllers;

[ApiController]
[Route("api/locations")]
public sealed class LocationsController : ControllerBase
{
    private readonly ILocationSearchService _locationSearchService;

    public LocationsController(ILocationSearchService locationSearchService)
    {
        _locationSearchService = locationSearchService;
    }

    [HttpGet("search")]
    public async Task<ActionResult<LocationSearchResponse>> Search(
        [FromQuery] string query,
        [FromQuery] string? countryCode = null,
        [FromQuery] int? limit = null,
        CancellationToken cancellationToken = default)
    {
        var normalizedQuery = query?.Trim() ?? string.Empty;
        if (normalizedQuery.Length < 3)
        {
            return BadRequest(new LocationSearchResponse
            {
                Success = false,
                Message = "Location query must contain at least 3 characters."
            });
        }

        if (normalizedQuery.Length > 120)
        {
            return BadRequest(new LocationSearchResponse
            {
                Success = false,
                Message = "Location query cannot exceed 120 characters."
            });
        }

        if (!string.IsNullOrWhiteSpace(countryCode) &&
            (countryCode.Trim().Length != 2 || !countryCode.Trim().All(char.IsLetter)))
        {
            return BadRequest(new LocationSearchResponse
            {
                Success = false,
                Message = "Country code must contain exactly 2 letters."
            });
        }

        var results = await _locationSearchService.SearchAsync(
            normalizedQuery,
            countryCode,
            limit,
            cancellationToken);

        return Ok(new LocationSearchResponse
        {
            Success = true,
            Data = results
        });
    }

    public sealed class LocationSearchResponse
    {
        public bool Success { get; init; }
        public IReadOnlyList<LocationSearchResult> Data { get; init; } = [];
        public string? Message { get; init; }
    }
}
