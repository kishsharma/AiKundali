using KundaliEngine.Domain.Models;
using System;
using System.Collections.Generic;
using System.Linq;

namespace KundaliEngine.Astrology.Services.Compatibility
{
    /// <summary>
    /// Phase 1 Compatibility Score Service
    /// 
    /// Orchestration layer for compatibility intelligence.
    /// Designed as a modular, extensible foundation for future compatibility analyzers.
    /// 
    /// Current responsibilities (Phase 1):
    /// - Accept two CompatibilityProfile objects
    /// - Create placeholder compatibility dimensions
    /// - Aggregate scores into overall compatibility
    /// - Produce structured CompatibilityResult
    /// - Preserve person identity context
    /// 
    /// Future extensibility (NOT Phase 1):
    /// - Moon compatibility analyzer
    /// - Nakshatra matching engine
    /// - Venus/Mars attraction analysis
    /// - 7th house comparison
    /// - Dasha synchronization
    /// - Guna milan calculator
    /// - Remedy engine
    /// - AI narrative generation
    /// - Compatibility confidence scoring
    /// - Relationship timeline prediction
    /// </summary>
    public class CompatibilityScoreService
    {
        /// <summary>
        /// Phase 1: Calculate compatibility between two people.
        /// 
        /// Flow:
        /// 1. Validate inputs (both profiles and charts present)
        /// 2. Create placeholder dimensions
        /// 3. Calculate dimension scores (Phase 1: baseline 50)
        /// 4. Aggregate overall score
        /// 5. Extract strengths/challenges
        /// 6. Produce structured result
        /// 
        /// Returns: CompatibilityResult with identity-aware context preserved.
        /// </summary>
        public CompatibilityResult CalculateCompatibility(
            CompatibilityProfile personA,
            CompatibilityProfile personB)
        {
            // Validate inputs
            if (personA == null)
                throw new ArgumentNullException(nameof(personA));
            if (personB == null)
                throw new ArgumentNullException(nameof(personB));
            if (personA.Chart == null)
                throw new ArgumentException("PersonA chart is missing", nameof(personA));
            if (personB.Chart == null)
                throw new ArgumentException("PersonB chart is missing", nameof(personB));

            // Create result with identity-aware context
            var result = new CompatibilityResult(personA.Name, personB.Name);

            // Phase 1: Create placeholder dimensions
            var dimensions = new List<CompatibilityDimension>
            {
                CalculateEmotionalHarmony(personA, personB),
                CalculateCommunicationCompatibility(personA, personB),
                CalculateMarriageStability(personA, personB),
                CalculateAttractionChemistry(personA, personB),
                CalculateSpiritualCompatibility(personA, personB),
                CalculateGrowthSupportPotential(personA, personB)
            };

            result.Dimensions = dimensions;

            // Calculate overall score (Phase 1: simple average)
            result.OverallCompatibilityScore = (int)Math.Round(dimensions.Average(d => d.Score));
            result.OverallConfidence = (int)Math.Round(dimensions.Average(d => d.Confidence));

            // Extract dominant strengths/challenges from dimensions
            ExtractDominantThemes(result);

            // Generate initial summary
            result.Summary = GenerateSummary(result);

            return result;
        }

        /// <summary>
        /// Phase 1 Placeholder: Emotional Harmony
        /// Future: Moon compatibility, emotional needs alignment, family background harmony.
        /// </summary>
        private CompatibilityDimension CalculateEmotionalHarmony(
            CompatibilityProfile personA,
            CompatibilityProfile personB)
        {
            var dimension = new CompatibilityDimension(
                name: "Emotional Harmony",
                score: 50,
                summary: "Baseline emotional compatibility assessment (Phase 1)"
            );

            dimension.Factors.Add("+ Chart data initialized");
            dimension.Factors.Add("* Awaiting Moon compatibility analyzer");
            dimension.Confidence = 40;

            return dimension;
        }

        /// <summary>
        /// Phase 1 Placeholder: Communication Compatibility
        /// Future: Mercury aspects, speech harmony, intellectual alignment, conversation flow.
        /// </summary>
        private CompatibilityDimension CalculateCommunicationCompatibility(
            CompatibilityProfile personA,
            CompatibilityProfile personB)
        {
            var dimension = new CompatibilityDimension(
                name: "Communication Compatibility",
                score: 50,
                summary: "Baseline communication compatibility assessment (Phase 1)"
            );

            dimension.Factors.Add("+ Chart data initialized");
            dimension.Factors.Add("* Awaiting Mercury/speech compatibility analyzer");
            dimension.Confidence = 40;

            return dimension;
        }

        /// <summary>
        /// Phase 1 Placeholder: Marriage Stability
        /// Future: 7th house analysis, Saturn aspects, dasha synchronization, stability indicators.
        /// </summary>
        private CompatibilityDimension CalculateMarriageStability(
            CompatibilityProfile personA,
            CompatibilityProfile personB)
        {
            var dimension = new CompatibilityDimension(
                name: "Marriage Stability",
                score: 50,
                summary: "Baseline marriage stability assessment (Phase 1)"
            );

            dimension.Factors.Add("+ Chart data initialized");
            dimension.Factors.Add("* Awaiting 7th house & Saturn compatibility analyzer");
            dimension.Confidence = 40;

            return dimension;
        }

        /// <summary>
        /// Phase 1 Placeholder: Attraction & Chemistry
        /// Future: Venus/Mars aspects, physical chemistry, attraction indicators, sexual compatibility.
        /// </summary>
        private CompatibilityDimension CalculateAttractionChemistry(
            CompatibilityProfile personA,
            CompatibilityProfile personB)
        {
            var dimension = new CompatibilityDimension(
                name: "Attraction & Chemistry",
                score: 50,
                summary: "Baseline attraction & chemistry assessment (Phase 1)"
            );

            dimension.Factors.Add("+ Chart data initialized");
            dimension.Factors.Add("* Awaiting Venus/Mars attraction analyzer");
            dimension.Confidence = 40;

            return dimension;
        }

        /// <summary>
        /// Phase 1 Placeholder: Spiritual Compatibility
        /// Future: 9th house analysis, Jupiter aspects, spiritual goals alignment, dharmic paths.
        /// </summary>
        private CompatibilityDimension CalculateSpiritualCompatibility(
            CompatibilityProfile personA,
            CompatibilityProfile personB)
        {
            var dimension = new CompatibilityDimension(
                name: "Spiritual Compatibility",
                score: 50,
                summary: "Baseline spiritual compatibility assessment (Phase 1)"
            );

            dimension.Factors.Add("+ Chart data initialized");
            dimension.Factors.Add("* Awaiting 9th house & Jupiter compatibility analyzer");
            dimension.Confidence = 40;

            return dimension;
        }

        /// <summary>
        /// Phase 1 Placeholder: Growth & Support Potential
        /// Future: Dasha synchronization, mutual support indicators, growth timeline, synergy potential.
        /// </summary>
        private CompatibilityDimension CalculateGrowthSupportPotential(
            CompatibilityProfile personA,
            CompatibilityProfile personB)
        {
            var dimension = new CompatibilityDimension(
                name: "Growth & Support Potential",
                score: 50,
                summary: "Baseline growth & support potential assessment (Phase 1)"
            );

            dimension.Factors.Add("+ Chart data initialized");
            dimension.Factors.Add("* Awaiting dasha synchronization & mutual support analyzer");
            dimension.Confidence = 40;

            return dimension;
        }

        /// <summary>
        /// Extract dominant strengths and challenges from dimension scores.
        /// Phase 1: Simple highest/lowest extraction.
        /// Future: Semantic extraction from detailed dimension analysis.
        /// </summary>
        private void ExtractDominantThemes(CompatibilityResult result)
        {
            if (result.Dimensions.Count == 0)
                return;

            // Dominant strengths: top 2-3 dimensions by score
            var strengths = result.Dimensions
                .OrderByDescending(d => d.Score)
                .Take(2)
                .Select(d => d.Name)
                .ToList();

            result.DominantStrengths = strengths;

            // Dominant challenges: bottom 2-3 dimensions by score
            var challenges = result.Dimensions
                .OrderBy(d => d.Score)
                .Take(2)
                .Select(d => d.Name)
                .ToList();

            result.DominantChallenges = challenges;

            // Aggregate factors
            result.Factors.Add($"Overall compatibility: {result.OverallCompatibilityScore}/100");
            result.Factors.AddRange(
                result.Dimensions
                    .SelectMany(d => d.Factors)
                    .Distinct()
            );
        }

        /// <summary>
        /// Generate identity-aware summary.
        /// Phase 1: Template-based (simple).
        /// Future: AI-generated narrative with semantic reasoning.
        /// </summary>
        private string GenerateSummary(CompatibilityResult result)
        {
            var template = $"Compatibility Analysis: {result.PersonAName} & {result.PersonBName}";

            if (result.OverallCompatibilityScore >= 70)
                return $"{template} — High compatibility potential (Score: {result.OverallCompatibilityScore}/100). Favorable foundation for relationship growth.";
            else if (result.OverallCompatibilityScore >= 50)
                return $"{template} — Moderate compatibility with both strengths and challenges (Score: {result.OverallCompatibilityScore}/100). Growth through mutual effort.";
            else
                return $"{template} — Compatibility requires awareness and conscious effort (Score: {result.OverallCompatibilityScore}/100). Focus on mutual support.";
        }
    }
}
