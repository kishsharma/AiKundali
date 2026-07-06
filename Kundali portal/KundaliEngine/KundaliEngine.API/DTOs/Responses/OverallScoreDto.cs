namespace KundaliEngine.API.DTOs.Responses
{
    public class OverallScoreDto
    {
        public int Overall { get; set; }
        public int Stability { get; set; }
        public int Growth { get; set; }
        public int Challenge { get; set; }
        public List<string> DominantStrengths { get; set; } = new();
        public List<string> DominantWeaknesses { get; set; } = new();
        public List<string> Factors { get; set; } = new(); // Add this for explanatory details
                                                           // Explanatory lists for UI
        public List<string> OverallFactors { get; set; } = new();
        public List<string> StabilityFactors { get; set; } = new();
        public List<string> GrowthFactors { get; set; } = new();
        public List<string> ChallengeFactors { get; set; } = new();
        // New structured numeric breakdown for UI (hover details)
        public ScoreBreakdownDto ScoreBreakdown { get; set; } = new();
    }
}