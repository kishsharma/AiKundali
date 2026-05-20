using KundaliEngine.Domain.Enums;
using KundaliEngine.Domain.Models;
using KundaliEngine.KnowledgeBase.Models;
using KundaliEngine.KnowledgeBase.Services;

namespace KundaliEngine.Astrology.Services.Scoring
{
    public class OverallKundaliScoreService
    {
        private const int MinimumScore = 0;
        private const int MaximumScore = 100;

        private static readonly IReadOnlyDictionary<string, HouseNumber[]> HouseGroups =
            new Dictionary<string, HouseNumber[]>(StringComparer.OrdinalIgnoreCase)
            {
                ["Dharma"] = new[] { HouseNumber.First, HouseNumber.Fifth, HouseNumber.Ninth },
                ["Artha"] = new[] { HouseNumber.Second, HouseNumber.Sixth, HouseNumber.Tenth },
                ["Kama"] = new[] { HouseNumber.Third, HouseNumber.Seventh, HouseNumber.Eleventh },
                ["Moksha"] = new[] { HouseNumber.Fourth, HouseNumber.Eighth, HouseNumber.Twelfth }
            };

        private readonly AstrologyKnowledgeService _knowledgeService;
        private readonly PlanetScoreService _planetScoreService;
        private readonly HouseScoreService _houseScoreService;

        public OverallKundaliScoreService()
            : this(new AstrologyKnowledgeService())
        {
        }

        public OverallKundaliScoreService(AstrologyKnowledgeService knowledgeService)
            : this(
                knowledgeService,
                new PlanetScoreService(knowledgeService),
                new HouseScoreService(knowledgeService))
        {
        }

        public OverallKundaliScoreService(
            AstrologyKnowledgeService knowledgeService,
            PlanetScoreService planetScoreService,
            HouseScoreService houseScoreService)
        {
            _knowledgeService = knowledgeService;
            _planetScoreService = planetScoreService;
            _houseScoreService = houseScoreService;
        }

        public OverallKundaliScoreResult Calculate(
            ZodiacSign ascendantSign,
            IReadOnlyList<PlanetPosition> planetPositions,
            AspectCalculationResult? aspectResult = null,
            Planet? activeMahadashaLord = null)
        {
            if (planetPositions is null)
            {
                throw new ArgumentNullException(nameof(planetPositions));
            }

            var factors = new List<string>();
            var planetScores = _planetScoreService.CalculateMany(planetPositions);
            var houseScores = _houseScoreService.CalculateAllHouseScores(
                ascendantSign,
                planetPositions,
                aspectResult);

            int lagnaScore = ScoreLagna(ascendantSign, planetScores, houseScores, factors);
            int moonScore = ScoreMoon(planetScores, factors);
            int dharmaScore = ScoreHouseGroup("Dharma", houseScores, factors);
            int arthaScore = ScoreHouseGroup("Artha", houseScores, factors);
            int kamaScore = ScoreHouseGroup("Kama", houseScores, factors);
            int mokshaScore = ScoreHouseGroup("Moksha", houseScores, factors);
            int planetaryBalanceScore = ScorePlanetaryBalance(planetScores, factors);
            int dashaScore = ScoreCurrentDasha(activeMahadashaLord, planetScores, factors);

            int overall = WeightedScore(new[]
            {
                (lagnaScore, 0.20),
                (moonScore, 0.15),
                (dharmaScore, 0.15),
                (arthaScore, 0.15),
                (kamaScore, 0.10),
                (mokshaScore, 0.10),
                (planetaryBalanceScore, 0.10),
                (dashaScore, 0.05)
            });

            int stability = WeightedScore(new[]
            {
                (lagnaScore, 0.35),
                (moonScore, 0.35),
                (mokshaScore, 0.15),
                (planetaryBalanceScore, 0.15)
            });

            int growth = WeightedScore(new[]
            {
                (dharmaScore, 0.30),
                (arthaScore, 0.30),
                (kamaScore, 0.20),
                (planetaryBalanceScore, 0.10),
                (dashaScore, 0.10)
            });

            int challengePressure = ScoreChallengePressure(
                planetScores,
                houseScores,
                mokshaScore,
                dashaScore,
                factors);

            return new OverallKundaliScoreResult
            {
                OverallScore = overall,
                StabilityScore = stability,
                GrowthScore = growth,
                ChallengeScore = challengePressure,
                DominantStrengths = BuildDominantStrengths(
                    lagnaScore,
                    moonScore,
                    dharmaScore,
                    arthaScore,
                    kamaScore,
                    mokshaScore,
                    planetaryBalanceScore,
                    dashaScore,
                    houseScores,
                    planetScores),
                DominantWeaknesses = BuildDominantWeaknesses(
                    lagnaScore,
                    moonScore,
                    dharmaScore,
                    arthaScore,
                    kamaScore,
                    mokshaScore,
                    planetaryBalanceScore,
                    dashaScore,
                    houseScores,
                    planetScores),
                Factors = factors
            };
        }

        private int ScoreLagna(
            ZodiacSign ascendantSign,
            IReadOnlyList<PlanetScoreResult> planetScores,
            IReadOnlyList<HouseScoreResult> houseScores,
            List<string> factors)
        {
            int firstHouseScore = GetHouseScore(houseScores, HouseNumber.First);
            Planet? lagnaLord = GetSignLord(ascendantSign);
            int lagnaLordScore = lagnaLord.HasValue
                ? GetPlanetScore(planetScores, lagnaLord.Value)
                : 50;

            int score = WeightedScore(new[]
            {
                (firstHouseScore, 0.55),
                (lagnaLordScore, 0.45)
            });

            factors.Add(FormatFactor(
                score,
                $"Lagna foundation combines First House strength ({firstHouseScore}/100) and {ascendantSign} lord {lagnaLord?.ToString() ?? "Unknown"} ({lagnaLordScore}/100)"));

            return score;
        }

        private static int ScoreMoon(
            IReadOnlyList<PlanetScoreResult> planetScores,
            List<string> factors)
        {
            int score = GetPlanetScore(planetScores, Planet.Moon);

            factors.Add(FormatFactor(
                score,
                $"Moon strength contributes to emotional stability, adaptability, and inner balance ({score}/100)"));

            return score;
        }

        private static int ScoreHouseGroup(
            string groupName,
            IReadOnlyList<HouseScoreResult> houseScores,
            List<string> factors)
        {
            HouseNumber[] houses = HouseGroups[groupName];
            var scores = houses.Select(house => GetHouseScore(houseScores, house)).ToList();
            int score = Clamp((int)Math.Round(scores.Average()));

            factors.Add(FormatFactor(
                score,
                $"{groupName} houses ({string.Join(", ", houses.Select(h => (int)h))}) aggregate to {score}/100"));

            return score;
        }

        private static int ScorePlanetaryBalance(
            IReadOnlyList<PlanetScoreResult> planetScores,
            List<string> factors)
        {
            if (planetScores.Count == 0)
            {
                factors.Add("+0 Planetary balance unavailable because no planet scores were provided");
                return 50;
            }

            double average = planetScores.Average(score => score.Score);
            int strongCount = planetScores.Count(score => score.Score >= 70);
            int weakCount = planetScores.Count(score => score.Score <= 40);
            int balance = Clamp((int)Math.Round(average + strongCount * 2 - weakCount * 3));

            factors.Add(FormatFactor(
                balance,
                $"Planetary balance reflects average planetary capacity {average:F0}/100 with {strongCount} strong and {weakCount} weak planets"));

            return balance;
        }

        private static int ScoreCurrentDasha(
            Planet? activeMahadashaLord,
            IReadOnlyList<PlanetScoreResult> planetScores,
            List<string> factors)
        {
            if (!activeMahadashaLord.HasValue)
            {
                factors.Add("+0 Current dasha influence not applied because active Mahadasha lord was not provided");
                return 50;
            }

            int score = GetPlanetScore(planetScores, activeMahadashaLord.Value);

            factors.Add(FormatFactor(
                score,
                $"Current Mahadasha lord {activeMahadashaLord.Value} contributes current manifestation support ({score}/100)"));

            return score;
        }

        private static int ScoreChallengePressure(
            IReadOnlyList<PlanetScoreResult> planetScores,
            IReadOnlyList<HouseScoreResult> houseScores,
            int mokshaScore,
            int dashaScore,
            List<string> factors)
        {
            int weakPlanetCount = planetScores.Count(score => score.Score <= 40);
            int weakHouseCount = houseScores.Count(score => score.Score <= 40);
            int pressure = 25 + weakPlanetCount * 6 + weakHouseCount * 5;

            if (mokshaScore < 45)
            {
                pressure += 8;
            }

            if (dashaScore < 45)
            {
                pressure += 6;
            }

            pressure = Clamp(pressure);

            factors.Add(FormatFactor(
                -pressure,
                $"Challenge pressure is {pressure}/100 from {weakPlanetCount} weak planets, {weakHouseCount} weak houses, Moksha score {mokshaScore}/100, and dasha support {dashaScore}/100"));

            return pressure;
        }

        private Planet? GetSignLord(ZodiacSign sign)
        {
            string signName = sign.ToString();

            PlanetKnowledge? lordKnowledge = _knowledgeService.GetPlanets()
                .FirstOrDefault(planet => planet.OwnSigns.Any(ownSign =>
                    ownSign.Equals(signName, StringComparison.OrdinalIgnoreCase)));

            return Enum.TryParse(lordKnowledge?.Name, out Planet lord)
                ? lord
                : null;
        }

        private static List<string> BuildDominantStrengths(
            int lagnaScore,
            int moonScore,
            int dharmaScore,
            int arthaScore,
            int kamaScore,
            int mokshaScore,
            int planetaryBalanceScore,
            int dashaScore,
            IReadOnlyList<HouseScoreResult> houseScores,
            IReadOnlyList<PlanetScoreResult> planetScores)
        {
            var strengths = new List<(string Label, int Score)>
            {
                ("Strong Lagna supports identity, vitality, and life direction", lagnaScore),
                ("Strong Moon supports emotional balance and adaptability", moonScore),
                ("Strong Dharma houses support purpose, wisdom, and alignment", dharmaScore),
                ("Strong Artha houses support execution, profession, and resources", arthaScore),
                ("Strong Kama houses support relationships, ambition, and social fulfillment", kamaScore),
                ("Strong Moksha houses support inner depth, release, and transformation", mokshaScore),
                ("Planetary balance supports overall manifestation capacity", planetaryBalanceScore),
                ("Current dasha lord supports present activation", dashaScore)
            };

            strengths.AddRange(houseScores
                .Where(house => house.Score >= 75)
                .Select(house => ($"Powerful {house.HouseName} supports {string.Join(", ", house.DominantThemes.Take(2))}", house.Score)));

            strengths.AddRange(planetScores
                .Where(planet => planet.Score >= 75)
                .Select(planet => ($"Strong {planet.Planet} improves chart-level delivery", planet.Score)));

            return strengths
                .Where(item => item.Score >= 70)
                .OrderByDescending(item => item.Score)
                .Take(6)
                .Select(item => item.Label)
                .ToList();
        }

        private static List<string> BuildDominantWeaknesses(
            int lagnaScore,
            int moonScore,
            int dharmaScore,
            int arthaScore,
            int kamaScore,
            int mokshaScore,
            int planetaryBalanceScore,
            int dashaScore,
            IReadOnlyList<HouseScoreResult> houseScores,
            IReadOnlyList<PlanetScoreResult> planetScores)
        {
            var weaknesses = new List<(string Label, int Score)>
            {
                ("Weak Lagna may reduce resilience and clear life direction", lagnaScore),
                ("Weak Moon may reduce emotional steadiness and inner balance", moonScore),
                ("Weak Dharma houses may reduce purpose, guidance, and alignment", dharmaScore),
                ("Weak Artha houses may reduce execution, profession, and material productivity", arthaScore),
                ("Weak Kama houses may reduce relationship flow, ambition, and social fulfillment", kamaScore),
                ("Weak Moksha houses may increase emotional pressure and inner turbulence", mokshaScore),
                ("Weak planetary balance may reduce overall chart delivery", planetaryBalanceScore),
                ("Weak current dasha lord may reduce present manifestation support", dashaScore)
            };

            weaknesses.AddRange(houseScores
                .Where(house => house.Score <= 40)
                .Select(house => ($"Challenged {house.HouseName} needs attention in {string.Join(", ", house.DominantThemes.Take(2))}", house.Score)));

            weaknesses.AddRange(planetScores
                .Where(planet => planet.Score <= 40)
                .Select(planet => ($"Weak {planet.Planet} may reduce related chart delivery", planet.Score)));

            return weaknesses
                .Where(item => item.Score <= 45)
                .OrderBy(item => item.Score)
                .Take(6)
                .Select(item => item.Label)
                .ToList();
        }

        private static int GetHouseScore(IReadOnlyList<HouseScoreResult> houseScores, HouseNumber houseNumber)
        {
            return houseScores.FirstOrDefault(house => house.HouseNumber == houseNumber)?.Score ?? 50;
        }

        private static int GetPlanetScore(IReadOnlyList<PlanetScoreResult> planetScores, Planet planet)
        {
            return planetScores.FirstOrDefault(score => score.Planet == planet)?.Score ?? 50;
        }

        private static int WeightedScore(IEnumerable<(int Score, double Weight)> weightedScores)
        {
            var items = weightedScores.ToList();
            double totalWeight = items.Sum(item => item.Weight);

            if (totalWeight <= 0)
            {
                return 50;
            }

            return Clamp((int)Math.Round(items.Sum(item => item.Score * item.Weight) / totalWeight));
        }

        private static int Clamp(int score)
        {
            return Math.Clamp(score, MinimumScore, MaximumScore);
        }

        private static string FormatFactor(int score, string reason)
        {
            string prefix = score >= 50
                ? "+"
                : "-";

            return $"{prefix} {reason}";
        }
    }
}
