using KundaliEngine.API.DTOs.Responses;
using KundaliEngine.Astrology.Astronomy;
using KundaliEngine.Domain.Enums;
using KundaliEngine.Domain.Models;

namespace KundaliEngine.API.Services.Gochar;

public sealed class RetrogradeTransitService : IRetrogradeTransitService
{
    private static readonly GeoLocation ReferenceLocation = new(0, 0);

    private static readonly IReadOnlyDictionary<Planet, int> DisplayOrder = new Dictionary<Planet, int>
    {
        [Planet.Mercury] = 1,
        [Planet.Venus] = 2,
        [Planet.Mars] = 3,
        [Planet.Jupiter] = 4,
        [Planet.Saturn] = 5,
        [Planet.Rahu] = 6,
        [Planet.Ketu] = 7
    };

    private readonly IEphemerisProvider _ephemerisProvider;

    public RetrogradeTransitService(IEphemerisProvider ephemerisProvider)
    {
        _ephemerisProvider = ephemerisProvider;
    }

    public CurrentRetrogradesResponse GetCurrentRetrogrades(bool includeNodes)
    {
        var calculatedAtUtc = DateTime.UtcNow;
        var planets = _ephemerisProvider
            .GetAllPlanets(calculatedAtUtc, ReferenceLocation)
            .Where(planet => planet.IsRetrograde)
            .Where(planet => includeNodes || !IsNode(planet.Planet))
            .OrderBy(planet => DisplayOrder.TryGetValue(planet.Planet, out var order) ? order : 99)
            .Select(Map)
            .ToList();

        return new CurrentRetrogradesResponse
        {
            CalculatedAtUtc = calculatedAtUtc,
            IncludeNodes = includeNodes,
            Note = includeNodes
                ? "Rahu and Ketu are included. In this engine they are treated as naturally retrograde shadow nodes."
                : "Rahu and Ketu are excluded by default because they are treated as naturally retrograde shadow nodes. Use includeNodes=true to include them.",
            Planets = planets
        };
    }

    private static RetrogradePlanetDto Map(PlanetPosition planet)
    {
        var towardSign = GetPreviousSign(planet.Sign);
        var isNode = IsNode(planet.Planet);

        return new RetrogradePlanetDto
        {
            Planet = planet.Planet.ToString(),
            CurrentSign = planet.Sign.ToString(),
            RetrogradeFromSign = planet.Sign.ToString(),
            RetrogradeTowardSign = towardSign.ToString(),
            Degree = planet.Degree,
            Minute = planet.Minute,
            DegreeInSign = Math.Round(planet.DegreeInSign, 4),
            Longitude = Math.Round(planet.Longitude, 6),
            IsNode = isNode,
            Motion = "Retrograde",
            Information = isNode
                ? $"{planet.Planet} is a shadow node and is treated as naturally retrograde in {planet.Sign}, moving toward {towardSign}."
                : $"{planet.Planet} is currently retrograde in {planet.Sign}, moving back toward {towardSign}."
        };
    }

    private static ZodiacSign GetPreviousSign(ZodiacSign sign)
    {
        var previous = (int)sign - 1;
        return previous < 1 ? ZodiacSign.Pisces : (ZodiacSign)previous;
    }

    private static bool IsNode(Planet planet)
    {
        return planet is Planet.Rahu or Planet.Ketu;
    }
}

