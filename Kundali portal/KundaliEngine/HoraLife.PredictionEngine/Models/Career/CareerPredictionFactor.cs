namespace HoraLife.PredictionEngine.Models.Career
{
    public sealed class CareerPredictionFactor
    {
        public string Title { get; init; } = string.Empty;
        public string Interpretation { get; init; } = string.Empty;
        public string[] EvidenceKeys { get; init; } = [];
        public string[] RuleIds { get; init; } = [];
    }
}
