using KundaliEngine.Astrology.Services.DivisionalCharts.Interfaces;
using KundaliEngine.Domain.Enums;
using KundaliEngine.Domain.Models;

namespace KundaliEngine.Astrology.Services.DivisionalCharts.Calculators
{
    public class BhamsaCalculator : IDivisionalChartCalculator
    {
        public List<PlanetPosition> Calculate(List<PlanetPosition> d1Planets)
        {
            return DivisionalChartCalculatorHelper.Calculate(d1Planets, CalculateSign);
        }

        public ZodiacSign CalculateSign(ZodiacSign d1Sign, double degreeInSign)
        {
            var divisionIndex = DivisionalChartCalculatorHelper.DivisionIndex(degreeInSign, 27);
            var element = ((int)d1Sign - 1) % 4;
            var startSign = element switch
            {
                0 => ZodiacSign.Aries,
                1 => ZodiacSign.Cancer,
                2 => ZodiacSign.Libra,
                _ => ZodiacSign.Capricorn
            };

            return DivisionalChartCalculatorHelper.OffsetFrom(startSign, divisionIndex);
        }
    }
}
