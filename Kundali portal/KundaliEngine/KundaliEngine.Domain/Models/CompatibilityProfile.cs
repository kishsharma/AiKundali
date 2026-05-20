using System;

namespace KundaliEngine.Domain.Models
{
    /// <summary>
    /// Represents a person participating in compatibility analysis.
    /// Preserves identity context for relationship-aware responses, reports, and future AI narratives.
    /// </summary>
    public class CompatibilityProfile
    {
        /// <summary>
        /// Person's name (required for identity-aware compatibility context).
        /// Used in reports, dashboards, and relationship narratives.
        /// </summary>
        public string Name { get; set; }

        /// <summary>
        /// The person's complete Kundali chart (already generated).
        /// Contains planets, houses, aspects, panchang, dasha—everything needed for compatibility analysis.
        /// </summary>
        public KundaliChart Chart { get; set; }

        /// <summary>
        /// Optional: Gender or relationship role context for future semantic compatibility.
        /// </summary>
        public string Gender { get; set; }

        /// <summary>
        /// Optional: Birth time quality indicator (accurate, approximate, unknown).
        /// Influences confidence in chart-based calculations.
        /// </summary>
        public string TimeAccuracy { get; set; }

        public CompatibilityProfile() { }

        public CompatibilityProfile(string name, KundaliChart chart)
        {
            Name = name;
            Chart = chart;
        }
    }
}
