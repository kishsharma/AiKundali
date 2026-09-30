using KundaliEngine.Astrology.Services.DivisionalCharts.Interfaces;
using KundaliEngine.Domain.Enums;
using KundaliEngine.Domain.Models;

namespace KundaliEngine.Astrology.Services.DivisionalCharts.Calculators
{
    public class AkshavedamsaCalculator : IDivisionalChartCalculator
    {
        public List<PlanetPosition> Calculate(List<PlanetPosition> d1Planets)
        {
            return DivisionalChartCalculatorHelper.Calculate(d1Planets, CalculateSign);
        }

        public ZodiacSign CalculateSign(ZodiacSign d1Sign, double degreeInSign)
        {
            var divisionIndex = DivisionalChartCalculatorHelper.DivisionIndex(degreeInSign, 45);
            var modality = ((int)d1Sign - 1) % 3;
            var startSign = modality switch
            {
                0 => ZodiacSign.Aries,
                1 => ZodiacSign.Leo,
                _ => ZodiacSign.Sagittarius
            };

            return DivisionalChartCalculatorHelper.OffsetFrom(startSign, divisionIndex);
        }
    }
}
