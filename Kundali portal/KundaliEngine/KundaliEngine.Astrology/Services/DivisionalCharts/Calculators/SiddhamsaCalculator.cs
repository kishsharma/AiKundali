using KundaliEngine.Astrology.Services.DivisionalCharts.Interfaces;
using KundaliEngine.Domain.Enums;
using KundaliEngine.Domain.Models;

namespace KundaliEngine.Astrology.Services.DivisionalCharts.Calculators
{
    public class SiddhamsaCalculator : IDivisionalChartCalculator
    {
        public List<PlanetPosition> Calculate(List<PlanetPosition> d1Planets)
        {
            return DivisionalChartCalculatorHelper.Calculate(d1Planets, CalculateSign);
        }

        public ZodiacSign CalculateSign(ZodiacSign d1Sign, double degreeInSign)
        {
            var divisionIndex = DivisionalChartCalculatorHelper.DivisionIndex(degreeInSign, 24);
            var startSign = DivisionalChartCalculatorHelper.IsOddSign(d1Sign)
                ? ZodiacSign.Leo
                : ZodiacSign.Cancer;

            return DivisionalChartCalculatorHelper.OffsetFrom(startSign, divisionIndex);
        }
    }
}
