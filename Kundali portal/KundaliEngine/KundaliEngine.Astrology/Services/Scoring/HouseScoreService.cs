using KundaliEngine.Domain.Enums;
using KundaliEngine.Domain.Models;
using KundaliEngine.KnowledgeBase.Models;
using KundaliEngine.KnowledgeBase.Services;

namespace KundaliEngine.Astrology.Services.Scoring
{
    public class HouseScoreService
    {
        private const int BaseScore = 50;
        private const int MinimumScore = 0;
        private const int MaximumScore = 100;
        private const double HouseLordWeight = 0.50;
        private const double OccupantStrengthWeight = 0.20;
        private const double AspectStrengthWeight = 0.15;

        private static readonly IReadOnlyDictionary<string, int> ClassificationInfluence =
            new Dictionary<string, int>(StringComparer.OrdinalIgnoreCase)
            {
                ["NaturalBenefic"] = 4,
                ["NaturalMalefic"] = -4,
                ["ShadowPlanet"] = -3,
                ["Neutral"] = 0
            };

        private static readonly IReadOnlyDictionary<string, int> HouseCategoryInfluence =
            new Dictionary<string, int>(StringComparer.OrdinalIgnoreCase)
            {
                ["Kendra"] = 6,
                ["Trikona"] = 8,
                ["Upachaya"] = 4,
                ["Dusthana"] = -8,
                ["Maraka"] = -5
            };

        private readonly AstrologyKnowledgeService _knowledgeService;
        private readonly PlanetScoreService _planetScoreService;

        public HouseScoreService()
            : this(new AstrologyKnowledgeService())
        {
        }

        public HouseScoreService(AstrologyKnowledgeService knowledgeService)
            : this(knowledgeService, new PlanetScoreService(knowledgeService))
        {
        }

        public HouseScoreService(
            AstrologyKnowledgeService knowledgeService,
            PlanetScoreService planetScoreService)
        {
            _knowledgeService = knowledgeService;
            _planetScoreService = planetScoreService;
        }

        public HouseScoreResult CalculateHouseScore(
            HouseNumber houseNumber,
            ZodiacSign houseSign,
            IReadOnlyList<PlanetPosition> planetPositions,
            AspectCalculationResult? aspectResult = null)
        {
            if (planetPositions is null)
            {
                throw new ArgumentNullException(nameof(planetPositions));
            }

            HouseKnowledge house = GetHouseKnowledge(houseNumber);
            int score = BaseScore;
            var factors = new List<string>
            {
                $"+ Base house manifestation starts at {BaseScore}"
            };

            score += ScoreHouseLord(houseNumber, houseSign, planetPositions, factors);
            score += ScoreOccupants(houseNumber, planetPositions, factors);
            score += ScoreAspects(houseNumber, planetPositions, aspectResult, factors);
            score += ScoreHouseCategory(house, factors);

            return new HouseScoreResult
            {
                HouseNumber = houseNumber,
                HouseName = house.Name,
                Score = Math.Clamp(score, MinimumScore, MaximumScore),
                Factors = factors,
                DominantThemes = GetDominantThemes(house)
            };
        }

        public List<HouseScoreResult> CalculateAllHouseScores(
            ZodiacSign ascendantSign,
            IReadOnlyList<PlanetPosition> planetPositions,
            AspectCalculationResult? aspectResult = null)
        {
            if (planetPositions is null)
            {
                throw new ArgumentNullException(nameof(planetPositions));
            }

            return Enumerable.Range(1, 12)
                .Select(number =>
                {
                    var houseNumber = (HouseNumber)number;
                    ZodiacSign houseSign = GetHouseSign(ascendantSign, houseNumber);

                    return CalculateHouseScore(
                        houseNumber,
                        houseSign,
                        planetPositions,
                        aspectResult);
                })
                .ToList();
        }

        private int ScoreHouseLord(
            HouseNumber houseNumber,
            ZodiacSign houseSign,
            IReadOnlyList<PlanetPosition> planetPositions,
            List<string> factors)
        {
            PlanetKnowledge? lordKnowledge = GetSignLord(houseSign);

            if (lordKnowledge == null || !Enum.TryParse(lordKnowledge.Name, out Planet lord))
            {
                factors.Add($"+0 No house lord knowledge found for {houseSign}");
                return 0;
            }

            PlanetPosition? lordPosition = planetPositions.FirstOrDefault(position => position.Planet == lord);

            if (lordPosition == null)
            {
                factors.Add($"+0 {houseNumber} lord {lord} was not found in planet positions");
                return 0;
            }

            PlanetScoreResult lordScore = _planetScoreService.Calculate(lordPosition);
            int weightedScore = WeightedDelta(lordScore.Score, HouseLordWeight);
            factors.Add(FormatFactor(
                weightedScore,
                $"{houseNumber} lord {lord} rules {houseSign} and carries {lordScore.Score}/100 planetary strength"));

            return weightedScore;
        }

        private int ScoreOccupants(
            HouseNumber houseNumber,
            IReadOnlyList<PlanetPosition> planetPositions,
            List<string> factors)
        {
            var occupants = planetPositions
                .Where(position => position.House == houseNumber)
                .ToList();

            if (occupants.Count == 0)
            {
                factors.Add("+0 No planets occupy this house");
                return 0;
            }

            int total = 0;

            foreach (var occupant in occupants)
            {
                PlanetScoreResult planetScore = _planetScoreService.Calculate(occupant);
                PlanetKnowledge? planetKnowledge = _knowledgeService.GetPlanet(occupant.Planet.ToString());
                int strengthInfluence = WeightedDelta(planetScore.Score, OccupantStrengthWeight);
                int semanticInfluence = GetClassificationInfluence(planetKnowledge);
                int influence = strengthInfluence + semanticInfluence;

                factors.Add(FormatFactor(
                    influence,
                    $"{occupant.Planet} occupies the house with {planetScore.Score}/100 strength and {GetClassificationLabel(planetKnowledge)} nature"));

                total += influence;
            }

            return total;
        }

        private int ScoreAspects(
            HouseNumber houseNumber,
            IReadOnlyList<PlanetPosition> planetPositions,
            AspectCalculationResult? aspectResult,
            List<string> factors)
        {
            var aspects = aspectResult?.HouseAspects
                .Where(aspect => aspect.ToHouse == (int)houseNumber)
                .ToList() ?? new List<HouseAspect>();

            if (aspects.Count == 0)
            {
                factors.Add("+0 No direct graha drishti aspects on this house");
                return 0;
            }

            int total = 0;

            foreach (var aspect in aspects)
            {
                PlanetPosition? aspectingPosition = planetPositions
                    .FirstOrDefault(position => position.Planet == aspect.FromPlanet);

                if (aspectingPosition == null)
                {
                    factors.Add($"+0 {aspect.FromPlanet} aspects the house, but its planet position was not found");
                    continue;
                }

                PlanetScoreResult planetScore = _planetScoreService.Calculate(aspectingPosition);
                PlanetKnowledge? planetKnowledge = _knowledgeService.GetPlanet(aspect.FromPlanet.ToString());
                int strengthInfluence = WeightedDelta(planetScore.Score, AspectStrengthWeight);
                int semanticInfluence = (int)Math.Round(GetClassificationInfluence(planetKnowledge) / 2.0);
                int influence = strengthInfluence + semanticInfluence;

                factors.Add(FormatFactor(
                    influence,
                    $"{aspect.FromPlanet} aspect from house {aspect.FromHouse} influences this house with {planetScore.Score}/100 strength and {GetClassificationLabel(planetKnowledge)} nature"));

                total += influence;
            }

            return total;
        }

        private int ScoreHouseCategory(HouseKnowledge house, List<string> factors)
        {
            if (string.IsNullOrWhiteSpace(house.BhavaType))
            {
                factors.Add("+0 No semantic house category available");
                return 0;
            }

            int total = 0;

            foreach (var influence in HouseCategoryInfluence)
            {
                if (!house.BhavaType.Contains(influence.Key, StringComparison.OrdinalIgnoreCase))
                {
                    continue;
                }

                total += influence.Value;
                factors.Add(FormatFactor(
                    influence.Value,
                    $"{house.Name} carries {influence.Key} house category influence"));
            }

            if (total == 0)
            {
                factors.Add($"+0 {house.Name} has neutral house category influence ({house.BhavaType})");
            }

            return total;
        }

        private HouseKnowledge GetHouseKnowledge(HouseNumber houseNumber)
        {
            return _knowledgeService.GetHouse((int)houseNumber) ?? new HouseKnowledge
            {
                Number = (int)houseNumber,
                Name = $"{houseNumber} House",
                BhavaType = string.Empty
            };
        }

        private PlanetKnowledge? GetSignLord(ZodiacSign sign)
        {
            string signName = sign.ToString();

            return _knowledgeService.GetPlanets()
                .FirstOrDefault(planet => ContainsText(planet.OwnSigns, signName));
        }

        private int GetClassificationInfluence(PlanetKnowledge? planetKnowledge)
        {
            if (planetKnowledge == null)
            {
                return 0;
            }

            return ClassificationInfluence.TryGetValue(planetKnowledge.Classification, out int influence)
                ? influence
                : 0;
        }

        private static List<string> GetDominantThemes(HouseKnowledge house)
        {
            return house.LifeAreas.Count > 0
                ? house.LifeAreas.Take(4).ToList()
                : house.Karakatva.Take(4).ToList();
        }

        private static ZodiacSign GetHouseSign(ZodiacSign ascendantSign, HouseNumber houseNumber)
        {
            int signIndex = ((int)ascendantSign - 1 + ((int)houseNumber - 1)) % 12;
            return (ZodiacSign)(signIndex + 1);
        }

        private static int WeightedDelta(int score, double weight)
        {
            return (int)Math.Round((score - BaseScore) * weight);
        }

        private static bool ContainsText(IEnumerable<string> values, string expected)
        {
            return values.Any(value => value.Equals(expected, StringComparison.OrdinalIgnoreCase));
        }

        private static string GetClassificationLabel(PlanetKnowledge? planetKnowledge)
        {
            return string.IsNullOrWhiteSpace(planetKnowledge?.Classification)
                ? "unknown"
                : planetKnowledge.Classification;
        }

        private static string FormatFactor(int score, string reason)
        {
            string prefix = score > 0 ? $"+{score}" : score.ToString();
            return $"{prefix} {reason}";
        }
    }
}
