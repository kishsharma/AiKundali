using KundaliEngine.Domain.Enums;
using KundaliEngine.Domain.Models;

namespace KundaliEngine.Astrology.Services.DivisionalCharts.Calculators
{
    internal static class DivisionalChartCalculatorHelper
    {
        public static List<PlanetPosition> Calculate(
            List<PlanetPosition> d1Planets,
            Func<ZodiacSign, double, ZodiacSign> signCalculator)
        {
            return d1Planets.Select(planet => CloneWithSign(
                planet,
                signCalculator(planet.Sign, planet.DegreeInSign))).ToList();
        }

        public static PlanetPosition CloneWithSign(PlanetPosition source, ZodiacSign sign)
        {
            return new PlanetPosition
            {
                Planet = source.Planet,
                PlanetName = source.PlanetName,
                Longitude = source.Longitude,
                Latitude = source.Latitude,
                Sign = sign,
                DegreeInSign = source.DegreeInSign,
                Degree = source.Degree,
                Minute = source.Minute,
                Nakshatra = source.Nakshatra,
                Pada = source.Pada,
                House = source.House,
                IsRetrograde = source.IsRetrograde,
                DetailedStrength = source.DetailedStrength,
                IsCombust = source.IsCombust
            };
        }

        public static ZodiacSign OffsetFrom(ZodiacSign startSign, int offset)
        {
            var signIndex = ((int)startSign - 1 + offset) % 12;
            if (signIndex < 0)
            {
                signIndex += 12;
            }

            return (ZodiacSign)(signIndex + 1);
        }

        public static int DivisionIndex(double degreeInSign, int divisions)
        {
            var normalizedDegree = degreeInSign % 30.0;
            if (normalizedDegree < 0)
            {
                normalizedDegree += 30.0;
            }

            return Math.Min((int)(normalizedDegree / (30.0 / divisions)), divisions - 1);
        }

        public static bool IsOddSign(ZodiacSign sign) => ((int)sign % 2) == 1;
    }
}
