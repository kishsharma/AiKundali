namespace KundaliEngine.Domain.Models
{
    public class OverallKundaliScoreResult
    {
        public int OverallScore { get; set; }
        public int StabilityScore { get; set; }
        public int GrowthScore { get; set; }
        public int ChallengeScore { get; set; }
        public List<string> DominantStrengths { get; set; } = new List<string>();
        public List<string> DominantWeaknesses { get; set; } = new List<string>();
        public List<string> Factors { get; set; } = new List<string>();
    }
}
