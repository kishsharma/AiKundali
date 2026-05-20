using System;
using System.Collections.Generic;

namespace KundaliEngine.Domain.Models
{
    /// <summary>
    /// Represents a single compatibility dimension (e.g., Emotional Harmony, Communication).
    /// Each dimension has a score (0-100), supporting factors, and a brief summary.
    /// Designed for explainable compatibility intelligence.
    /// </summary>
    public class CompatibilityDimension
    {
        /// <summary>
        /// Name of the compatibility dimension (e.g., "Emotional Harmony", "Communication Compatibility").
        /// </summary>
        public string Name { get; set; }

        /// <summary>
        /// Compatibility score for this dimension (0-100).
        /// 0 = low/incompatible, 100 = high/very compatible.
        /// </summary>
        public int Score { get; set; }

        /// <summary>
        /// List of human-readable factors explaining the score.
        /// Examples:
        /// + Strong Moon harmony between charts
        /// - Mercury in challenging aspects
        /// + Dasha synchronization supports growth
        /// Used for transparency and AI narrative generation.
        /// </summary>
        public List<string> Factors { get; set; } = new List<string>();

        /// <summary>
        /// Brief summary of this dimension's compatibility.
        /// Placeholder in Phase 1; future phases will use semantic/AI generation.
        /// </summary>
        public string Summary { get; set; }

        /// <summary>
        /// Confidence in this dimension's score (0-100).
        /// Accounts for chart accuracy, data completeness, astrological certainty.
        /// </summary>
        public int Confidence { get; set; } = 50;

        public CompatibilityDimension() { }

        public CompatibilityDimension(string name, int score)
        {
            Name = name;
            Score = Math.Clamp(score, 0, 100);
        }

        public CompatibilityDimension(string name, int score, string summary)
        {
            Name = name;
            Score = Math.Clamp(score, 0, 100);
            Summary = summary;
        }
    }
}
