namespace KundaliEngine.API.DTOs.Responses
{
    public class GenerateKundaliResponse
    {
        public KundaliSummaryDto Summary { get; set; }

        public PanchangDto Panchang { get; set; }

        public List<PlanetDto> Planets { get; set; }

        public List<HouseDto> Houses { get; set; }

        public OverallScoreDto Scores { get; set; }

        public CurrentDashaDto CurrentDasha { get; set; }

        public List<MahadashaPeriodDto> MahadashaPeriods { get; set; } = new List<MahadashaPeriodDto>();
    }
}
