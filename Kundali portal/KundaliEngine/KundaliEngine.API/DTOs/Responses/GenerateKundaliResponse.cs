using KundaliEngine.API.Prediction.DTOs;

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

        public RashiInsightDto? RashiInsight { get; set; }

        public NakshatraInsightDto? NakshatraInsight { get; set; }

        public TransitInsightDto? TransitInsight { get; set; }

        public RemediesRecommendationDto? RemediesRecommendation { get; set; }

        public List<MahadashaPeriodDto> MahadashaPeriods { get; set; } = new List<MahadashaPeriodDto>();
        public List<YogaDto> Yogas { get; set; } = new();
        public List<DoshaDto> Doshas { get; set; } = new();
        public DivisionalChartDto? D9Chart { get; set; }
        public SudarshanChakraDto? SudarshanChakra { get; set; }
        public AdvancedStrengthTableDto? AdvancedStrengthTables { get; set; }
        public List<PredictionResponseDto> Predictions { get; set; } = new();
    }
}


