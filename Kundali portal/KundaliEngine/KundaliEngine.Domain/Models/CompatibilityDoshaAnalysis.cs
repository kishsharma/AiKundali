using System.Collections.Generic;

namespace KundaliEngine.Domain.Models
{
    public class CompatibilityDoshaAnalysis
    {
        public int OverallDoshaScore { get; set; } = 100;
        public string OverallDoshaStatus { get; set; } = "No Dosha";
        public List<string> DoshaFactors { get; set; } = new();
        public List<string> DoshaRemedies { get; set; } = new();
        public List<CompatibilityDoshaItem> Items { get; set; } = new();
        public List<ScoreComponent> ScoreBreakdown { get; set; } = new();
    }

    public class CompatibilityDoshaItem
    {
        public string Key { get; set; } = string.Empty;
        public string Name { get; set; } = string.Empty;
        public string Category { get; set; } = string.Empty;
        public string Status { get; set; } = "No Dosha";
        public int CompatibilityScore { get; set; } = 100;
        public int RiskScore { get; set; }
        public string Severity { get; set; } = "None";
        public string Tone { get; set; } = "green";
        public CompatibilityDoshaPersonSummary PersonA { get; set; } = new();
        public CompatibilityDoshaPersonSummary PersonB { get; set; } = new();
        public string Reason { get; set; } = string.Empty;
        public List<string> Factors { get; set; } = new();
        public List<string> Remedies { get; set; } = new();
        public List<string> References { get; set; } = new();
        public List<ScoreComponent> ScoreBreakdown { get; set; } = new();
    }

    public class CompatibilityDoshaPersonSummary
    {
        public bool IsPresent { get; set; }
        public string Severity { get; set; } = "None";
        public string Reason { get; set; } = string.Empty;
        public List<string> InvolvedPlanets { get; set; } = new();
        public List<int> InvolvedHouses { get; set; } = new();
    }
}
