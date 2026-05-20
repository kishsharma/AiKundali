using KundaliEngine.Domain.Enums;

namespace KundaliEngine.Domain.Models.DivisionalCharts
{
    public class DivisionalChart
    {
        public DivisionalChartType ChartType { get; set; }

        public ZodiacSign AscendantSign { get; set; }

        public int AscendantDegree { get; set; }

        public List<PlanetPosition> PlanetPositions { get; set; }
            = new();

        public string Description { get; set; }
            = string.Empty;
    }
}