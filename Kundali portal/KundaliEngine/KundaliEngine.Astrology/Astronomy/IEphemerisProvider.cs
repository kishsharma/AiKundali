using System;
using System.Collections.Generic;
using KundaliEngine.Domain.Enums;
using KundaliEngine.Domain.Models;

namespace KundaliEngine.Astrology.Astronomy
{
    public interface IEphemerisProvider
    {
        PlanetPosition GetPlanet(
            Planet planet,
            DateTime utc,
            GeoLocation location);

        IReadOnlyList<PlanetPosition> GetAllPlanets(
            DateTime utc,
            GeoLocation location);

        AscendantResult GetAscendant(
            DateTime utc,
            GeoLocation location);

        HouseCalculationResult GetHouseCusps(
            DateTime utc,
            GeoLocation location);
    }
}
