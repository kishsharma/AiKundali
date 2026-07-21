namespace KundaliEngine.API.Services.CurrentLifeFocus;

public interface ILifeAreaMapper
{
    IReadOnlyList<LifeAreaActivation> Map(
        IReadOnlyList<HouseActivationScore> houseScores,
        CurrentLifeFocusConfiguration configuration);
}

public class LifeAreaMapper : ILifeAreaMapper
{
    public IReadOnlyList<LifeAreaActivation> Map(
        IReadOnlyList<HouseActivationScore> houseScores,
        CurrentLifeFocusConfiguration configuration)
    {
        return configuration.LifeAreas
            .Select(area => MapArea(area, houseScores, configuration.Scoring))
            .OrderByDescending(area => area.ActivationScore)
            .ThenByDescending(area => area.Confidence)
            .ThenBy(area => area.Title, StringComparer.OrdinalIgnoreCase)
            .ToList();
    }

    private static LifeAreaActivation MapArea(
        LifeAreaConfiguration area,
        IReadOnlyList<HouseActivationScore> houseScores,
        CurrentLifeFocusScoringConfiguration scoring)
    {
        var weightedTotal = 0.0;
        var totalWeight = 0.0;
        var support = 0;
        var pressure = 0;
        var reasons = new List<string>();
        var supportFactors = new List<string>();
        var pressureFactors = new List<string>();

        foreach (var areaHouse in area.Houses)
        {
            var house = houseScores.First(item => item.House == areaHouse.House);
            weightedTotal += house.TotalScore * areaHouse.Weight;
            totalWeight += areaHouse.Weight;
            support += (int)Math.Round(house.SupportScore * areaHouse.Weight);
            pressure += (int)Math.Round(house.PressureScore * areaHouse.Weight);
            reasons.AddRange(house.Reasons);
            supportFactors.AddRange(house.SupportiveFactors);
            pressureFactors.AddRange(house.PressureFactors);
        }

        var activation = totalWeight <= 0 ? 0 : Math.Clamp((int)Math.Round(weightedTotal / totalWeight), 0, 100);
        var activeHouses = area.Houses
            .Select(h => new { h.House, Score = houseScores.First(score => score.House == h.House).TotalScore })
            .OrderByDescending(h => h.Score)
            .ThenBy(h => h.House)
            .Where(h => h.Score >= Math.Max(35, activation - 15))
            .Take(3)
            .Select(h => h.House)
            .ToList();

        if (activeHouses.Count == 0)
        {
            activeHouses = area.Houses
                .OrderByDescending(h => h.Weight)
                .Take(2)
                .Select(h => h.House)
                .ToList();
        }

        return new LifeAreaActivation
        {
            Key = area.Key,
            Title = area.DisplayName,
            ActivationScore = activation,
            ActivationLevel = GetActivationLevel(activation, scoring),
            Tone = GetTone(support, pressure, scoring),
            Confidence = CalculateConfidence(activation, activeHouses.Count, reasons.Count, support, pressure),
            SupportScore = support,
            PressureScore = pressure,
            ActivatedHouses = activeHouses,
            ShortReasons = BuildReasons(area, activeHouses, reasons),
            SupportiveFactors = supportFactors.Distinct(StringComparer.OrdinalIgnoreCase).Take(5).ToList(),
            PressureFactors = pressureFactors.Distinct(StringComparer.OrdinalIgnoreCase).Take(5).ToList()
        };
    }

    private static string GetActivationLevel(int score, CurrentLifeFocusScoringConfiguration scoring)
    {
        if (score >= scoring.HighActivationThreshold)
        {
            return "High";
        }

        return score >= scoring.MediumActivationThreshold ? "Medium" : "Low";
    }

    private static string GetTone(int support, int pressure, CurrentLifeFocusScoringConfiguration scoring)
    {
        var delta = support - pressure;
        if (delta >= scoring.SupportiveToneThreshold)
        {
            return "Supportive";
        }

        if (-delta >= scoring.PressuredToneThreshold)
        {
            return "Pressured";
        }

        return "Mixed";
    }

    private static int CalculateConfidence(int activation, int activeHouseCount, int reasonCount, int support, int pressure)
    {
        var confirmation = Math.Min(reasonCount * 4, 24);
        var houseSpread = Math.Min(activeHouseCount * 8, 24);
        var toneClarity = Math.Min(Math.Abs(support - pressure), 18);
        return Math.Clamp(35 + (activation / 4) + confirmation + houseSpread + toneClarity, 0, 95);
    }

    private static List<string> BuildReasons(LifeAreaConfiguration area, List<int> activeHouses, List<string> reasons)
    {
        var result = new List<string>();

        foreach (var house in activeHouses.Take(2))
        {
            result.Add($"{HouseLabel(house)} is active in this theme, highlighting {HouseTheme(house)}.");
        }

        result.AddRange(
            reasons
                .Select(ToReadableReason)
                .Where(reason => !string.IsNullOrWhiteSpace(reason))
                .Distinct(StringComparer.OrdinalIgnoreCase)
                .OrderBy(GetReasonPriority)
                .Take(3));

        return result
            .Distinct(StringComparer.OrdinalIgnoreCase)
            .Take(3)
            .ToList();
    }

    private static string ToReadableReason(string reason)
    {
        if (reason.Contains("Mahadasha lord", StringComparison.OrdinalIgnoreCase))
        {
            return reason
                .Replace("occupies house", "directly occupies natal house", StringComparison.OrdinalIgnoreCase)
                .Replace("owns house", "rules natal house", StringComparison.OrdinalIgnoreCase);
        }

        if (reason.Contains("Antardasha lord", StringComparison.OrdinalIgnoreCase))
        {
            return reason
                .Replace("occupies house", "is placed in natal house", StringComparison.OrdinalIgnoreCase)
                .Replace("owns house", "rules natal house", StringComparison.OrdinalIgnoreCase);
        }

        if (reason.Contains("transits house", StringComparison.OrdinalIgnoreCase))
        {
            return reason
                .Replace("transits house", "is transiting natal house", StringComparison.OrdinalIgnoreCase)
                .Replace(" from natal Lagna", " from Lagna", StringComparison.OrdinalIgnoreCase)
                .Replace(" from natal Moon", " from Moon", StringComparison.OrdinalIgnoreCase);
        }

        if (reason.Contains("aspects house", StringComparison.OrdinalIgnoreCase))
        {
            return reason
                .Replace("aspects house", "is aspecting natal house", StringComparison.OrdinalIgnoreCase)
                .Replace(" from natal Lagna", " from Lagna", StringComparison.OrdinalIgnoreCase)
                .Replace(" from natal Moon", " from Moon", StringComparison.OrdinalIgnoreCase);
        }

        return reason.Trim();
    }

    private static int GetReasonPriority(string reason)
    {
        if (reason.Contains("Antardasha", StringComparison.OrdinalIgnoreCase))
        {
            return 0;
        }

        if (reason.Contains("Mahadasha", StringComparison.OrdinalIgnoreCase))
        {
            return 1;
        }

        if (reason.Contains("transiting", StringComparison.OrdinalIgnoreCase))
        {
            return 2;
        }

        if (reason.Contains("aspecting", StringComparison.OrdinalIgnoreCase))
        {
            return 3;
        }

        return 4;
    }

    private static string HouseLabel(int value)
    {
        return value switch
        {
            1 => "1st house self/vitality axis",
            2 => "2nd house money and family axis",
            3 => "3rd house effort and courage axis",
            4 => "4th house home and emotional base",
            5 => "5th house learning and children axis",
            6 => "6th house work, debt and competition axis",
            7 => "7th house partnership axis",
            8 => "8th house shared-resource and uncertainty axis",
            9 => "9th house fortune, dharma and travel axis",
            10 => "10th house career and public-role axis",
            11 => "11th house gains and network axis",
            12 => "12th house rest, expense and foreign axis",
            _ => $"House {value}"
        };
    }

    private static string HouseTheme(int value)
    {
        return value switch
        {
            1 => "personal direction, body and confidence",
            2 => "cash flow, speech, family and stored resources",
            3 => "initiative, communication, short travel and persistence",
            4 => "peace of mind, residence, mother and property foundations",
            5 => "education, creativity, children and judgment",
            6 => "service, routine, debts, disputes and competitors",
            7 => "marriage, agreements, clients and direct partnerships",
            8 => "joint assets, sudden changes, research and vulnerability",
            9 => "guidance, higher learning, long travel and belief",
            10 => "profession, status, responsibility and visible work",
            11 => "income, networks, elder support and ambitions",
            12 => "sleep, expenses, retreat, isolation and foreign links",
            _ => "this life area"
        };
    }
}

