using KundaliEngine.Astrology.Services.DivisionalCharts.Interfaces;
using KundaliEngine.Domain.Models;

namespace KundaliEngine.Astrology.Services.DivisionalCharts.Calculators
{
    public class DashamsaCalculator
        : IDivisionalChartCalculator
    {
        public List<PlanetPosition> Calculate(
            List<PlanetPosition> d1Planets)
        {
            throw new NotImplementedException(
                "Dashamsa (D10) calculation not implemented yet.");
        }
    }
}