using System.Collections.Generic;

namespace KundaliEngine.Domain.Models
{
    public class YogaMeta
    {
        public string Key { get; set; } = string.Empty; // Unique identifier
        public string Name { get; set; } = string.Empty; // Human-readable name
        public string SanskritName { get; set; } = string.Empty; // Sanskrit name
        public string Category { get; set; } = string.Empty; // e.g. "Raja Yoga", "Dhana Yoga", etc.
        public string Nature { get; set; } = string.Empty; // "Benefic", "Malefic", "Mixed"
        public string Description { get; set; } = string.Empty; // Short description
        public string Impact { get; set; } = string.Empty; // Impact statement
        public List<string> References { get; set; } = new(); // Classical references
    }

    // Optionally, a detection result model
    public class YogaDetectionResult
    {
        public string Key { get; set; } = string.Empty;
        public bool IsPresent { get; set; }
        public string? ActivationPeriod { get; set; } // e.g. "Jupiter Mahadasha"
        public YogaMeta? Meta { get; set; } // Metadata for reporting
    }
}
