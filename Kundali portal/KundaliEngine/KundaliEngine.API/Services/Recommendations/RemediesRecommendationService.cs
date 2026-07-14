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
            CurrentDashaDto currentDasha)
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
                    planetScores.First(score => score.Planet == position.Planet)))
                .Where(candidate => candidate.Knowledge?.GemstoneRecommendation?.PrimaryGemstone != null)
                .ToList();

            var categories = BuildCategoryAnalyses(candidates, lagnaLord, chandraRashiLord, mahadashaLord, antardashaLord);
            var suitable = candidates
                .Where(candidate => candidate.Score >= 45)
                .OrderByDescending(candidate => candidate.Score)
                .ThenByDescending(candidate => candidate.PlanetScore.Score)
                .ToList();

            RemedyCandidate? primary = suitable.FirstOrDefault();
            RemedyCandidate? alternative = suitable
                .Skip(1)
                .FirstOrDefault(candidate => candidate.Planet != primary?.Planet);

            var avoid = candidates
                .Where(candidate => candidate.Score < 35 || candidate.FunctionalInfluence <= -5 || candidate.HasHighRisk)
                .OrderBy(candidate => candidate.Score)
                .Take(4)
                .Select(BuildAvoidRecommendation)
                .ToList();

            return new RemediesRecommendationDto
            {
                Primary = primary == null ? null : BuildSelection(primary, "Primary"),
                Alternative = alternative == null ? null : BuildSelection(alternative, "Alternative"),
                Categories = categories,
                Avoid = avoid,
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
            PlanetScoreResult planetScore)
        {
            PlanetKnowledge? knowledge = _knowledgeService.GetPlanet(position.Planet.ToString());
            int functionalInfluence = GetFunctionalPlanetInfluence(position.Planet, ascendantSign);
            bool isLagnaLord = position.Planet == lagnaLord;
            bool isChandraRashiLord = position.Planet == chandraRashiLord;
            bool isMahadashaLord = position.Planet == mahadashaLord;
            bool isAntardashaLord = position.Planet == antardashaLord;
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
                factors);
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
                Remedies = MapRemedyCatalog(candidate.Knowledge.Remedies)
            };
        }

        private static RemedyAvoidRecommendationDto BuildAvoidRecommendation(RemedyCandidate candidate)
        {
            return new RemedyAvoidRecommendationDto
            {
                Planet = candidate.Planet.ToString(),
                RemedyName = candidate.Knowledge?.GemstoneRecommendation.PrimaryGemstone.Name ?? string.Empty,
                Reason = $"{candidate.Planet} is not a safe first recommendation in this chart context.",
                Factors = candidate.Factors.Take(4).ToList()
            };
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
            List<string> Factors);
    }
}
