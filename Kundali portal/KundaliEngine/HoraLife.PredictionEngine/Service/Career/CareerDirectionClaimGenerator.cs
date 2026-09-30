using HoraLife.PredictionEngine.Enums;
using HoraLife.PredictionEngine.Models;

namespace HoraLife.PredictionEngine.Services.Career;

/// <summary>
/// Career direction claims: salaried service versus business, and suitable fields.
/// </summary>
/// <remarks>
/// HoraLife chart rules, not a scripture quotation.
/// Service leans on the 6th house, the 10th lord in the 6th, and Sun, Saturn, or Mars.
/// Business leans on the 7th house, the 3rd lord joining work or trade, and Mercury, Venus, or Jupiter.
/// Fields come from planets in the 10th, then the 10th lord, then planets in the 6th or 7th.
/// Claims use Neutral tone so they do not change the career support or pressure outlook.
/// </remarks>
internal sealed class CareerDirectionClaimGenerator
{
    private const int DecisionMargin = 2;
    private const int MinimumScore = 2;
    private const int MaxFields = 3;

    private static readonly HashSet<string> ServicePlanets = new(StringComparer.OrdinalIgnoreCase)
    {
        "Sun", "Saturn", "Mars"
    };

    private static readonly HashSet<string> TradePlanets = new(StringComparer.OrdinalIgnoreCase)
    {
        "Mercury", "Venus", "Jupiter"
    };

    public IReadOnlyList<PredictionClaim> Generate(PredictionRequest request, PredictionContext context)
    {
        ArgumentNullException.ThrowIfNull(request);
        ArgumentNullException.ThrowIfNull(context);

        if (request.LifeArea != PredictionLifeArea.Career)
        {
            throw new ArgumentException("This service supports only Career predictions.", nameof(request));
        }

        var houses = context.Houses
            .GroupBy(house => house.HouseNumber)
            .ToDictionary(group => group.Key, group => group.First());

        if (!houses.ContainsKey(10))
        {
            return Array.Empty<PredictionClaim>();
        }

        var planets = context.Planets
            .Where(planet => !string.IsNullOrWhiteSpace(planet.Planet))
            .GroupBy(planet => planet.Planet, StringComparer.OrdinalIgnoreCase)
            .ToDictionary(group => group.Key, group => group.First(), StringComparer.OrdinalIgnoreCase);

        var jobScore = 0;
        var businessScore = 0;
        var ruleIds = new List<string>();
        var evidenceKeys = new List<string>();

        void AddJob(int points, string ruleId, string evidenceKey)
        {
            jobScore += points;
            ruleIds.Add(ruleId);
            evidenceKeys.Add(evidenceKey);
        }

        void AddBusiness(int points, string ruleId, string evidenceKey)
        {
            businessScore += points;
            ruleIds.Add(ruleId);
            evidenceKeys.Add(evidenceKey);
        }

        if (houses.TryGetValue(10, out var tenthHouse) &&
            planets.TryGetValue(tenthHouse.HouseLord, out var tenthLord))
        {
            var lordKey = $"house-lord:10:{tenthLord.Planet}:house:{tenthLord.HouseNumber}";
            switch (tenthLord.HouseNumber)
            {
                case 6:
                    AddJob(3, "career.path.tenth_lord_in_6", lordKey);
                    break;
                case 7:
                    AddBusiness(3, "career.path.tenth_lord_in_7", lordKey);
                    break;
                case 3:
                    AddBusiness(2, "career.path.tenth_lord_in_3", lordKey);
                    break;
                case 10:
                    AddJob(1, "career.path.tenth_lord_in_10", lordKey);
                    AddBusiness(1, "career.path.tenth_lord_in_10", lordKey);
                    break;
                case 11:
                    AddJob(1, "career.path.tenth_lord_in_11", lordKey);
                    AddBusiness(1, "career.path.tenth_lord_in_11", lordKey);
                    break;
            }

            if (ServicePlanets.Contains(tenthLord.Planet))
            {
                AddJob(1, "career.path.tenth_lord_service", $"planet:{tenthLord.Planet}:as_10th_lord");
            }

            if (TradePlanets.Contains(tenthLord.Planet))
            {
                AddBusiness(1, "career.path.tenth_lord_trade", $"planet:{tenthLord.Planet}:as_10th_lord");
            }
        }

        ScoreHouseLord(houses, planets, 6, static house => house is 10 ? 3 : house is 1 or 6 ? 2 : 0, AddJob, "career.path.sixth_lord_in_service");
        ScoreHouseLord(houses, planets, 7, static house => house is 10 ? 3 : house is 2 or 7 or 11 ? 2 : 0, AddBusiness, "career.path.seventh_lord_in_trade");
        ScoreHouseLord(houses, planets, 3, static house => house is 7 or 10 ? 2 : 0, AddBusiness, "career.path.third_lord_enterprise");

        foreach (var planet in planets.Values)
        {
            var planetKey = $"planet:{planet.Planet}:house:{planet.HouseNumber}";
            if (planet.HouseNumber == 10 && ServicePlanets.Contains(planet.Planet))
            {
                AddJob(2, "career.path.service_planet_in_10", planetKey);
            }

            if (planet.HouseNumber == 10 && TradePlanets.Contains(planet.Planet))
            {
                AddBusiness(2, "career.path.trade_planet_in_10", planetKey);
            }

            if (planet.HouseNumber == 6 && ServicePlanets.Contains(planet.Planet))
            {
                AddJob(1, "career.path.service_planet_in_6", planetKey);
            }

            if (planet.HouseNumber == 7 && TradePlanets.Contains(planet.Planet))
            {
                AddBusiness(2, "career.path.trade_planet_in_7", planetKey);
            }

            if ((planet.HouseNumber is 7 or 10) &&
                planet.Planet.Equals("Rahu", StringComparison.OrdinalIgnoreCase))
            {
                AddBusiness(1, "career.path.rahu_trade", planetKey);
            }
        }

        var claims = new List<PredictionClaim>();
        if (Math.Max(jobScore, businessScore) >= MinimumScore)
        {
            var pathKey = businessScore >= jobScore + DecisionMargin
                ? "career_path_business"
                : jobScore >= businessScore + DecisionMargin
                    ? "career_path_job"
                    : "career_path_both";

            claims.Add(new PredictionClaim
            {
                Key = pathKey,
                LifeArea = PredictionLifeArea.Career,
                FocusKey = request.FocusKey,
                Tone = PredictionTone.Neutral,
                Stage = PredictionClaimStage.Natal,
                RuleIds = ruleIds.Distinct(StringComparer.OrdinalIgnoreCase).ToArray(),
                EvidenceKeys =
                [
                    $"score:job:{jobScore}",
                    $"score:business:{businessScore}",
                    .. evidenceKeys.Distinct(StringComparer.OrdinalIgnoreCase)
                ]
            });
        }

        foreach (var planet in RankFieldPlanets(houses, planets))
        {
            claims.Add(new PredictionClaim
            {
                Key = "career_field_" + planet.Planet.ToLowerInvariant(),
                LifeArea = PredictionLifeArea.Career,
                FocusKey = request.FocusKey,
                Tone = PredictionTone.Neutral,
                Stage = PredictionClaimStage.Natal,
                RuleIds = ["career.field.karaka"],
                EvidenceKeys =
                [
                    $"planet:{planet.Planet}",
                    $"planet:{planet.Planet}:house:{planet.HouseNumber}"
                ]
            });
        }

        return claims;
    }

    private static void ScoreHouseLord(
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

    private static IReadOnlyList<PlanetPredictionData> RankFieldPlanets(
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

            var strength = Math.Max(planet.StrengthScore ?? 0, 0);
            var total = weight + Math.Min(strength, 30);
            if (!weights.TryGetValue(planet.Planet, out var current) || total > current)
            {
                weights[planet.Planet] = total;
            }
        }

        foreach (var planet in planets.Values.Where(planet => planet.HouseNumber == 10))
        {
            Consider(planet, 50);
        }

        if (houses.TryGetValue(10, out var tenthHouse) &&
            planets.TryGetValue(tenthHouse.HouseLord, out var tenthLord))
        {
            Consider(tenthLord, 40);
        }

        foreach (var planet in planets.Values.Where(planet => planet.HouseNumber is 6 or 7))
        {
            if (ServicePlanets.Contains(planet.Planet) ||
                TradePlanets.Contains(planet.Planet) ||
                planet.Planet.Equals("Moon", StringComparison.OrdinalIgnoreCase) ||
                planet.Planet.Equals("Rahu", StringComparison.OrdinalIgnoreCase) ||
                planet.Planet.Equals("Ketu", StringComparison.OrdinalIgnoreCase))
            {
                Consider(planet, 20);
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
