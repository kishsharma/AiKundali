using HoraLife.PredictionEngine.Enums;
using HoraLife.PredictionEngine.Models;

namespace HoraLife.PredictionEngine.Services.Education;

/// <summary>
/// Education direction claims: higher study versus practical skill, and suitable subjects.
/// Claims use Neutral tone so they do not change the education support or pressure outlook.
/// </summary>
internal sealed class EducationDirectionClaimGenerator
{
    private const int DecisionMargin = 2;
    private const int MinimumScore = 2;
    private const int MaxFields = 3;

    public IReadOnlyList<PredictionClaim> Generate(PredictionRequest request, PredictionContext context)
    {
        ArgumentNullException.ThrowIfNull(request);
        ArgumentNullException.ThrowIfNull(context);

        if (request.LifeArea != PredictionLifeArea.Education)
        {
            throw new ArgumentException("This service supports only Education predictions.", nameof(request));
        }

        var houses = context.Houses
            .GroupBy(house => house.HouseNumber)
            .ToDictionary(group => group.Key, group => group.First());
        var planets = context.Planets
            .Where(planet => !string.IsNullOrWhiteSpace(planet.Planet))
            .GroupBy(planet => planet.Planet, StringComparer.OrdinalIgnoreCase)
            .ToDictionary(group => group.Key, group => group.First(), StringComparer.OrdinalIgnoreCase);

        if (!houses.ContainsKey(4) && !houses.ContainsKey(5))
        {
            return Array.Empty<PredictionClaim>();
        }

        var higher = 0;
        var skill = 0;
        var ruleIds = new List<string>();
        var evidenceKeys = new List<string>();

        void AddHigher(int points, string ruleId, string evidenceKey)
        {
            higher += points;
            ruleIds.Add(ruleId);
            evidenceKeys.Add(evidenceKey);
        }

        void AddSkill(int points, string ruleId, string evidenceKey)
        {
            skill += points;
            ruleIds.Add(ruleId);
            evidenceKeys.Add(evidenceKey);
        }

        ScoreLord(houses, planets, 5, house => house == 9 ? 3 : house is 4 or 5 ? 2 : 0, AddHigher, "education.path.fifth_lord_higher");
        ScoreLord(houses, planets, 9, house => house is 4 or 5 ? 3 : house == 9 ? 2 : 0, AddHigher, "education.path.ninth_lord_higher");
        ScoreLord(houses, planets, 4, house => house is 3 or 6 or 10 ? 3 : house == 4 ? 2 : 0, AddSkill, "education.path.fourth_lord_skill");
        ScoreLord(houses, planets, 3, house => house is 4 or 5 ? 2 : 0, AddSkill, "education.path.third_lord_skill");

        foreach (var planet in planets.Values)
        {
            var key = $"planet:{planet.Planet}:house:{planet.HouseNumber}";
            if (planet.Planet.Equals("Jupiter", StringComparison.OrdinalIgnoreCase) && planet.HouseNumber is 4 or 5 or 9)
            {
                AddHigher(2, "education.path.jupiter_higher", key);
            }

            if (planet.Planet.Equals("Mercury", StringComparison.OrdinalIgnoreCase) && planet.HouseNumber is 3 or 4 or 5 or 6 or 10)
            {
                AddSkill(2, "education.path.mercury_skill", key);
            }

            if (planet.Planet.Equals("Mars", StringComparison.OrdinalIgnoreCase) && planet.HouseNumber is 3 or 4 or 5)
            {
                AddSkill(1, "education.path.mars_skill", key);
            }
        }

        var claims = new List<PredictionClaim>();
        if (Math.Max(higher, skill) >= MinimumScore)
        {
            var pathKey = higher >= skill + DecisionMargin
                ? "education_path_higher"
                : skill >= higher + DecisionMargin
                    ? "education_path_skill"
                    : "education_path_both";

            claims.Add(new PredictionClaim
            {
                Key = pathKey,
                LifeArea = PredictionLifeArea.Education,
                FocusKey = request.FocusKey,
                Tone = PredictionTone.Neutral,
                Stage = PredictionClaimStage.Natal,
                RuleIds = ruleIds.Distinct(StringComparer.OrdinalIgnoreCase).ToArray(),
                EvidenceKeys =
                [
                    $"score:higher:{higher}",
                    $"score:skill:{skill}",
                    .. evidenceKeys.Distinct(StringComparer.OrdinalIgnoreCase)
                ]
            });
        }

        foreach (var planet in RankSubjects(houses, planets))
        {
            claims.Add(new PredictionClaim
            {
                Key = "education_field_" + planet.Planet.ToLowerInvariant(),
                LifeArea = PredictionLifeArea.Education,
                FocusKey = request.FocusKey,
                Tone = PredictionTone.Neutral,
                Stage = PredictionClaimStage.Natal,
                RuleIds = ["education.field.karaka"],
                EvidenceKeys =
                [
                    $"planet:{planet.Planet}",
                    $"planet:{planet.Planet}:house:{planet.HouseNumber}"
                ]
            });
        }

        return claims;
    }

    private static void ScoreLord(
        IReadOnlyDictionary<int, HousePredictionData> houses,
        IReadOnlyDictionary<string, PlanetPredictionData> planets,
        int sourceHouse,
        Func<int, int> pointsForTargetHouse,
        Action<int, string, string> add,
        string ruleId)
    {
        if (!houses.TryGetValue(sourceHouse, out var house) ||
            !planets.TryGetValue(house.HouseLord, out var lord))
        {
            return;
        }

        var points = pointsForTargetHouse(lord.HouseNumber);
        if (points <= 0)
        {
            return;
        }

        add(points, ruleId, $"house-lord:{sourceHouse}:{lord.Planet}:house:{lord.HouseNumber}");
    }

    private static IReadOnlyList<PlanetPredictionData> RankSubjects(
        IReadOnlyDictionary<int, HousePredictionData> houses,
        IReadOnlyDictionary<string, PlanetPredictionData> planets)
    {
        var weights = new Dictionary<string, int>(StringComparer.OrdinalIgnoreCase);

        void Consider(PlanetPredictionData planet, int weight)
        {
            if (weight <= 0 || string.IsNullOrWhiteSpace(planet.Planet))
            {
                return;
            }

            var total = weight + Math.Min(Math.Max(planet.StrengthScore ?? 0, 0), 30);
            if (!weights.TryGetValue(planet.Planet, out var current) || total > current)
            {
                weights[planet.Planet] = total;
            }
        }

        foreach (var planet in planets.Values.Where(planet => planet.HouseNumber is 4 or 5 or 9))
        {
            Consider(planet, 50);
        }

        foreach (var houseNumber in new[] { 4, 5, 9 })
        {
            if (houses.TryGetValue(houseNumber, out var house) &&
                planets.TryGetValue(house.HouseLord, out var lord))
            {
                Consider(lord, 40);
            }
        }

        return weights
            .OrderByDescending(pair => pair.Value)
            .ThenBy(pair => pair.Key, StringComparer.OrdinalIgnoreCase)
            .Take(MaxFields)
            .Select(pair => planets[pair.Key])
            .ToArray();
    }
}
