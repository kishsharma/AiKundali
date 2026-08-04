using KundaliEngine.Astrology.Services.KundaliDosha;
using KundaliEngine.Domain.Models;
using KundaliEngine.KnowledgeBase.Services;
using System;
using System.Collections.Generic;
using System.Linq;

namespace KundaliEngine.Astrology.Services.Compatibility
{
    public class CompatibilityDoshaAnalysisService
    {
        // Compatibility intentionally uses a small relationship-facing allow-list.
        // Broader individual Kundali findings such as Kaal Sarpa, Pitru/Pitra,
        // Guru Chandal, Shrapit, Kemadruma, Sakata, Gandmool, Daridra, Chandra Grahan,
        // Surya Grahan and generic Raja/Dhana/spiritual/career yogas should remain in 
        // personal reports, not in match scoring.
        private static readonly string[] DoshaKeys =
        {
            "manglik",
            "shani"
        };

        private static readonly Dictionary<string, double> DoshaWeights = new(StringComparer.OrdinalIgnoreCase)
        {
            ["manglik"] = 0.80,
            ["shani"] = 0.20
        };

        public CompatibilityDoshaAnalysis Analyze(
            string personAName,
            string personBName,
            IReadOnlyCollection<DoshaRuleMatch> personADoshas,
            IReadOnlyCollection<DoshaRuleMatch> personBDoshas,
            DoshaMetadataService metadataService)
        {
            var personAByKey = ToLookup(personADoshas);
            var personBByKey = ToLookup(personBDoshas);

            // Consult KB metadata and skip any dosha explicitly excluded from compatibility
            var items = DoshaKeys
                .Select(key => new { Key = key, Meta = metadataService.GetDosha(key) })
                .Where(x => x.Meta == null || !x.Meta.ExcludeFromCompatibility)
                .Select(x => BuildDoshaItem(
                    x.Key,
                    personAName,
                    personBName,
                    personAByKey.GetValueOrDefault(x.Key),
                    personBByKey.GetValueOrDefault(x.Key),
                    x.Meta))
                .ToList();

            var scoreBreakdown = items
                .Select(item =>
                {
                    var weight = DoshaWeights.TryGetValue(item.Key, out var w) ? w : 0.0;
                    return new ScoreComponent
                    {
                        Label = item.Name,
                        Score = item.CompatibilityScore,
                        Weight = weight,
                        Contribution = Math.Round(item.CompatibilityScore * weight, 2)
                    };
                })
                .ToList();

            var overallScore = (int)Math.Round(scoreBreakdown.Sum(item => item.Contribution));
            var doshaFactors = items
                .Select(item => $"{item.Name}: {item.Status}, score {item.CompatibilityScore}/100. {item.Reason}")
                .ToList();

            return new CompatibilityDoshaAnalysis
            {
                OverallDoshaScore = Math.Clamp(overallScore, 0, 100),
                OverallDoshaStatus = GetOverallStatus(items),
                DoshaFactors = doshaFactors,
                DoshaRemedies = items
                    .Where(item => item.Status != "No Dosha")
                    .SelectMany(item => item.Remedies)
                    .Distinct(StringComparer.OrdinalIgnoreCase)
                    .ToList(),
                Items = items,
                ScoreBreakdown = scoreBreakdown
            };
        }

        // ... rest of file unchanged ...
        private static Dictionary<string, DoshaRuleMatch> ToLookup(IReadOnlyCollection<DoshaRuleMatch> matches)
        {
            return matches
                .Where(match => match.IsPresent)
                .GroupBy(match => match.Key, StringComparer.OrdinalIgnoreCase)
                .ToDictionary(group => group.Key, group => group.First(), StringComparer.OrdinalIgnoreCase);
        }

        private static CompatibilityDoshaItem BuildDoshaItem(
            string key,
            string personAName,
            string personBName,
            DoshaRuleMatch? personAMatch,
            DoshaRuleMatch? personBMatch,
            DoshaMeta? meta)
        {
            var personA = BuildPersonSummary(personAMatch);
            var personB = BuildPersonSummary(personBMatch);
            var personASeverity = SeverityValue(personA.Severity);
            var personBSeverity = SeverityValue(personB.Severity);

            var comparison = Compare(key, personAName, personBName, personA, personB, personASeverity, personBSeverity);
            var riskScore = (int)Math.Round(Math.Clamp(comparison.RiskScore, 0, 100));
            var compatibilityScore = 100 - riskScore;

            var itemName = meta?.Name ?? ToTitle(key);
            var factors = new List<string>
            {
                $"Person A intensity: {personA.Severity} ({personASeverity}/100 risk value).",
                $"Person B intensity: {personB.Severity} ({personBSeverity}/100 risk value).",
                comparison.Formula
            };

            if (!string.IsNullOrWhiteSpace(personA.Reason))
            {
                factors.Add($"{personAName}: {personA.Reason}");
            }

            if (!string.IsNullOrWhiteSpace(personB.Reason))
            {
                factors.Add($"{personBName}: {personB.Reason}");
            }

            return new CompatibilityDoshaItem
            {
                Key = key,
                Name = itemName,
                Category = meta?.Category ?? "Kundali Dosha",
                Status = comparison.Status,
                CompatibilityScore = compatibilityScore,
                RiskScore = riskScore,
                Severity = SeverityFromRisk(riskScore),
                Tone = ToneFor(comparison.Status, riskScore),
                PersonA = personA,
                PersonB = personB,
                Reason = comparison.Reason,
                Factors = factors,
                Remedies = meta?.Remedies ?? new(),
                References = meta?.References ?? new(),
                ScoreBreakdown = new List<ScoreComponent>
                {
                    new()
                    {
                        Label = comparison.ScoreLabel,
                        Score = compatibilityScore,
                        Weight = 1,
                        Contribution = compatibilityScore
                    }
                }
            };
        }

        private static CompatibilityDoshaPersonSummary BuildPersonSummary(DoshaRuleMatch? match)
        {
            if (match == null)
            {
                return new CompatibilityDoshaPersonSummary
                {
                    IsPresent = false,
                    Severity = "None",
                    Reason = "This dosha is not present in the individual Kundali."
                };
            }

            return new CompatibilityDoshaPersonSummary
            {
                IsPresent = match.IsPresent,
                Severity = match.Severity,
                Reason = match.Reason,
                InvolvedPlanets = match.InvolvedPlanets,
                InvolvedHouses = match.InvolvedHouses
            };
        }

        private static DoshaComparison Compare(
            string key,
            string personAName,
            string personBName,
            CompatibilityDoshaPersonSummary personA,
            CompatibilityDoshaPersonSummary personB,
            int personASeverity,
            int personBSeverity)
        {
            return key switch
            {
                "manglik" => CompareManglik(personAName, personBName, personA, personB, personASeverity, personBSeverity),
                _ => CompareDefault(personAName, personBName, personA, personB, personASeverity, personBSeverity)
            };
        }

        private static DoshaComparison CompareManglik(
            string personAName,
            string personBName,
            CompatibilityDoshaPersonSummary personA,
            CompatibilityDoshaPersonSummary personB,
            int personASeverity,
            int personBSeverity)
        {
            if (!personA.IsPresent && !personB.IsPresent)
            {
                return NoDosha("Manglik Dosha", "Neither chart has Manglik Dosha from the individual Kundali rules.");
            }

            if (personA.IsPresent && personB.IsPresent)
            {
                var risk = Math.Clamp(15 + Math.Abs(personASeverity - personBSeverity) * 0.35, 15, 40);
                return new DoshaComparison(
                    "Balanced Dosha",
                    risk,
                    $"Both {personAName} and {personBName} are Manglik. Matched Manglik influence balances the major mismatch risk; only severity difference remains.",
                    "Balanced Manglik residual risk = 15 + absolute severity difference x 0.35, clamped to 15-40.",
                    "Balanced Manglik residual risk");
            }

            var name = personA.IsPresent ? personAName : personBName;
            var severity = personA.IsPresent ? personASeverity : personBSeverity;
            return new DoshaComparison(
                "Active Dosha",
                severity,
                $"{name} has Manglik Dosha while the other chart does not, so the one-sided Mars marriage pressure is counted as compatibility risk.",
                "One-sided Manglik risk = present person's severity value.",
                "One-sided Manglik risk");
        }

        private static DoshaComparison CompareDefault(
            string personAName,
            string personBName,
            CompatibilityDoshaPersonSummary personA,
            CompatibilityDoshaPersonSummary personB,
            int personASeverity,
            int personBSeverity)
        {
            if (!personA.IsPresent && !personB.IsPresent)
            {
                return NoDosha("Dosha", "This dosha is not present in either chart.");
            }

            if (personA.IsPresent && personB.IsPresent)
            {
                var risk = (personASeverity + personBSeverity) / 2.0;
                return new DoshaComparison(
                    "Mutual Pressure",
                    risk,
                    $"Both {personAName} and {personBName} have this dosha.",
                    "Mutual dosha risk = average severity value.",
                    "Mutual dosha risk");
            }

            var name = personA.IsPresent ? personAName : personBName;
            var severity = personA.IsPresent ? personASeverity : personBSeverity;
            return new DoshaComparison(
                "Active Dosha",
                severity,
                $"{name} has this dosha while the other chart does not.",
                "One-sided dosha risk = present person's severity value.",
                "One-sided dosha risk");
        }

        private static DoshaComparison NoDosha(string name, string reason)
        {
            return new DoshaComparison(
                "No Dosha",
                0,
                reason,
                $"{name} risk = 0 because the dosha is absent in both charts.",
                $"{name} absence");
        }

        private static int SeverityValue(string? severity)
        {
            return severity?.Trim().ToLowerInvariant() switch
            {
                "low" => 25,
                "medium" => 55,
                "high" => 85,
                _ => 0
            };
        }

        private static string SeverityFromRisk(int riskScore)
        {
            if (riskScore == 0) return "None";
            if (riskScore <= 30) return "Low";
            if (riskScore <= 60) return "Medium";
            return "High";
        }

        private static string ToneFor(string status, int riskScore)
        {
            if (status == "No Dosha") return "green";
            if (status == "Balanced Dosha") return "blue";
            return riskScore >= 70 ? "pink" : "orange";
        }

        private static string GetOverallStatus(IEnumerable<CompatibilityDoshaItem> items)
        {
            var itemList = items.ToList();
            if (itemList.All(item => item.Status == "No Dosha")) return "No Dosha";
            if (itemList.Any(item => item.Severity == "High")) return "High Dosha Attention";
            if (itemList.Any(item => item.Status == "Active Dosha")) return "Active Dosha";
            if (itemList.Any(item => item.Status == "Mutual Pressure")) return "Mutual Pressure";
            return "Balanced Dosha";
        }

        private static string ToTitle(string key)
        {
            return string.Join(" ", key.Split('_').Select(part => char.ToUpperInvariant(part[0]) + part[1..]));
        }

        private sealed record DoshaComparison(
            string Status,
            double RiskScore,
            string Reason,
            string Formula,
            string ScoreLabel);
    }
}
