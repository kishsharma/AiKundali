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
        private const double KarakaStrengthWeight = 0.15;

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

        private static readonly IReadOnlyDictionary<HouseNumber, Planet[]> HouseKarakas =
            new Dictionary<HouseNumber, Planet[]>
            {
                [HouseNumber.First] = new[] { Planet.Sun },
                [HouseNumber.Second] = new[] { Planet.Jupiter, Planet.Mercury },
                [HouseNumber.Third] = new[] { Planet.Mars },
                [HouseNumber.Fourth] = new[] { Planet.Moon, Planet.Venus },
                [HouseNumber.Fifth] = new[] { Planet.Jupiter },
                [HouseNumber.Sixth] = new[] { Planet.Mars, Planet.Saturn },
                [HouseNumber.Seventh] = new[] { Planet.Venus, Planet.Jupiter },
                [HouseNumber.Eighth] = new[] { Planet.Saturn, Planet.Ketu },
                [HouseNumber.Ninth] = new[] { Planet.Jupiter, Planet.Sun },
                [HouseNumber.Tenth] = new[] { Planet.Sun, Planet.Saturn, Planet.Mercury },
                [HouseNumber.Eleventh] = new[] { Planet.Jupiter },
                [HouseNumber.Twelfth] = new[] { Planet.Saturn, Planet.Ketu }
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
            AspectCalculationResult? aspectResult = null,
            ZodiacSign? ascendantSign = null)
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
            score += ScoreHouseLordPlacement(houseNumber, houseSign, planetPositions, factors);
            score += ScoreOccupants(houseNumber, planetPositions, ascendantSign, factors);
            score += ScoreAspects(houseNumber, planetPositions, aspectResult, ascendantSign, factors);
            score += ScoreKarakaSupport(houseNumber, planetPositions, factors);
            score += ScoreAfflictionProtection(houseNumber, houseSign, planetPositions, aspectResult, factors);
            score += ScoreFunctionalLordship(houseNumber, ascendantSign, factors);
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
                        aspectResult,
                        ascendantSign);
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

        private int ScoreHouseLordPlacement(
            HouseNumber houseNumber,
            ZodiacSign houseSign,
            IReadOnlyList<PlanetPosition> planetPositions,
            List<string> factors)
        {
            PlanetKnowledge? lordKnowledge = GetSignLord(houseSign);

            if (lordKnowledge == null || !Enum.TryParse(lordKnowledge.Name, out Planet lord))
            {
                factors.Add($"+0 House lord placement unavailable because no lord was found for {houseSign}");
                return 0;
            }

            PlanetPosition? lordPosition = planetPositions.FirstOrDefault(position => position.Planet == lord);

            if (lordPosition == null)
            {
                factors.Add($"+0 House lord placement unavailable because {lord} position was not found");
                return 0;
            }

            int relativeHouse = GetRelativeHouse(houseNumber, lordPosition.House);
            int score = relativeHouse switch
            {
                1 => 8,
                5 or 9 => 6,
                4 or 7 or 10 => 4,
                3 or 11 => 3,
                2 => -3,
                6 or 8 or 12 => -8,
                _ => 0
            };

            if (lordPosition.IsCombust)
            {
                score -= 4;
            }

            if (lordPosition.IsRetrograde)
            {
                score -= 2;
            }

            factors.Add(FormatFactor(
                score,
                $"{houseNumber} lord {lord} is placed {relativeHouse} house(s) from its own house"
                + GetConditionSuffix(lordPosition)));

            return score;
        }

        private int ScoreOccupants(
            HouseNumber houseNumber,
            IReadOnlyList<PlanetPosition> planetPositions,
            ZodiacSign? ascendantSign,
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
                int functionalInfluence = GetFunctionalPlanetInfluence(occupant.Planet, ascendantSign);
                int influence = strengthInfluence + semanticInfluence + functionalInfluence;

                factors.Add(FormatFactor(
                    influence,
                    $"{occupant.Planet} occupies the house with {planetScore.Score}/100 strength, {GetClassificationLabel(planetKnowledge)} nature, and {FormatFunctionalRole(functionalInfluence)}"));

                total += influence;
            }

            return total;
        }

        private int ScoreAspects(
            HouseNumber houseNumber,
            IReadOnlyList<PlanetPosition> planetPositions,
            AspectCalculationResult? aspectResult,
            ZodiacSign? ascendantSign,
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
                double aspectWeight = AspectStrengthWeight * GetAspectPotency(aspect);
                int strengthInfluence = WeightedDelta(planetScore.Score, aspectWeight);
                int semanticInfluence = (int)Math.Round(GetClassificationInfluence(planetKnowledge) / 2.0);
                int functionalInfluence = (int)Math.Round(GetFunctionalPlanetInfluence(aspect.FromPlanet, ascendantSign) / 2.0);
                int influence = strengthInfluence + semanticInfluence + functionalInfluence;

                factors.Add(FormatFactor(
                    influence,
                    $"{aspect.FromPlanet} aspect from house {aspect.FromHouse} using {aspect.AspectOffset}th-house drishti influences this house with {planetScore.Score}/100 strength, {GetClassificationLabel(planetKnowledge)} nature, and {FormatFunctionalRole(functionalInfluence)}"));

                total += influence;
            }

            return total;
        }

        private int ScoreKarakaSupport(
            HouseNumber houseNumber,
            IReadOnlyList<PlanetPosition> planetPositions,
            List<string> factors)
        {
            if (!HouseKarakas.TryGetValue(houseNumber, out Planet[]? karakas) || karakas.Length == 0)
            {
                factors.Add("+0 No karaka mapping available for this house");
                return 0;
            }

            var karakaScores = karakas
                .Select(karaka => new
                {
                    Karaka = karaka,
                    Position = planetPositions.FirstOrDefault(position => position.Planet == karaka)
                })
                .Where(item => item.Position != null)
                .Select(item => new
                {
                    item.Karaka,
                    Score = _planetScoreService.Calculate(item.Position!).Score
                })
                .ToList();

            if (karakaScores.Count == 0)
            {
                factors.Add($"+0 Natural karakas for this house were not found ({string.Join(", ", karakas)})");
                return 0;
            }

            int averageScore = (int)Math.Round(karakaScores.Average(item => item.Score));
            int influence = WeightedDelta(averageScore, KarakaStrengthWeight);

            factors.Add(FormatFactor(
                influence,
                $"Natural karaka support from {string.Join(", ", karakaScores.Select(item => $"{item.Karaka} {item.Score}/100"))} averages {averageScore}/100"));

            return influence;
        }

        private int ScoreAfflictionProtection(
            HouseNumber houseNumber,
            ZodiacSign houseSign,
            IReadOnlyList<PlanetPosition> planetPositions,
            AspectCalculationResult? aspectResult,
            List<string> factors)
        {
            int score = 0;

            var occupants = planetPositions
                .Where(position => position.House == houseNumber)
                .ToList();
            var aspects = aspectResult?.HouseAspects
                .Where(aspect => aspect.ToHouse == (int)houseNumber)
                .ToList() ?? new List<HouseAspect>();

            int protectiveCount = occupants.Count(IsNaturalBenefic)
                + aspects.Count(aspect => IsNaturalBenefic(aspect.FromPlanet));
            int afflictionCount = occupants.Count(IsNaturalMaleficOrShadow)
                + aspects.Count(aspect => IsNaturalMaleficOrShadow(aspect.FromPlanet));

            score += Math.Min(protectiveCount * 2, 8);
            score -= Math.Min(afflictionCount * 2, 10);

            if (aspects.Any(aspect => aspect.FromPlanet == Planet.Jupiter))
            {
                score += 3;
            }

            if (occupants.Any(position => position.Planet is Planet.Rahu or Planet.Ketu))
            {
                score -= 3;
            }

            PlanetPosition? lordPosition = GetHouseLordPosition(houseSign, planetPositions);
            if (lordPosition != null)
            {
                bool lordWithNodes = planetPositions.Any(position =>
                    position.House == lordPosition.House
                    && position.Planet != lordPosition.Planet
                    && position.Planet is Planet.Rahu or Planet.Ketu);

                if (lordWithNodes)
                {
                    score -= 4;
                }
            }

            int previousHouse = GetWrappedHouse((int)houseNumber - 1);
            int nextHouse = GetWrappedHouse((int)houseNumber + 1);
            bool previousMalefic = planetPositions.Any(position => (int)position.House == previousHouse && IsNaturalMaleficOrShadow(position));
            bool nextMalefic = planetPositions.Any(position => (int)position.House == nextHouse && IsNaturalMaleficOrShadow(position));
            bool previousBenefic = planetPositions.Any(position => (int)position.House == previousHouse && IsNaturalBenefic(position));
            bool nextBenefic = planetPositions.Any(position => (int)position.House == nextHouse && IsNaturalBenefic(position));

            if (previousMalefic && nextMalefic)
            {
                score -= 6;
            }
            else if (previousBenefic && nextBenefic)
            {
                score += 4;
            }

            score = Math.Clamp(score, -12, 12);
            factors.Add(FormatFactor(
                score,
                $"Affliction/protection balance from {protectiveCount} benefic influences, {afflictionCount} malefic or shadow influences, Jupiter protection, node contact, and adjacent-house enclosure"));

            return score;
        }

        private int ScoreFunctionalLordship(
            HouseNumber houseNumber,
            ZodiacSign? ascendantSign,
            List<string> factors)
        {
            if (ascendantSign == null)
            {
                factors.Add("+0 Functional lordship unavailable without ascendant sign");
                return 0;
            }

            ZodiacSign houseSign = GetHouseSign(ascendantSign.Value, houseNumber);
            PlanetKnowledge? lordKnowledge = GetSignLord(houseSign);

            if (lordKnowledge == null || !Enum.TryParse(lordKnowledge.Name, out Planet lord))
            {
                factors.Add($"+0 Functional lordship unavailable because no lord was found for {houseSign}");
                return 0;
            }

            int influence = GetFunctionalPlanetInfluence(lord, ascendantSign);
            factors.Add(FormatFactor(
                influence,
                $"{houseNumber} lord {lord} has {FormatFunctionalRole(influence)} for {ascendantSign} ascendant"));

            return influence;
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

        private PlanetPosition? GetHouseLordPosition(
            ZodiacSign houseSign,
            IReadOnlyList<PlanetPosition> planetPositions)
        {
            PlanetKnowledge? lordKnowledge = GetSignLord(houseSign);

            return lordKnowledge != null && Enum.TryParse(lordKnowledge.Name, out Planet lord)
                ? planetPositions.FirstOrDefault(position => position.Planet == lord)
                : null;
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

        private int GetFunctionalPlanetInfluence(Planet planet, ZodiacSign? ascendantSign)
        {
            if (ascendantSign == null || planet is Planet.Rahu or Planet.Ketu)
            {
                return 0;
            }

            var ruledHouses = GetRuledHouses(planet, ascendantSign.Value).ToList();
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

        private IEnumerable<HouseNumber> GetRuledHouses(Planet planet, ZodiacSign ascendantSign)
        {
            PlanetKnowledge? planetKnowledge = _knowledgeService.GetPlanet(planet.ToString());
            if (planetKnowledge == null)
            {
                yield break;
            }

            foreach (string ownSign in planetKnowledge.OwnSigns)
            {
                if (!Enum.TryParse(ownSign, out ZodiacSign sign))
                {
                    continue;
                }

                int houseNumber = ((int)sign - (int)ascendantSign + 12) % 12 + 1;
                yield return (HouseNumber)houseNumber;
            }
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

        private static int GetRelativeHouse(HouseNumber fromHouse, HouseNumber toHouse)
        {
            return ((int)toHouse - (int)fromHouse + 12) % 12 + 1;
        }

        private static int GetWrappedHouse(int house)
        {
            return ((house - 1 + 12) % 12) + 1;
        }

        private static bool IsKendra(HouseNumber houseNumber)
        {
            return houseNumber is HouseNumber.First or HouseNumber.Fourth or HouseNumber.Seventh or HouseNumber.Tenth;
        }

        private static bool IsTrikona(HouseNumber houseNumber)
        {
            return houseNumber is HouseNumber.First or HouseNumber.Fifth or HouseNumber.Ninth;
        }

        private static double GetAspectPotency(HouseAspect aspect)
        {
            bool specialAspect = aspect.FromPlanet switch
            {
                Planet.Mars => aspect.AspectOffset is 4 or 8,
                Planet.Jupiter => aspect.AspectOffset is 5 or 9,
                Planet.Saturn => aspect.AspectOffset is 3 or 10,
                _ => false
            };

            return specialAspect ? 1.15 : 1.0;
        }

        private bool IsNaturalBenefic(PlanetPosition position)
        {
            return IsNaturalBenefic(position.Planet);
        }

        private bool IsNaturalBenefic(Planet planet)
        {
            PlanetKnowledge? planetKnowledge = _knowledgeService.GetPlanet(planet.ToString());
            return planetKnowledge?.Classification.Equals("NaturalBenefic", StringComparison.OrdinalIgnoreCase) == true;
        }

        private bool IsNaturalMaleficOrShadow(PlanetPosition position)
        {
            return IsNaturalMaleficOrShadow(position.Planet);
        }

        private bool IsNaturalMaleficOrShadow(Planet planet)
        {
            PlanetKnowledge? planetKnowledge = _knowledgeService.GetPlanet(planet.ToString());
            return planetKnowledge?.Classification.Equals("NaturalMalefic", StringComparison.OrdinalIgnoreCase) == true
                || planetKnowledge?.Classification.Equals("ShadowPlanet", StringComparison.OrdinalIgnoreCase) == true;
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

        private static string FormatFunctionalRole(int influence)
        {
            return influence switch
            {
                > 0 => $"functional support (+{influence})",
                < 0 => $"functional challenge ({influence})",
                _ => "neutral functional role"
            };
        }

        private static string GetConditionSuffix(PlanetPosition position)
        {
            var conditions = new List<string>();

            if (position.IsCombust)
            {
                conditions.Add("combust");
            }

            if (position.IsRetrograde)
            {
                conditions.Add("retrograde");
            }

            return conditions.Count == 0
                ? string.Empty
                : $" while {string.Join(" and ", conditions)}";
        }

        private static string FormatFactor(int score, string reason)
        {
            string prefix = score > 0 ? $"+{score}" : score.ToString();
            return $"{prefix} {reason}";
        }
    }
}
