namespace KundaliEngine.API.DTOs.Responses
{
    public class DivisionalChartDto
    {
        public string ChartType { get; set; } = string.Empty;

        public string Ascendant { get; set; } = string.Empty;

        public List<PlanetDto> Planets { get; set; } = new();

        public List<HouseDto> Houses { get; set; } = new();

        public string Description { get; set; } = string.Empty;
    }
}
