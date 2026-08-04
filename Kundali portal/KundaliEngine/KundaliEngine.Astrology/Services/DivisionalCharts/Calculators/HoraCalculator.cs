using KundaliEngine.Astrology.Services.DivisionalCharts.Interfaces;
using KundaliEngine.Domain.Enums;
using KundaliEngine.Domain.Models;

namespace KundaliEngine.Astrology.Services.DivisionalCharts.Calculators
{
    public class HoraCalculator : IDivisionalChartCalculator
    {
        public List<PlanetPosition> Calculate(List<PlanetPosition> d1Planets)
        {
            return DivisionalChartCalculatorHelper.Calculate(d1Planets, CalculateSign);
        }

        public ZodiacSign CalculateSign(ZodiacSign d1Sign, double degreeInSign)
        {
            var isFirstHalf = degreeInSign < 15.0;
            var isOddSign = DivisionalChartCalculatorHelper.IsOddSign(d1Sign);

            return isOddSign == isFirstHalf ? ZodiacSign.Leo : ZodiacSign.Cancer;
        }
    }
}
