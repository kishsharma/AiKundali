using KundaliEngine.Astrology.Services.DivisionalCharts.Interfaces;
using KundaliEngine.Domain.Enums;
using KundaliEngine.Domain.Models;

namespace KundaliEngine.Astrology.Services.DivisionalCharts.Calculators
{
    public class TrimsamsaCalculator : IDivisionalChartCalculator
    {
        public List<PlanetPosition> Calculate(List<PlanetPosition> d1Planets)
        {
            return DivisionalChartCalculatorHelper.Calculate(d1Planets, CalculateSign);
        }

        public ZodiacSign CalculateSign(ZodiacSign d1Sign, double degreeInSign)
        {
            var degree = degreeInSign % 30.0;
            if (degree < 0)
            {
                degree += 30.0;
            }

            if (DivisionalChartCalculatorHelper.IsOddSign(d1Sign))
            {
                if (degree < 5) return ZodiacSign.Aries;
                if (degree < 10) return ZodiacSign.Aquarius;
                if (degree < 18) return ZodiacSign.Sagittarius;
                if (degree < 25) return ZodiacSign.Gemini;
                return ZodiacSign.Libra;
            }

            if (degree < 5) return ZodiacSign.Taurus;
            if (degree < 12) return ZodiacSign.Virgo;
            if (degree < 20) return ZodiacSign.Pisces;
            if (degree < 25) return ZodiacSign.Capricorn;
            return ZodiacSign.Scorpio;
        }
    }
}
