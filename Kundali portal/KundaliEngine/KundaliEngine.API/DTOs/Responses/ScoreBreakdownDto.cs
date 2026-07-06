namespace KundaliEngine.API.DTOs.Responses
{
    public class ScoreComponentDto
    {
        public string Label { get; set; } = string.Empty;
        public int Score { get; set; }
        public double Weight { get; set; }
        public double Contribution { get; set; }
    }

    public class ScoreBreakdownDto
    {
        public List<ScoreComponentDto> Overall { get; set; } = new();
        public List<ScoreComponentDto> Stability { get; set; } = new();
        public List<ScoreComponentDto> Growth { get; set; } = new();
        public List<ScoreComponentDto> Challenge { get; set; } = new();
    }
}
