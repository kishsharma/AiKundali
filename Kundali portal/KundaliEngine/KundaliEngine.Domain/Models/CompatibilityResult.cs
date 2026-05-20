using System;
using System.Collections.Generic;

namespace KundaliEngine.Domain.Models
{
    /// <summary>
    /// Represents the complete compatibility analysis result between two people.
    /// Identity-aware: preserves person names for relationship-contextual responses.
    /// Designed for reports, dashboards, APIs, and saved matchmaking history.
    /// </summary>
    public class CompatibilityResult
    {
        /// <summary>
        /// Name of the first person (for identity-aware narrative generation).
        /// </summary>
        public string PersonAName { get; set; }

        /// <summary>
        /// Name of the second person (for identity-aware narrative generation).
        /// </summary>
        public string PersonBName { get; set; }

        /// <summary>
        /// Overall compatibility score (0-100).
        /// Aggregate of all compatibility dimensions.
        /// </summary>
        public int OverallCompatibilityScore { get; set; }

        /// <summary>
        /// List of compatibility dimensions (Emotional, Communication, Marriage Stability, etc.).
        /// Each dimension provides detailed analysis and factors.
        /// </summary>
        public List<CompatibilityDimension> Dimensions { get; set; } = new List<CompatibilityDimension>();

        /// <summary>
        /// Confidence in the overall compatibility score (0-100).
        /// Aggregate of dimension confidences and chart data quality.
        /// </summary>
        public int OverallConfidence { get; set; } = 50;

        /// <summary>
        /// List of dominant strengths in the compatibility (top life areas, harmonies).
        /// Extracted from highest-scoring dimensions and chart analysis.
        /// Examples:
        /// - Strong emotional alignment
        /// - Shared spiritual values
        /// - Supportive growth periods
        /// </summary>
        public List<string> DominantStrengths { get; set; } = new List<string>();

        /// <summary>
        /// List of dominant challenges in the compatibility.
        /// Extracted from lowest-scoring dimensions and chart tensions.
        /// Examples:
        /// - Communication pressure during Saturn transit
        /// - Different life paces in current dasha
        /// - Financial planning differences
        /// </summary>
        public List<string> DominantChallenges { get; set; } = new List<string>();

        /// <summary>
        /// Aggregated factors explaining the overall result.
        /// Combines factors from all dimensions and chart-level analysis.
        /// Used for transparency and explainable compatibility reasoning.
        /// </summary>
        public List<string> Factors { get; set; } = new List<string>();

        /// <summary>
        /// High-level summary of the relationship compatibility.
        /// Identity-aware: uses person names.
        /// Placeholder in Phase 1; future: AI-generated narrative.
        /// Example: "Kishore and Priya show strong emotional harmony but face communication challenges during Saturn transit."
        /// </summary>
        public string Summary { get; set; }

        /// <summary>
        /// Timestamp of analysis (for saved history and tracking).
        /// </summary>
        public DateTime AnalyzedAtUtc { get; set; } = DateTime.UtcNow;

        /// <summary>
        /// Phase or version of compatibility engine used.
        /// Useful for tracking evolution and future compatibility recalculations.
        /// </summary>
        public string EngineVersion { get; set; } = "1.0-foundation";

        public CompatibilityResult() { }

        public CompatibilityResult(string personAName, string personBName)
        {
            PersonAName = personAName;
            PersonBName = personBName;
        }
    }
}
