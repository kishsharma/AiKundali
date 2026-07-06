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

        // inside Calculate(...)

        public OverallKundaliScoreResult Calculate(
                   ZodiacSign ascendantSign,
                   IReadOnlyList<PlanetPosition> planetPositions,
                   AspectCalculationResult? aspectResult = null,
                   Planet? activeMahadashaLord = null)
        {
            if (planetPositions is null)
                throw new ArgumentNullException(nameof(planetPositions));

            // Per-category factor lists
            var overallFactors = new List<string>();
            var stabilityFactors = new List<string>();
            var growthFactors = new List<string>();
            var challengeFactors = new List<string>();

            var planetScores = _planetScoreService.CalculateMany(planetPositions);
            var houseScores = _houseScoreService.CalculateAllHouseScores(
                ascendantSign,
                planetPositions,
                aspectResult);

            // Reuse a temporary list for individual helper factor capture
            var tmp = new List<string>();

            // Lagna - contributes to Overall & Stability
            int lagnaScore = ScoreLagna(ascendantSign, planetScores, houseScores, tmp);
            overallFactors.AddRange(tmp);
            stabilityFactors.AddRange(tmp);
            tmp.Clear();

            // Moon - contributes to Overall & Stability
            int moonScore = ScoreMoon(planetScores, tmp);
            overallFactors.AddRange(tmp);
            stabilityFactors.AddRange(tmp);
            tmp.Clear();

            // Sun - contributes to Overall & Stability
            int sunScore = ScoreSun(planetScores, tmp);
            overallFactors.AddRange(tmp);
            stabilityFactors.AddRange(tmp);
            tmp.Clear();

            // Core identity - contributes to Overall
            int coreIdentityScore = ScoreCoreIdentity(lagnaScore, moonScore, sunScore, tmp);
            overallFactors.AddRange(tmp);
            tmp.Clear();

            // Kendra power - Overall & Stability
            int kendraPowerScore = ScoreHouseSet("Kendra", new[] { HouseNumber.First, HouseNumber.Fourth, HouseNumber.Seventh, HouseNumber.Tenth }, houseScores, tmp);
            overallFactors.AddRange(tmp);
            stabilityFactors.AddRange(tmp);
            tmp.Clear();

            // Trikona power - Overall & Growth
            int trikonaPowerScore = ScoreHouseSet("Trikona", new[] { HouseNumber.First, HouseNumber.Fifth, HouseNumber.Ninth }, houseScores, tmp);
            overallFactors.AddRange(tmp);
            growthFactors.AddRange(tmp);
            tmp.Clear();

            // Dharma / Artha / Kama / Moksha
            int dharmaScore = ScoreHouseGroup("Dharma", houseScores, tmp);
            overallFactors.AddRange(tmp);
            growthFactors.AddRange(tmp);
            tmp.Clear();

            int arthaScore = ScoreHouseGroup("Artha", houseScores, tmp);
            overallFactors.AddRange(tmp);
            growthFactors.AddRange(tmp);
            tmp.Clear();

            int kamaScore = ScoreHouseGroup("Kama", houseScores, tmp);
            overallFactors.AddRange(tmp);
            growthFactors.AddRange(tmp);
            tmp.Clear();

            int mokshaScore = ScoreHouseGroup("Moksha", houseScores, tmp);
            overallFactors.AddRange(tmp);
            // moksha contributes to challenge explanation primarily
            challengeFactors.AddRange(tmp);
            tmp.Clear();

            // Planetary balance - Overall & Growth
            int planetaryBalanceScore = ScorePlanetaryBalance(planetScores, tmp);
            overallFactors.AddRange(tmp);
            growthFactors.AddRange(tmp);
            tmp.Clear();

            // Current dasha note (already adds to tmp via method)
            ScoreCurrentDasha(activeMahadashaLord, tmp);
            overallFactors.AddRange(tmp);
            tmp.Clear();

            // Build numeric component lists for structured breakdown
            var overallComponents = new List<(string Label, int Score, double Weight)>
            {
                ("Core identity", coreIdentityScore, 0.20),
                ("Kendra power", kendraPowerScore, 0.18),
                ("Trikona power", trikonaPowerScore, 0.18),
                ("Dharma", dharmaScore, 0.12),
                ("Artha", arthaScore, 0.12),
                ("Kama", kamaScore, 0.08),
                ("Moksha", mokshaScore, 0.07),
                ("Planetary balance", planetaryBalanceScore, 0.05)
            };

            var stabilityComponents = new List<(string Label, int Score, double Weight)>
            {
                ("Lagna", lagnaScore, 0.35),
                ("Moon", moonScore, 0.35),
                ("Sun", sunScore, 0.15),
                ("Kendra power", kendraPowerScore, 0.15)
            };

            var growthComponents = new List<(string Label, int Score, double Weight)>
            {
                ("Trikona power", trikonaPowerScore, 0.30),
                ("Dharma", dharmaScore, 0.25),
                ("Artha", arthaScore, 0.20),
                ("Kama", kamaScore, 0.15),
                ("Planetary balance", planetaryBalanceScore, 0.10)
            };

            // Challenge breakdown uses the challenge pressure calculation
            tmp = new List<string>();
            int challengePressure = ScoreChallengePressure(
                planetScores,
                houseScores,
                mokshaScore,
                tmp);

            // compute final weighted scores
            int overall = WeightedScore(overallComponents.Select(c => ((int)c.Score, c.Weight)));
            int stability = WeightedScore(stabilityComponents.Select(c => ((int)c.Score, c.Weight)));
            int growth = WeightedScore(growthComponents.Select(c => ((int)c.Score, c.Weight)));
            int challenge = Clamp(challengePressure);

            var result = new OverallKundaliScoreResult
            {
                OverallScore = overall,
                StabilityScore = stability,
                GrowthScore = growth,
                ChallengeScore = challenge,
                DominantStrengths = BuildDominantStrengths(
                    lagnaScore,
                    moonScore,
                    sunScore,
                    coreIdentityScore,
                    kendraPowerScore,
                    trikonaPowerScore,
                    dharmaScore,
                    arthaScore,
                    kamaScore,
                    mokshaScore,
                    planetaryBalanceScore,
                    houseScores,
                    planetScores),
                DominantWeaknesses = BuildDominantWeaknesses(
                    lagnaScore,
                    moonScore,
                    sunScore,
                    coreIdentityScore,
                    kendraPowerScore,
                    trikonaPowerScore,
                    dharmaScore,
                    arthaScore,
                    kamaScore,
                    mokshaScore,
                    planetaryBalanceScore,
                    houseScores,
                    planetScores)
            };

            // populate aggregated and per-category lists (dedup)
            result.Factors = overallFactors.Distinct().ToList();
            result.OverallFactors = overallFactors.Distinct().ToList();
            result.StabilityFactors = stabilityFactors.Distinct().ToList();
            result.GrowthFactors = growthFactors.Distinct().ToList();
            result.ChallengeFactors = challengeFactors.Distinct().ToList();

            // populate structured numeric breakdown
            result.ScoreBreakdown = new ScoreBreakdown
            {
                Overall = overallComponents.Select(c => new ScoreComponent
                {
                    Label = c.Label,
                    Score = c.Score,
                    Weight = c.Weight,
                    Contribution = Math.Round(c.Score * c.Weight, 2)
                }).ToList(),
                Stability = stabilityComponents.Select(c => new ScoreComponent
                {
                    Label = c.Label,
                    Score = c.Score,
                    Weight = c.Weight,
                    Contribution = Math.Round(c.Score * c.Weight, 2)
                }).ToList(),
                Growth = growthComponents.Select(c => new ScoreComponent
                {
                    Label = c.Label,
                    Score = c.Score,
                    Weight = c.Weight,
                    Contribution = Math.Round(c.Score * c.Weight, 2)
                }).ToList(),
                Challenge = new List<ScoreComponent>
                {
                    new ScoreComponent
                    {
                        Label = "Challenge pressure",
                        Score = challengePressure,
                        Weight = 1.0,
                        Contribution = challengePressure
                    },
                    new ScoreComponent
                    {
                        Label = "Weak planets count",
                        Score = planetScores.Count(ps => ps.Score <= 40),
                        Weight = 0,
                        Contribution = 0
                    },
                    new ScoreComponent
                    {
                        Label = "Weak houses count",
                        Score = houseScores.Count(hs => hs.Score <= 40),
                        Weight = 0,
                        Contribution = 0
                    },
                    new ScoreComponent
                    {
                        Label = "Moksha",
                        Score = mokshaScore,
                        Weight = 0,
                        Contribution = 0
                    }
                }
            };

            return result;
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

        private static int ScoreSun(
            IReadOnlyList<PlanetScoreResult> planetScores,
            List<string> factors)
        {
            int score = GetPlanetScore(planetScores, Planet.Sun);

            factors.Add(FormatFactor(
                score,
                $"Sun strength contributes to vitality, authority, confidence, and visible self-expression ({score}/100)"));

            return score;
        }

        private static int ScoreCoreIdentity(
            int lagnaScore,
            int moonScore,
            int sunScore,
            List<string> factors)
        {
            int score = WeightedScore(new[]
            {
                (lagnaScore, 0.45),
                (moonScore, 0.30),
                (sunScore, 0.25)
            });

            factors.Add(FormatFactor(
                score,
                $"Core identity combines Lagna ({lagnaScore}/100), Moon ({moonScore}/100), and Sun ({sunScore}/100)"));

            return score;
        }

        private static int ScoreHouseSet(
            string setName,
            HouseNumber[] houses,
            IReadOnlyList<HouseScoreResult> houseScores,
            List<string> factors)
        {
            var scores = houses.Select(house => GetHouseScore(houseScores, house)).ToList();
            int score = Clamp((int)Math.Round(scores.Average()));

            factors.Add(FormatFactor(
                score,
                $"{setName} power from houses ({string.Join(", ", houses.Select(h => (int)h))}) aggregates to {score}/100"));

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

        private static void ScoreCurrentDasha(
            Planet? activeMahadashaLord,
            List<string> factors)
        {
            string suffix = activeMahadashaLord.HasValue
                ? $"active Mahadasha lord {activeMahadashaLord.Value} belongs to dasha/transit prediction"
                : "no active Mahadasha lord was provided";

            factors.Add($"+0 Current dasha influence excluded from static Kundali score because {suffix}");
        }

        private static int ScoreChallengePressure(
            IReadOnlyList<PlanetScoreResult> planetScores,
            IReadOnlyList<HouseScoreResult> houseScores,
            int mokshaScore,
            List<string> factors)
        {
            int weakPlanetCount = planetScores.Count(score => score.Score <= 40);
            int weakHouseCount = houseScores.Count(score => score.Score <= 40);
            int pressure = 25 + weakPlanetCount * 6 + weakHouseCount * 5;

            if (mokshaScore < 45)
            {
                pressure += 8;
            }

            pressure = Clamp(pressure);

            factors.Add(FormatFactor(
                -pressure,
                $"Challenge pressure is {pressure}/100 from {weakPlanetCount} weak planets, {weakHouseCount} weak houses, and Moksha score {mokshaScore}/100"));

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
            int sunScore,
            int coreIdentityScore,
            int kendraPowerScore,
            int trikonaPowerScore,
            int dharmaScore,
            int arthaScore,
            int kamaScore,
            int mokshaScore,
            int planetaryBalanceScore,
            IReadOnlyList<HouseScoreResult> houseScores,
            IReadOnlyList<PlanetScoreResult> planetScores)
        {
            var strengths = new List<(string Label, int Score)>
            {
                ("Strong Lagna supports identity, vitality, and life direction", lagnaScore),
                ("Strong Moon supports emotional balance and adaptability", moonScore),
                ("Strong Sun supports vitality, confidence, authority, and visibility", sunScore),
                ("Strong core identity aligns Lagna, Moon, and Sun", coreIdentityScore),
                ("Strong Kendra houses support life structure and practical manifestation", kendraPowerScore),
                ("Strong Trikona houses support dharma, fortune, intelligence, and grace", trikonaPowerScore),
                ("Strong Dharma houses support purpose, wisdom, and alignment", dharmaScore),
                ("Strong Artha houses support execution, profession, and resources", arthaScore),
                ("Strong Kama houses support relationships, ambition, and social fulfillment", kamaScore),
                ("Strong Moksha houses support inner depth, release, and transformation", mokshaScore),
                ("Planetary balance supports overall manifestation capacity", planetaryBalanceScore)
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
            int sunScore,
            int coreIdentityScore,
            int kendraPowerScore,
            int trikonaPowerScore,
            int dharmaScore,
            int arthaScore,
            int kamaScore,
            int mokshaScore,
            int planetaryBalanceScore,
            IReadOnlyList<HouseScoreResult> houseScores,
            IReadOnlyList<PlanetScoreResult> planetScores)
        {
            var weaknesses = new List<(string Label, int Score)>
            {
                ("Weak Lagna may reduce resilience and clear life direction", lagnaScore),
                ("Weak Moon may reduce emotional steadiness and inner balance", moonScore),
                ("Weak Sun may reduce vitality, confidence, authority, and visibility", sunScore),
                ("Weak core identity may reduce alignment between body, mind, and will", coreIdentityScore),
                ("Weak Kendra houses may reduce stability, structure, and practical manifestation", kendraPowerScore),
                ("Weak Trikona houses may reduce dharma, fortune, judgment, and grace", trikonaPowerScore),
                ("Weak Dharma houses may reduce purpose, guidance, and alignment", dharmaScore),
                ("Weak Artha houses may reduce execution, profession, and material productivity", arthaScore),
                ("Weak Kama houses may reduce relationship flow, ambition, and social fulfillment", kamaScore),
                ("Weak Moksha houses may increase emotional pressure and inner turbulence", mokshaScore),
                ("Weak planetary balance may reduce overall chart delivery", planetaryBalanceScore)
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
