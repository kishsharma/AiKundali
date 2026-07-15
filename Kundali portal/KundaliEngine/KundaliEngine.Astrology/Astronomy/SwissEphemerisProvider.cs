using System;
using System.Collections.Generic;
using KundaliEngine.Astrology.Mapping;
using KundaliEngine.Domain.Enums;
using KundaliEngine.Domain.Models;

namespace KundaliEngine.Astrology.Astronomy
{
    public sealed class SwissEphemerisProvider : IEphemerisProvider
    {
        private static readonly Planet[] SupportedPlanets =
        {
            Planet.Sun,
            Planet.Moon,
            Planet.Mars,
            Planet.Mercury,
            Planet.Jupiter,
            Planet.Venus,
            Planet.Saturn,
            Planet.Rahu,
            Planet.Ketu
        };

        public SwissEphemerisProvider()
        {
        }

        public SwissEphemerisProvider(string ephePath)
        {
            SwissEphWrapper.Initialize(ephePath);
        }

        public PlanetPosition GetPlanet(
            Planet planet,
            DateTime utc,
            GeoLocation location)
        {
            double jd = JulianDayCalculator.ToJulianDay(utc);
            double longitude;
            double latitude;
            bool isRetrograde;
            if (planet == Planet.Ketu)
            {
                var rahu = GetPlanet(Planet.Rahu, utc, location);
                longitude = NormalizeLongitude(rahu.Longitude + 180.0);
                latitude = -rahu.Latitude;
                isRetrograde = true;
            }
            else
            {
                (longitude, latitude, isRetrograde) = SwissEphWrapper.CalculatePlanet(planet.ToString(), jd);
            }

            return PlanetPositionMapper.Map(planet, longitude, latitude,isRetrograde);
        }

        public IReadOnlyList<PlanetPosition> GetAllPlanets(
            DateTime utc,
            GeoLocation location)
        {
            var planetPositions = new List<PlanetPosition>();

            foreach (var planet in SupportedPlanets)
            {
                var result = GetPlanet(planet, utc, location);
                planetPositions.Add(result);
            }

            return planetPositions;
        }

        public AscendantResult GetAscendant(
            DateTime utc,
            GeoLocation location)
        {
            double julianDay = JulianDayCalculator.ToJulianDay(utc);
            var ascendant = SwissEphWrapper.CalculateAscendant(julianDay, location.Latitude, location.Longitude);
            return new AscendantResult(ascendant);
        }

        public HouseCalculationResult GetHouseCusps(
            DateTime utc,
            GeoLocation location)
        {
            double julianDay = JulianDayCalculator.ToJulianDay(utc);
            var (cusps, ascMc) = SwissEphWrapper.CalculateHouseCusps(julianDay, location.Latitude, location.Longitude);
            return new HouseCalculationResult(cusps, ascMc);
        }

        private static double NormalizeLongitude(double longitude)
        {
            longitude %= 360.0;
            if (longitude < 0)
                longitude += 360.0;

            return longitude;
        }
    }
}
