using KundaliEngine.Astrology.Services.DivisionalCharts.Interfaces;
using KundaliEngine.Domain.Enums;
using KundaliEngine.Domain.Models;

namespace KundaliEngine.Astrology.Services.DivisionalCharts.Calculators
{
    public class NavamsaCalculator : IDivisionalChartCalculator
    {
        public List<PlanetPosition> Calculate(List<PlanetPosition> d1Planets)
        {
            return DivisionalChartCalculatorHelper.Calculate(d1Planets, CalculateSign);
        }

        public ZodiacSign CalculateSign(ZodiacSign d1Sign, double degreeInSign)
        {
            return CalculateNavamsaSign(d1Sign, degreeInSign);
        }

        public static ZodiacSign CalculateNavamsaSign(ZodiacSign d1Sign, double degreeInSign)
        {
            var navamsaIndex = DivisionalChartCalculatorHelper.DivisionIndex(degreeInSign, 9);
            var baseSignIndex = (int)d1Sign - 1;
            var d9SignIndex = (baseSignIndex * 9 + navamsaIndex) % 12;

            return (ZodiacSign)(d9SignIndex + 1);
        }
    }
}
