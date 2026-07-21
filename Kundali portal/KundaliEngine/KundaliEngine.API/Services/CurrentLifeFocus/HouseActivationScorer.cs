using KundaliEngine.API.Helper_functions;
using KundaliEngine.Domain.Enums;
using KundaliEngine.Domain.Models;

namespace KundaliEngine.API.Services.CurrentLifeFocus;

public interface IHouseActivationScorer
{
    IReadOnlyList<HouseActivationScore> Score(HouseActivationContext context);
}

public class HouseActivationScorer : IHouseActivationScorer
{
    private static readonly HashSet<Planet> RankingTransitPlanets = new()
    {
        Planet.Saturn,
        Planet.Jupiter,
        Planet.Rahu,
        Planet.Ketu,
        Planet.Mars
    };

    public IReadOnlyList<HouseActivationScore> Score(HouseActivationContext context)
    {
        return Enumerable.Range(1, 12)
            .Select(house => ScoreHouse(house, context))
            .ToList();
    }

    private static HouseActivationScore ScoreHouse(int house, HouseActivationContext context)
    {
        var score = new HouseActivationScore { House = house };
        ScoreDasha(house, context, score);
        ScoreGochar(house, context, score);
        ScoreNatalSensitivity(house, context, score);

        score.TotalScore = ClampScore(
            (score.DashaScore * context.Scoring.DashaActivationWeight)
            + (score.GocharScore * context.Scoring.GocharActivationWeight)
            + (score.NatalSensitivityScore * context.Scoring.NatalSensitivityWeight));

        return score;
    }

    private static void ScoreDasha(int house, HouseActivationContext context, HouseActivationScore score)
    {
        AddDashaPlanet(context.Mahadasha, context.Scoring.MahadashaWeight, "Mahadasha", house, context, score);
        AddDashaPlanet(context.Antardasha, context.Scoring.AntardashaWeight, "Antardasha", house, context, score);
    }

    private static void AddDashaPlanet(
        Planet planet,
        double weight,
        string label,
        int house,
        HouseActivationContext context,
        HouseActivationScore score)
    {
        var natalPosition = context.NatalPlanets.FirstOrDefault(position => position.Planet == planet);
        if (natalPosition == null)
        {
            return;
        }

        var contribution = 0;
        if ((int)natalPosition.House == house)
        {
            contribution += (int)Math.Round(70 * weight);
            score.Reasons.Add($"{label} lord {planet} occupies house {house}.");
        }

        var houseSign = GetHouseSign(context.NatalAscendantSign, house);
        if (string.Equals(HelperData.GetSignOwner(houseSign), planet.ToString(), StringComparison.OrdinalIgnoreCase))
        {
            contribution += (int)Math.Round(45 * weight);
            score.Reasons.Add($"{label} lord {planet} owns house {house}.");
        }

        contribution = Math.Clamp(contribution, 0, 100);
        score.DashaScore = Math.Clamp(score.DashaScore + contribution, 0, 100);
        AddPlanetTone(planet, contribution, score, $"{label} lord {planet}");
    }

    private static void ScoreGochar(int house, HouseActivationContext context, HouseActivationScore score)
    {
        AddTransitSet(house, context.TransitPlanetsFromLagna, context.TransitAspectsFromLagna, "from natal Lagna", score);
        AddTransitSet(house, context.TransitPlanetsFromMoon, context.TransitAspectsFromMoon, "from natal Moon", score, 0.75);
        score.GocharScore = Math.Clamp(score.GocharScore, 0, 100);
    }

    private static void AddTransitSet(
        int house,
        IReadOnlyList<PlanetPosition> transitPlanets,
        AspectCalculationResult aspects,
        string reference,
        HouseActivationScore score,
        double referenceWeight = 1.0)
    {
        foreach (var transit in transitPlanets.Where(planet => RankingTransitPlanets.Contains(planet.Planet)))
        {
            var planetWeight = GetTransitWeight(transit.Planet) * referenceWeight;
            if ((int)transit.House == house)
            {
                var contribution = (int)Math.Round(45 * planetWeight);
                score.GocharScore += contribution;
                score.Reasons.Add($"{transit.Planet} transits house {house} {reference}.");
                AddPlanetTone(transit.Planet, contribution, score, $"{transit.Planet} transit");
            }

            if (aspects.HouseAspects.Any(aspect => aspect.FromPlanet == transit.Planet && aspect.ToHouse == house))
            {
                var contribution = (int)Math.Round(28 * planetWeight);
                score.GocharScore += contribution;
                score.Reasons.Add($"{transit.Planet} aspects house {house} {reference}.");
                AddPlanetTone(transit.Planet, contribution, score, $"{transit.Planet} aspect");
            }
        }
    }

    private static void ScoreNatalSensitivity(int house, HouseActivationContext context, HouseActivationScore score)
    {
        var natalScore = context.NatalHouseScores.FirstOrDefault(item => (int)item.HouseNumber == house);
        if (natalScore == null)
        {
            score.NatalSensitivityScore = 35;
            return;
        }

        var occupants = context.NatalPlanets.Count(position => (int)position.House == house);
        score.NatalSensitivityScore = Math.Clamp((int)Math.Round((natalScore.Score * 0.75) + Math.Min(occupants * 10, 25)), 0, 100);

        if (score.NatalSensitivityScore >= 70)
        {
            score.SupportScore += 5;
            score.SupportiveFactors.Add($"Natal house {house} has a steadier base score, so this theme has more underlying capacity.");
        }

        if (score.NatalSensitivityScore < 40)
        {
            score.PressureScore += 5;
            score.PressureFactors.Add($"Natal house {house} is sensitive in the birth chart, so this theme needs cleaner handling.");
        }
    }

    private static void AddPlanetTone(Planet planet, int contribution, HouseActivationScore score, string source)
    {
        if (contribution <= 0)
        {
            return;
        }

        var sourceType = GetSourceType(source);
        if (planet is Planet.Jupiter or Planet.Venus)
        {
            score.SupportScore += Math.Max(4, contribution / 5);
            score.SupportiveFactors.Add(BuildSupportFactor(planet, sourceType));
            return;
        }

        if (planet is Planet.Saturn or Planet.Rahu or Planet.Ketu or Planet.Mars)
        {
            score.PressureScore += Math.Max(4, contribution / 5);
            score.PressureFactors.Add(BuildPressureFactor(planet, sourceType));
            return;
        }

        score.SupportScore += Math.Max(2, contribution / 8);
    }

    private static string GetSourceType(string source)
    {
        if (source.Contains("Mahadasha", StringComparison.OrdinalIgnoreCase))
        {
            return "Mahadasha";
        }

        if (source.Contains("Antardasha", StringComparison.OrdinalIgnoreCase))
        {
            return "Antardasha";
        }

        if (source.Contains("aspect", StringComparison.OrdinalIgnoreCase))
        {
            return "aspect";
        }

        if (source.Contains("transit", StringComparison.OrdinalIgnoreCase))
        {
            return "transit";
        }

        return "activation";
    }

    private static string BuildSupportFactor(Planet planet, string sourceType)
    {
        var quality = planet switch
        {
            Planet.Jupiter => "guidance, growth and protection",
            Planet.Venus => "agreement, comfort and relationship ease",
            _ => "constructive support"
        };

        return sourceType switch
        {
            "Mahadasha" => $"Mahadasha lord {planet} adds a supportive background through {quality}.",
            "Antardasha" => $"Antardasha lord {planet} can make {quality} more immediately available.",
            "aspect" => $"{planet} aspect adds a supportive influence through {quality}.",
            "transit" => $"{planet} transit brings a constructive opening through {quality}.",
            _ => $"{planet} adds support through {quality}."
        };
    }

    private static string BuildPressureFactor(Planet planet, string sourceType)
    {
        var quality = planet switch
        {
            Planet.Saturn => "delay, duty and sustained responsibility",
            Planet.Rahu => "restlessness, urgency and unusual pressure",
            Planet.Ketu => "detachment, uncertainty and sudden course correction",
            Planet.Mars => "heat, conflict and quick action",
            _ => "pressure"
        };

        return sourceType switch
        {
            "Mahadasha" => $"Mahadasha lord {planet} sets a pressure backdrop through {quality}.",
            "Antardasha" => $"Antardasha lord {planet} can make {quality} feel more immediate.",
            "aspect" => $"{planet} aspect adds pressure through {quality}.",
            "transit" => $"{planet} transit activates this area through {quality}.",
            _ => $"{planet} adds pressure through {quality}."
        };
    }

    private static double GetTransitWeight(Planet planet)
    {
        return planet switch
        {
            Planet.Saturn => 1.25,
            Planet.Jupiter => 1.15,
            Planet.Rahu or Planet.Ketu => 1.1,
            Planet.Mars => 0.85,
            _ => 0
        };
    }

    private static ZodiacSign GetHouseSign(ZodiacSign ascendantSign, int house)
    {
        var signIndex = (((int)ascendantSign - 1) + (house - 1)) % 12;
        return (ZodiacSign)(signIndex + 1);
    }

    private static int ClampScore(double value) => Math.Clamp((int)Math.Round(value), 0, 100);
}
