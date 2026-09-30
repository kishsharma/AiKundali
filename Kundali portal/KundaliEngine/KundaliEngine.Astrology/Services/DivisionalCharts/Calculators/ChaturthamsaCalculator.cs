using KundaliEngine.Astrology.Services.DivisionalCharts.Interfaces;
using KundaliEngine.Domain.Enums;
using KundaliEngine.Domain.Models;

namespace KundaliEngine.Astrology.Services.DivisionalCharts.Calculators
{
    public class ChaturthamsaCalculator : IDivisionalChartCalculator
    {
        public List<PlanetPosition> Calculate(List<PlanetPosition> d1Planets)
        {
            return DivisionalChartCalculatorHelper.Calculate(d1Planets, CalculateSign);
        }

        public ZodiacSign CalculateSign(ZodiacSign d1Sign, double degreeInSign)
        {
            var divisionIndex = DivisionalChartCalculatorHelper.DivisionIndex(degreeInSign, 4);
            var modality = ((int)d1Sign - 1) % 3;
            var startOffset = modality switch
            {
                0 => 0,
                1 => 3,
                _ => 6
            };

            return DivisionalChartCalculatorHelper.OffsetFrom(d1Sign, startOffset + divisionIndex);
        }
    }
}
