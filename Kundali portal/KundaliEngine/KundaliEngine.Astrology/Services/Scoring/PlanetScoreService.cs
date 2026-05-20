using KundaliEngine.Domain.Enums;
using KundaliEngine.Domain.Models;
using KundaliEngine.KnowledgeBase.Models;
using KundaliEngine.KnowledgeBase.Services;

namespace KundaliEngine.Astrology.Services.Scoring
{
    public class PlanetScoreService
    {
        private const int BaseScore = 50;
        private const int MinimumScore = 0;
        private const int MaximumScore = 100;

        private static readonly IReadOnlyDictionary<string, int> DignityWeights =
            new Dictionary<string, int>(StringComparer.OrdinalIgnoreCase)
            {
                ["Exalted"] = 25,
                ["Own Sign"] = 18,
                ["Moolatrikona"] = 20,
                ["Friendly Sign"] = 10,
                ["Neutral Sign"] = 0,
                ["Enemy Sign"] = -10,
                ["Debilitated"] = -25
            };

        private static readonly string[] SupportiveHouseTypes = { "Kendra", "Trikona" };
        private static readonly string[] ChallengingHouseTypes = { "Dusthana" };

        private readonly AstrologyKnowledgeService _knowledgeService;

        public PlanetScoreService()
            : this(new AstrologyKnowledgeService())
        {
        }

        public PlanetScoreService(AstrologyKnowledgeService knowledgeService)
        {
            _knowledgeService = knowledgeService;
        }

        public PlanetScoreResult Calculate(PlanetPosition position)
        {
            if (position is null)
            {
                throw new ArgumentNullException(nameof(position));
            }

            int score = BaseScore;
            var factors = new List<string>
            {
                $"+ Base planetary capacity starts at {BaseScore}"
            };

            score += ScoreDignity(position, factors);
            score += ScoreCombustion(position, factors);
            score += ScoreHousePlacement(position, factors);

            return new PlanetScoreResult
            {
                Planet = position.Planet,
                Score = Math.Clamp(score, MinimumScore, MaximumScore),
                Factors = factors
            };
        }

        public IReadOnlyList<PlanetScoreResult> CalculateMany(IEnumerable<PlanetPosition> positions)
        {
            if (positions is null)
            {
                throw new ArgumentNullException(nameof(positions));
            }

            return positions.Select(Calculate).ToList();
        }

        private int ScoreDignity(PlanetPosition position, List<string> factors)
        {
            PlanetKnowledge? planet = _knowledgeService.GetPlanet(position.Planet.ToString());

            if (planet == null)
            {
                factors.Add($"+0 No dignity knowledge found for {position.Planet}");
                return 0;
            }

            string dignity = GetDignity(planet, position.Sign);
            int score = DignityWeights[dignity];
            factors.Add(FormatFactor(score, $"{position.Planet} is {dignity.ToLowerInvariant()} in {position.Sign}"));

            return score;
        }

        private int ScoreCombustion(PlanetPosition position, List<string> factors)
        {
            if (!position.IsCombust)
            {
                factors.Add("+0 Not combust");
                return 0;
            }

            factors.Add("-15 Combustion reduces independent planetary expression");
            return -15;
        }

        private int ScoreHousePlacement(PlanetPosition position, List<string> factors)
        {
            HouseKnowledge? house = _knowledgeService.GetHouse((int)position.House);

            if (house == null)
            {
                factors.Add($"+0 No house knowledge found for {position.House}");
                return 0;
            }

            if (HasAnyToken(house.BhavaType, SupportiveHouseTypes))
            {
                factors.Add($"+10 Strong house placement in {house.Name} ({house.BhavaType})");
                return 10;
            }

            if (HasAnyToken(house.BhavaType, ChallengingHouseTypes))
            {
                factors.Add($"-10 Challenging house placement in {house.Name} ({house.BhavaType})");
                return -10;
            }

            factors.Add($"+0 Neutral house placement in {house.Name} ({house.BhavaType})");
            return 0;
        }

        private string GetDignity(PlanetKnowledge planet, ZodiacSign sign)
        {
            string signName = sign.ToString();

            if (EqualsText(planet.Exaltation, signName))
            {
                return "Exalted";
            }

            if (EqualsText(planet.Debilitation, signName))
            {
                return "Debilitated";
            }

            if (EqualsText(planet.Moolatrikona, signName))
            {
                return "Moolatrikona";
            }

            if (ContainsText(planet.OwnSigns, signName))
            {
                return "Own Sign";
            }

            PlanetKnowledge? signLord = GetSignLord(signName);

            if (signLord == null)
            {
                return "Neutral Sign";
            }

            if (ContainsText(planet.Friends, signLord.Name))
            {
                return "Friendly Sign";
            }

            if (ContainsText(planet.Enemies, signLord.Name))
            {
                return "Enemy Sign";
            }

            return "Neutral Sign";
        }

        private PlanetKnowledge? GetSignLord(string signName)
        {
            return _knowledgeService.GetPlanets()
                .FirstOrDefault(planet => ContainsText(planet.OwnSigns, signName));
        }

        private static bool HasAnyToken(string value, IEnumerable<string> tokens)
        {
            return tokens.Any(token =>
                value.Contains(token, StringComparison.OrdinalIgnoreCase));
        }

        private static bool ContainsText(IEnumerable<string> values, string expected)
        {
            return values.Any(value => EqualsText(value, expected));
        }

        private static bool EqualsText(string value, string expected)
        {
            return value.Equals(expected, StringComparison.OrdinalIgnoreCase);
        }

        private static string FormatFactor(int score, string reason)
        {
            string prefix = score > 0 ? $"+{score}" : score.ToString();
            return $"{prefix} {reason}";
        }
    }
}
