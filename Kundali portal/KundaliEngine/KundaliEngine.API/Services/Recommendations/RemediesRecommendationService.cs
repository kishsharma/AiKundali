using KundaliEngine.API.DTOs.Responses;
using KundaliEngine.API.Helper_functions;
using KundaliEngine.Astrology.Services.Scoring;
using KundaliEngine.Domain.Enums;
using KundaliEngine.Domain.Models;
using KundaliEngine.KnowledgeBase.Models;
using KundaliEngine.KnowledgeBase.Services;

namespace KundaliEngine.API.Services.Recommendations
{
    public class RemediesRecommendationService
    {
        private readonly AstrologyKnowledgeService _knowledgeService;
        private readonly PlanetScoreService _planetScoreService;

        public RemediesRecommendationService(
            AstrologyKnowledgeService knowledgeService,
            PlanetScoreService planetScoreService)
        {
            _knowledgeService = knowledgeService;
            _planetScoreService = planetScoreService;
        }

        public RemediesRecommendationDto Recommend(
            ZodiacSign ascendantSign,
            ZodiacSign moonSign,
            IReadOnlyList<PlanetPosition> planets,
            CurrentDashaDto currentDasha,
            AdvancedStrengthTableDto strengthTables)
        {
            var planetScores = _planetScoreService.CalculateMany(planets);
            Planet? lagnaLord = ParsePlanet(HelperData.GetSignOwner(ascendantSign));
            Planet? chandraRashiLord = ParsePlanet(HelperData.GetSignOwner(moonSign));
            Planet? mahadashaLord = ParsePlanet(currentDasha.Mahadasha);
            Planet? antardashaLord = ParsePlanet(currentDasha.Antardasha);

            var candidates = planets
                .Select(position => BuildCandidate(
                    position,
                    ascendantSign,
                    lagnaLord,
                    chandraRashiLord,
                    mahadashaLord,
                    antardashaLord,
                    planetScores.First(score => score.Planet == position.Planet),
                    strengthTables))
                .ToList();

            var eligibleCandidates = candidates.Where(candidate => candidate.IsEligible).ToList();
            var suitable = eligibleCandidates
                .Where(candidate => candidate.Score >= 45)
                .OrderByDescending(candidate => candidate.Score)
                .ThenByDescending(candidate => candidate.PlanetScore.Score)
                .ToList();
            var categories = BuildCategoryAnalyses(suitable, lagnaLord, chandraRashiLord, mahadashaLord, antardashaLord);

            RemedyCandidate? primary = suitable.FirstOrDefault();
            RemedyCandidate? alternative = suitable
                .Skip(1)
                .FirstOrDefault(candidate => candidate.Planet != primary?.Planet);

            var avoid = candidates
                .Where(candidate => !candidate.IsEligible)
                .OrderByDescending(candidate => candidate.Score)
                .Take(4)
                .Select(BuildAvoidRecommendation)
                .ToList();

            return new RemediesRecommendationDto
            {
                Primary = primary == null ? null : BuildSelection(primary, "Primary"),
                Alternative = alternative == null ? null : BuildSelection(alternative, "Alternative"),
                Categories = categories,
                Avoid = avoid,
                CandidateEvidence = candidates.Select(BuildEligibilityEvidence).ToList(),
                NonGemstoneOptions = candidates.Where(candidate => !candidate.IsEligible)
                    .SelectMany(candidate => BuildNonGemstoneOptions(candidate.Knowledge))
                    .Distinct(StringComparer.OrdinalIgnoreCase).Take(8).ToList(),
                NoGemstoneReason = primary == null
                    ? eligibleCandidates.Any()
                        ? "No candidate met the minimum recommendation score after the safety checks. Consider non-gem practices or ask a qualified astrologer to review the chart."
                        : "No gemstone passed the suitability and safety checks with the available chart evidence."
                    : string.Empty,
                Summary = BuildSummary(primary, alternative)
            };
        }

        private RemedyCandidate BuildCandidate(
            PlanetPosition position,
            ZodiacSign ascendantSign,
            Planet? lagnaLord,
            Planet? chandraRashiLord,
            Planet? mahadashaLord,
            Planet? antardashaLord,
            PlanetScoreResult planetScore,
            AdvancedStrengthTableDto strengthTables)
        {
            PlanetKnowledge? knowledge = _knowledgeService.GetPlanet(position.Planet.ToString());
            int functionalInfluence = GetFunctionalPlanetInfluence(position.Planet, ascendantSign);
            bool isLagnaLord = position.Planet == lagnaLord;
            bool isChandraRashiLord = position.Planet == chandraRashiLord;
            bool isMahadashaLord = position.Planet == mahadashaLord;
            bool isAntardashaLord = position.Planet == antardashaLord;
            var shadbala = strengthTables.Shadbala.FirstOrDefault(row => row.Planet.Equals(position.Planet.ToString(), StringComparison.OrdinalIgnoreCase));
            bool hasHighRisk = IsHighImpactPlanet(position.Planet) && (functionalInfluence < 0 || position.IsCombust || planetScore.Score < 45);

            int score = 30;
            var factors = new List<string>();

            if (isLagnaLord)
            {
                score += 25;
                factors.Add($"{position.Planet} is the Lagna lord, so it strongly supports chart-level recommendation.");
            }

            if (functionalInfluence > 0)
            {
                score += functionalInfluence * 3;
                factors.Add($"{position.Planet} has functional support for {ascendantSign} ascendant.");
            }
            else if (functionalInfluence < 0)
            {
                score += functionalInfluence * 4;
                factors.Add($"{position.Planet} has functional challenge for {ascendantSign} ascendant.");
            }

            if (isChandraRashiLord)
            {
                score += 14;
                factors.Add($"{position.Planet} is the Chandra Rashi lord, so it can support emotional and lived-experience themes.");
            }

            if (isMahadashaLord)
            {
                score += 14;
                factors.Add($"{position.Planet} is the current Mahadasha lord.");
            }

            if (isAntardashaLord)
            {
                score += 9;
                factors.Add($"{position.Planet} is the current Antardasha lord.");
            }

            int strengthContribution = (int)Math.Round((planetScore.Score - 50) * 0.35);
            score += strengthContribution;
            factors.Add(BuildStrengthFactor(position.Planet, planetScore.Score));

            // Shadbala is only a small ranking preference after the safety gate passes.
            if (shadbala is not null && shadbala.Percent < 100)
            {
                score += 3;
                factors.Add($"Below-required Shadbala ({shadbala.Percent}%); this is a small ranking factor, not gemstone eligibility by itself.");
            }

            if (position.IsCombust)
            {
                score -= 8;
                factors.Add($"{position.Planet} is combust, so direct strengthening needs caution.");
            }

            if (position.IsRetrograde)
            {
                score -= 2;
                factors.Add($"{position.Planet} is retrograde, so results may be more internalized or delayed.");
            }

            if (hasHighRisk)
            {
                score -= 12;
                factors.Add($"{position.Planet} is a high-impact gemstone candidate and needs stricter review.");
            }

            score = ApplyStrengthSafetyCap(score, planetScore.Score, factors);

            var eligibilityReasons = new List<string>();
            if (functionalInfluence <= 0)
                eligibilityReasons.Add("Functional suitability is not positive for this ascendant, so strengthening this planet is not recommended by this rule set.");
            if (position.IsCombust)
                eligibilityReasons.Add("Combustion requires caution before strengthening this planet.");
            if (planetScore.Score < 45)
                eligibilityReasons.Add("Planetary strength score is below the minimum review threshold.");
            if (shadbala is null)
                eligibilityReasons.Add("Required Shadbala evidence is unavailable for this planet.");
            if (hasHighRisk)
                eligibilityReasons.Add("This high-impact planet has a chart risk flag and needs individual expert review.");
            if (knowledge?.GemstoneRecommendation.PrimaryGemstone is null || string.IsNullOrWhiteSpace(knowledge.GemstoneRecommendation.PrimaryGemstone.Name))
                eligibilityReasons.Add("Gemstone catalog data is incomplete.");
            bool isEligible = eligibilityReasons.Count == 0;

            if (factors.Count == 1)
            {
                factors.Add($"{position.Planet} is included for full chart comparison, but it has no dominant recommendation trigger.");
            }

            return new RemedyCandidate(
                position.Planet,
                position,
                knowledge,
                planetScore,
                Math.Clamp(score, 0, 100),
                functionalInfluence,
                isLagnaLord,
                isChandraRashiLord,
                isMahadashaLord,
                isAntardashaLord,
                hasHighRisk,
                factors,
                isEligible,
                eligibilityReasons,
                shadbala,
                BuildEvidence(position, ascendantSign, knowledge, planetScore, shadbala, strengthTables));
        }

        private static List<RemedyCategoryAnalysisDto> BuildCategoryAnalyses(
            IReadOnlyList<RemedyCandidate> candidates,
            Planet? lagnaLord,
            Planet? chandraRashiLord,
            Planet? mahadashaLord,
            Planet? antardashaLord)
        {
            var categories = new List<RemedyCategoryAnalysisDto>();

            AddCategory(categories, "LagnaBased", candidates.FirstOrDefault(candidate => candidate.Planet == lagnaLord));
            AddCategory(categories, "ChandraRashiBased", candidates.FirstOrDefault(candidate => candidate.Planet == chandraRashiLord));
            AddCategory(categories, "MahadashaBased", candidates.FirstOrDefault(candidate => candidate.Planet == mahadashaLord));
            AddCategory(categories, "AntardashaBased", candidates.FirstOrDefault(candidate => candidate.Planet == antardashaLord));

            return categories;
        }

        private static void AddCategory(List<RemedyCategoryAnalysisDto> categories, string category, RemedyCandidate? candidate)
        {
            if (candidate == null)
            {
                return;
            }

            categories.Add(new RemedyCategoryAnalysisDto
            {
                Category = category,
                Planet = candidate.Planet.ToString(),
                RecommendedRemedy = candidate.Knowledge?.GemstoneRecommendation.PrimaryGemstone.Name ?? string.Empty,
                Score = candidate.Score,
                Suitability = GetSuitability(candidate.Score),
                Reasons = candidate.Factors.Take(4).ToList()
            });
        }

        private static RemedySelectionDto BuildSelection(RemedyCandidate candidate, string role)
        {
            GemstoneRecommendationItemKnowledge gemstone = candidate.Knowledge!.GemstoneRecommendation.PrimaryGemstone;

            return new RemedySelectionDto
            {
                RemedyType = "Gemstone",
                RecommendationRole = role,
                Planet = candidate.Planet.ToString(),
                Name = gemstone.Name,
                SanskritName = gemstone.SanskritName,
                EnglishName = gemstone.EnglishName,
                Category = gemstone.Category,
                StrengthLevel = gemstone.StrengthLevel,
                Confidence = candidate.Score,
                Suitability = GetSuitability(candidate.Score),
                Reason = BuildReason(candidate),
                Factors = candidate.Factors,
                Remedies = MapRemedyCatalog(candidate.Knowledge.Remedies),
                Evidence = candidate.Evidence
            };
        }

        private static RemedyAvoidRecommendationDto BuildAvoidRecommendation(RemedyCandidate candidate)
        {
            return new RemedyAvoidRecommendationDto
            {
                Planet = candidate.Planet.ToString(),
                RemedyName = candidate.Knowledge?.GemstoneRecommendation.PrimaryGemstone.Name ?? string.Empty,
                Reason = string.Join(" ", candidate.EligibilityReasons),
                Factors = candidate.EligibilityReasons.Take(4).ToList()
            };
        }

        private static GemstoneEligibilityDto BuildEligibilityEvidence(RemedyCandidate candidate) => new()
        {
            Planet = candidate.Planet.ToString(),
            Gemstone = candidate.Knowledge?.GemstoneRecommendation.PrimaryGemstone.Name ?? string.Empty,
            Eligible = candidate.IsEligible,
            Status = candidate.IsEligible ? "Eligible" : "NotRecommendedFromAvailableEvidence",
            Reasons = candidate.IsEligible
                ? new List<string> { "Passed the current functional suitability and safety checks." }
                : candidate.EligibilityReasons,
            Evidence = candidate.Evidence
        };

        private GemstoneEvidenceDto BuildEvidence(
            PlanetPosition position,
            ZodiacSign ascendantSign,
            PlanetKnowledge? knowledge,
            PlanetScoreResult planetScore,
            ShadbalaRowDto? shadbala,
            AdvancedStrengthTableDto strengthTables)
        {
            var relevantHouses = new HashSet<int> { (int)position.House };
            foreach (ZodiacSign sign in Enum.GetValues<ZodiacSign>())
            {
                if (ParsePlanet(HelperData.GetSignOwner(sign)) == position.Planet)
                    relevantHouses.Add(((int)sign - (int)ascendantSign + 12) % 12 + 1);
            }

            string dignity = GetDignity(knowledge, position.Sign);

            var risks = new List<string>();
            if (position.IsCombust) risks.Add("Combust");
            if (position.IsRetrograde) risks.Add("Retrograde");
            if (planetScore.Score < 45) risks.Add("Low Planetary Strength score");
            return new GemstoneEvidenceDto
            {
                Dignity = dignity,
                PlanetaryStrengthScore = planetScore.Score,
                ShadbalaTotal = shadbala?.Total,
                ShadbalaRequired = shadbala?.Required,
                ShadbalaPercent = shadbala?.Percent,
                RiskFactors = risks,
                HouseSupport = strengthTables.HouseSupport.Where(row => relevantHouses.Contains(row.House))
                    .Select(row => new HouseSupportEvidenceDto { House = row.House, Score = row.Score, SupportLevel = row.SupportLevel, IsEstimated = true }).ToList()
            };
        }

        private string GetDignity(PlanetKnowledge? knowledge, ZodiacSign sign)
        {
            if (knowledge is null) return "Unavailable";
            string signName = sign.ToString();
            if (knowledge.Exaltation.Equals(signName, StringComparison.OrdinalIgnoreCase)) return "Exalted";
            if (knowledge.Debilitation.Equals(signName, StringComparison.OrdinalIgnoreCase)) return "Debilitated";
            if (knowledge.Moolatrikona.Equals(signName, StringComparison.OrdinalIgnoreCase)) return "Moolatrikona";
            if (knowledge.OwnSigns.Contains(signName, StringComparer.OrdinalIgnoreCase)) return "Own Sign";

            var signLord = _knowledgeService.GetPlanets().FirstOrDefault(planet => planet.OwnSigns.Contains(signName, StringComparer.OrdinalIgnoreCase));
            if (signLord is null) return "Neutral Sign";
            if (knowledge.Friends.Contains(signLord.Name, StringComparer.OrdinalIgnoreCase)) return "Friendly Sign";
            if (knowledge.Enemies.Contains(signLord.Name, StringComparer.OrdinalIgnoreCase)) return "Enemy Sign";
            return "Neutral Sign";
        }

        private static IEnumerable<string> BuildNonGemstoneOptions(PlanetKnowledge? knowledge)
        {
            if (knowledge is null) yield break;
            if (!string.IsNullOrWhiteSpace(knowledge.Remedies.Yantra.Name)) yield return $"Yantra: {knowledge.Remedies.Yantra.Name}";
            if (!string.IsNullOrWhiteSpace(knowledge.Remedies.Japa.Mantra)) yield return $"Japa: {knowledge.Remedies.Japa.Mantra}";
            foreach (var item in knowledge.Remedies.Daan.Where(value => !string.IsNullOrWhiteSpace(value))) yield return $"Daan: {item}";
        }

        private static RemedyCatalogDto MapRemedyCatalog(PlanetRemediesKnowledge remedies)
        {
            return new RemedyCatalogDto
            {
                Gemstone = new GemstoneRemedyCatalogDto
                {
                    Primary = MapStone(remedies.Gemstone.Primary),
                    Alternatives = remedies.Gemstone.Alternatives
                        .Select(stone => new RemedyStoneDto
                        {
                            Name = stone.Name,
                            Purpose = stone.Purpose
                        })
                        .ToList(),
                    WearingRules = new WearingRulesDto
                    {
                        Finger = remedies.Gemstone.WearingRules.Finger,
                        DayToWear = remedies.Gemstone.WearingRules.DayToWear,
                        Time = remedies.Gemstone.WearingRules.Time,
                        Metal = remedies.Gemstone.WearingRules.Metal
                    },
                    Caution = remedies.Gemstone.Caution
                },
                Yantra = new YantraRemedyDto
                {
                    Name = remedies.Yantra.Name,
                    Purpose = remedies.Yantra.Purpose,
                    ApplicableWhen = remedies.Yantra.ApplicableWhen
                },
                Daan = remedies.Daan,
                Japa = new JapaRemedyDto
                {
                    Mantra = remedies.Japa.Mantra,
                    Count = remedies.Japa.Count,
                    Day = remedies.Japa.Day
                },
                AdditionalRemedies = new List<object>()
            };
        }

        private static RemedyStoneDto MapStone(RatnaRemedyKnowledge stone)
        {
            return new RemedyStoneDto
            {
                Name = stone.Name,
                SanskritName = stone.SanskritName,
                EnglishName = stone.EnglishName,
                Purpose = stone.Purpose,
                RecommendedFor = stone.RecommendedFor
            };
        }

        private static string BuildReason(RemedyCandidate candidate)
        {
            string gemstone = candidate.Knowledge?.GemstoneRecommendation.PrimaryGemstone.Name ?? "the gemstone";
            return $"{gemstone} is selected because {candidate.Planet} has the strongest overall remedy suitability after checking Lagna, Chandra Rashi, dasha, strength, and caution factors.";
        }

        private static string BuildSummary(RemedyCandidate? primary, RemedyCandidate? alternative)
        {
            if (primary == null)
            {
                return "No gemstone is strongly recommended from the current first-pass remedy analysis; use yantra, daan, japa, or expert review before wearing gemstones.";
            }

            string summary = $"Primary remedy recommendation is {primary.Knowledge?.GemstoneRecommendation.PrimaryGemstone.Name} for {primary.Planet}.";
            if (alternative != null)
            {
                summary += $" Alternative consideration is {alternative.Knowledge?.GemstoneRecommendation.PrimaryGemstone.Name} for {alternative.Planet}.";
            }

            return summary;
        }

        private static int GetFunctionalPlanetInfluence(Planet planet, ZodiacSign ascendantSign)
        {
            if (planet is Planet.Rahu or Planet.Ketu)
            {
                return 0;
            }

            var ruledHouses = GetRuledHouses(planet, ascendantSign).ToList();
            if (ruledHouses.Count == 0)
            {
                return 0;
            }

            bool rulesKendra = ruledHouses.Any(IsKendra);
            bool rulesTrikona = ruledHouses.Any(IsTrikona);
            int score = 0;

            foreach (HouseNumber ruledHouse in ruledHouses)
            {
                score += ruledHouse switch
                {
                    HouseNumber.First => 5,
                    HouseNumber.Fifth or HouseNumber.Ninth => 6,
                    HouseNumber.Fourth or HouseNumber.Tenth => 3,
                    HouseNumber.Seventh => 0,
                    HouseNumber.Third or HouseNumber.Eleventh => 1,
                    HouseNumber.Second => -2,
                    HouseNumber.Sixth or HouseNumber.Eighth or HouseNumber.Twelfth => -5,
                    _ => 0
                };
            }

            if (rulesKendra && rulesTrikona)
            {
                score += 6;
            }

            return Math.Clamp(score, -8, 10);
        }

        private static IEnumerable<HouseNumber> GetRuledHouses(Planet planet, ZodiacSign ascendantSign)
        {
            foreach (ZodiacSign sign in Enum.GetValues<ZodiacSign>())
            {
                if (ParsePlanet(HelperData.GetSignOwner(sign)) != planet)
                {
                    continue;
                }

                int houseNumber = ((int)sign - (int)ascendantSign + 12) % 12 + 1;
                yield return (HouseNumber)houseNumber;
            }
        }

        private static bool IsKendra(HouseNumber houseNumber)
        {
            return houseNumber is HouseNumber.First or HouseNumber.Fourth or HouseNumber.Seventh or HouseNumber.Tenth;
        }

        private static bool IsTrikona(HouseNumber houseNumber)
        {
            return houseNumber is HouseNumber.First or HouseNumber.Fifth or HouseNumber.Ninth;
        }

        private static bool IsHighImpactPlanet(Planet planet)
        {
            return planet is Planet.Saturn or Planet.Rahu or Planet.Ketu or Planet.Mars;
        }

        private static string BuildStrengthFactor(Planet planet, int score)
        {
            if (score <= 50)
            {
                return $"{planet} has neutral planet strength ({score}/100), so this recommendation is based on functional suitability rather than raw planetary strength.";
            }

            return $"{planet} has supportive planet strength ({score}/100).";
        }

        private static int ApplyStrengthSafetyCap(int score, int planetStrengthScore, List<string> factors)
        {
            if (planetStrengthScore > 50)
            {
                return score;
            }

            int cappedScore = Math.Min(score, 68);
            if (cappedScore < score)
            {
                factors.Add("Neutral planet strength caps the remedy suitability below Highly Recommended until deeper review confirms direct gemstone strengthening.");
            }

            return cappedScore;
        }

        private static string GetSuitability(int score)
        {
            return score switch
            {
                >= 75 => "HighlyRecommended",
                >= 60 => "Recommended",
                >= 45 => "RecommendedWithCaution",
                >= 35 => "AlternativePreferred",
                _ => "NotRecommended"
            };
        }

        private static Planet? ParsePlanet(string? value)
        {
            return Enum.TryParse(value, ignoreCase: true, out Planet planet)
                ? planet
                : null;
        }

        private sealed record RemedyCandidate(
            Planet Planet,
            PlanetPosition Position,
            PlanetKnowledge? Knowledge,
            PlanetScoreResult PlanetScore,
            int Score,
            int FunctionalInfluence,
            bool IsLagnaLord,
            bool IsChandraRashiLord,
            bool IsMahadashaLord,
            bool IsAntardashaLord,
            bool HasHighRisk,
            List<string> Factors,
            bool IsEligible,
            List<string> EligibilityReasons,
            ShadbalaRowDto? Shadbala,
            GemstoneEvidenceDto Evidence);
    }
}
