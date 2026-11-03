using System.Collections.Generic;

namespace KundaliEngine.Domain.Models
{
    /// <summary>
    /// Per-chart evidence for a relationship-relevant house and a transparent
    /// two-chart mean of the house support scores.
    /// </summary>
    public class RelationshipHouseAnalysis
    {
        public int HouseNumber { get; set; }
        public string ThemeKey { get; set; } = string.Empty;
        public RelationshipHouseEvidence PersonA { get; set; } = new();
        public RelationshipHouseEvidence PersonB { get; set; } = new();
        public int? CombinedScore { get; set; }
        public string ConcernLevel { get; set; } = "Unavailable";
    }

    public class RelationshipHouseEvidence
    {
        public bool IsAvailable { get; set; }
        public int? SupportScore { get; set; }
        public string ConcernLevel { get; set; } = "Unavailable";
        public string HouseLord { get; set; } = string.Empty;
        public int? HouseLordStrength { get; set; }
        public List<string> Occupants { get; set; } = new();
        public List<string> BeneficInfluences { get; set; } = new();
        public List<string> MaleficInfluences { get; set; } = new();
        public List<string> MitigatingFactors { get; set; } = new();
        public List<string> Factors { get; set; } = new();
    }
}
