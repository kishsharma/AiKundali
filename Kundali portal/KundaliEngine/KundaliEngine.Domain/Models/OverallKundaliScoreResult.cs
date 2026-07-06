using System.Collections.Generic;

namespace KundaliEngine.Domain.Models
{
    public class ScoreComponent
    {
        public string Label { get; set; } = string.Empty;
        public int Score { get; set; }
        public double Weight { get; set; }
        public double Contribution { get; set; }
    }

    public class ScoreBreakdown
    {
        public List<ScoreComponent> Overall { get; set; } = new();
        public List<ScoreComponent> Stability { get; set; } = new();
        public List<ScoreComponent> Growth { get; set; } = new();
        public List<ScoreComponent> Challenge { get; set; } = new();
    }

    public class OverallKundaliScoreResult
    {
        public int OverallScore { get; set; }
        public int StabilityScore { get; set; }
        public int GrowthScore { get; set; }
        public int ChallengeScore { get; set; }

        public List<string> DominantStrengths { get; set; } = new List<string>();
        public List<string> DominantWeaknesses { get; set; } = new List<string>();

        // Backwards compatible aggregated factors
        public List<string> Factors { get; set; } = new List<string>();

        // New: per-category explanatory factor lists for UI hover/click
        public List<string> OverallFactors { get; set; } = new List<string>();
        public List<string> StabilityFactors { get; set; } = new List<string>();
        public List<string> GrowthFactors { get; set; } = new List<string>();
        public List<string> ChallengeFactors { get; set; } = new List<string>();
        // New structured numeric breakdown for UI
        public ScoreBreakdown ScoreBreakdown { get; set; } = new();
    }
}
