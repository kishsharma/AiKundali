using KundaliEngine.Astrology.Calculations;
using KundaliEngine.Domain.Enums;
using KundaliEngine.Domain.Models;

namespace KundaliEngine.Astrology.Mapping
{
    public static class PlanetPositionMapper
    {
        public static PlanetPosition Map(Planet planet, double longitude, double latitude)
        {
            var (sign, degree, minute) = ZodiacCalculator.Calculate(longitude);
            var (nakshatra, pada) = NakshatraCalculator.Calculate(longitude);

            return new PlanetPosition
            {
                Planet = planet,
                Longitude = longitude,
                Latitude = latitude,
                Sign = sign,
                Degree = degree,
                Minute = minute,
                Nakshatra = nakshatra,
                Pada = pada
            };
        }
    }
}
