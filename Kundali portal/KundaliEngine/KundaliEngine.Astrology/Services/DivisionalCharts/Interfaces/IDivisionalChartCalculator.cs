using KundaliEngine.Domain.Models;

namespace KundaliEngine.Astrology.Services.DivisionalCharts.Interfaces
{
    public interface IDivisionalChartCalculator
    {
        List<PlanetPosition> Calculate(List<PlanetPosition> d1Planets);
    }
}