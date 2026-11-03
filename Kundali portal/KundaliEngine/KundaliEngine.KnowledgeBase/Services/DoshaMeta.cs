using System.Collections.Generic;

namespace KundaliEngine.Domain.Models
{
    public class DoshaMeta
    {
        public string Key { get; set; } = string.Empty;
        public string Name { get; set; } = string.Empty;
        public string Category { get; set; } = string.Empty;
        public string Nature { get; set; } = string.Empty;
        public string Description { get; set; } = string.Empty;
        public List<DoshaImpactArea> ImpactAreas { get; set; } = new();
        public List<string> Remedies { get; set; } = new();
        public List<string> References { get; set; } = new();
        public DoshaShlokaMetadata? Shloka { get; set; }
        // New: when true this dosha should be ignored by compatibility analysis.
        public bool ExcludeFromCompatibility { get; set; } = false;
    }

    public class DoshaShlokaMetadata
    {
        public string SourceText { get; set; } = string.Empty;
        public int Chapter { get; set; }
        public string ChapterTitle { get; set; } = string.Empty;
        public string Verses { get; set; } = string.Empty;
        public string Sanskrit { get; set; } = string.Empty;
        public string Transliteration { get; set; } = string.Empty;
        public string SimpleMeaning { get; set; } = string.Empty;
        public string ReferenceType { get; set; } = string.Empty;
        public string VerificationStatus { get; set; } = string.Empty;
        public string ScopeNote { get; set; } = string.Empty;
        public string EditionNote { get; set; } = string.Empty;
    }

    public class DoshaImpactArea
    {
        public string Category { get; set; } = string.Empty;
        public List<string> Statements { get; set; } = new();
    }
}
