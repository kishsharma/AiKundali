using System;
using KundaliEngine.Astrology.Mapping;
using KundaliEngine.Domain.Enums;
using KundaliEngine.Domain.Models;

namespace KundaliEngine.Astrology.Astronomy
{
    public class PlanetPositionCalculator
    {
        public PlanetPosition Calculate(Planet planet, DateTime utc)
        {
            double jd = JulianDayCalculator.ToJulianDay(utc);
            double longitude;
            double latitude;

            if (planet == Planet.Ketu)
            {
                var rahu = Calculate(Planet.Rahu, utc);
                longitude = NormalizeLongitude(rahu.Longitude + 180.0);
                latitude = -rahu.Latitude;
            }
            else
            {
                (longitude, latitude) = SwissEphWrapper.CalculatePlanet(planet.ToString(), jd);
            }

            return PlanetPositionMapper.Map(planet, longitude, latitude);
        }

        public PlanetPosition Calculate(string planetName, DateTime utc)
        {
            Planet planet = Enum.Parse<Planet>(planetName, ignoreCase: true);
            return Calculate(planet, utc);
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
