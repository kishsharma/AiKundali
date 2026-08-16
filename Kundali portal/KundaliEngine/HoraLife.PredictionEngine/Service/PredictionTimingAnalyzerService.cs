using HoraLife.PredictionEngine.Enums;
using HoraLife.PredictionEngine.Models;

namespace HoraLife.PredictionEngine.Services;

internal sealed class PredictionTimingAnalyzerService
{
    private const int HighRelevanceWeight = 3;
    private const int SignificatorWeight = 2;
    private const int SecondaryWeight = 1;
    private const int DefaultMaxWindows = 5;
    private const double SubstantialOverlapThreshold = 0.5d;

    public IReadOnlyList<PredictionTimingWindow> Analyze(
        PredictionRequest request,
        PredictionAreaProfile profile,
        PredictionEvidenceSet evidence)
    {
        ArgumentNullException.ThrowIfNull(request);
        ArgumentNullException.ThrowIfNull(profile);
        ArgumentNullException.ThrowIfNull(evidence);

        if (evidence.DashaPeriods.Count == 0)
        {
            return Array.Empty<PredictionTimingWindow>();
        }

        var relevance = BuildRelevantPlanets(profile, evidence);
        if (relevance.Count == 0)
        {
            return Array.Empty<PredictionTimingWindow>();
        }

        var options = ResolveOptions(request, profile);

        var periodsById = evidence.DashaPeriods
            .ToDictionary(period => period.PeriodId, StringComparer.OrdinalIgnoreCase);

        var parentIds = evidence.DashaPeriods
            .Where(period => !string.IsNullOrWhiteSpace(period.ParentPeriodId))
            .Select(period => period.ParentPeriodId!)
            .ToHashSet(StringComparer.OrdinalIgnoreCase);

        var candidatePeriods = evidence.DashaPeriods
            .Where(period => !parentIds.Contains(period.PeriodId))
            .Where(period => IsWithinFutureHorizon(period, request.ReferenceDate, options.FutureHorizonYears))
            .OrderBy(period => period.StartDate)
            .ToArray();

        var windows = new List<WindowScore>();

        foreach (var period in candidatePeriods)
        {
            var window = ScoreWindow(period, periodsById, relevance, request.ReferenceDate);
            if (window is not null)
            {
                windows.Add(window);
            }
        }

        var ranked = windows
            .OrderByDescending(window => window.ActivationScore)
            .ThenByDescending(window => window.StrengthRank)
            .ThenBy(window => window.Proximity)
            .ThenBy(window => window.Window.StartDate)
            .ToList();

        var deduplicated = CollapseRedundantWindows(ranked);
        var selected = SelectBestWindows(deduplicated, options.MaxWindows);

        return selected
            .Select(window => window.Window)
            .ToArray();
    }

    private static Dictionary<string, RelevantPlanet> BuildRelevantPlanets(
        PredictionAreaProfile profile,
        PredictionEvidenceSet evidence)
    {
        var relevant = new Dictionary<string, RelevantPlanet>(StringComparer.OrdinalIgnoreCase);

        foreach (var primaryLord in evidence.PrimaryHouseLords)
        {
            AddRelevantPlanet(
                relevant,
                primaryLord.Planet,
                HighRelevanceWeight,
                "relationships.timing.primary_house_lord",
                $"planet:{primaryLord.Planet}:primary_house_lord");
        }

        foreach (var significator in evidence.NaturalSignificators)
        {
            AddRelevantPlanet(
                relevant,
                significator.Planet,
                SignificatorWeight,
                "relationships.timing.natural_significator",
                $"planet:{significator.Planet}:natural_significator");
        }

        var primaryHouseNumbers = profile.PrimaryHouses.ToHashSet();
        foreach (var houseAspect in evidence.HouseAspects.Where(fact => primaryHouseNumbers.Contains(fact.HouseNumber)))
        {
            foreach (var planet in houseAspect.AspectedBy)
            {
                AddRelevantPlanet(
                    relevant,
                    planet,
                    SecondaryWeight,
                    "relationships.timing.primary_house_aspector",
                    $"house:{houseAspect.HouseNumber}:aspected_by:{planet}");
            }
        }

        foreach (var supportingHouse in evidence.SupportingHouses)
        {
            if (!string.IsNullOrWhiteSpace(supportingHouse.HouseLord))
            {
                AddRelevantPlanet(
                    relevant,
                    supportingHouse.HouseLord,
                    SecondaryWeight,
                    "relationships.timing.supporting_house_lord",
                    $"house:{supportingHouse.HouseNumber}:lord:{supportingHouse.HouseLord}");
            }
        }

        foreach (var divisionalEvidence in evidence.DivisionalFocusEvidence)
        {
            foreach (var house in divisionalEvidence.Houses)
            {
                if (house.HouseLordPlanet is not null)
                {
                    AddRelevantPlanet(
                        relevant,
                        house.HouseLordPlanet.Planet,
                        SecondaryWeight,
                        "relationships.timing.divisional_house_lord",
                        house.HouseLordEvidenceKey);
                }

                foreach (var occupant in house.Occupants)
                {
                    AddRelevantPlanet(
                        relevant,
                        occupant.Planet,
                        SecondaryWeight,
                        "relationships.timing.divisional_house_occupant",
                        $"divisional:{divisionalEvidence.ChartType}:house:{house.HouseNumber}:occupant:{occupant.Planet}");
                }
            }
        }

        return relevant;
    }

    private static TimingSelectionOptions ResolveOptions(
        PredictionRequest request,
        PredictionAreaProfile profile)
    {
        var config = profile.CustomQuestionTiming
            .FirstOrDefault(item => item.FocusKey.Equals(request.FocusKey, StringComparison.OrdinalIgnoreCase));

        return new TimingSelectionOptions(
            config?.FutureHorizonYears,
            config?.MaxWindows is > 0 ? config.MaxWindows : DefaultMaxWindows);
    }

    private static bool IsWithinFutureHorizon(
        DashaPeriod period,
        DateTimeOffset referenceDate,
        int? futureHorizonYears)
    {
        if (period.EndDate <= referenceDate)
        {
            return false;
        }

        if (futureHorizonYears is not > 0)
        {
            return true;
        }

        var horizonEnd = referenceDate.AddYears(futureHorizonYears.Value);
        return period.StartDate < horizonEnd;
    }

    private static void AddRelevantPlanet(
        IDictionary<string, RelevantPlanet> relevant,
        string planet,
        int weight,
        string reasonKey,
        string evidenceKey)
    {
        if (string.IsNullOrWhiteSpace(planet))
        {
            return;
        }

        if (!relevant.TryGetValue(planet, out var current))
        {
            relevant[planet] = new RelevantPlanet(
                planet,
                weight,
                new HashSet<string>(StringComparer.Ordinal) { reasonKey },
                new HashSet<string>(StringComparer.OrdinalIgnoreCase) { evidenceKey });
            return;
        }

        current.Weight = Math.Max(current.Weight, weight);
        current.ReasonKeys.Add(reasonKey);
        current.EvidenceKeys.Add(evidenceKey);
    }

    private static WindowScore? ScoreWindow(
        DashaPeriod period,
        IReadOnlyDictionary<string, DashaPeriod> periodsById,
        IReadOnlyDictionary<string, RelevantPlanet> relevance,
        DateTimeOffset referenceDate)
    {
        var hierarchy = ExpandHierarchy(period, periodsById);
        var triggerPlanets = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
        var evidenceKeys = new HashSet<string>(StringComparer.OrdinalIgnoreCase);

        var score = 0;
        var highRelevanceActivated = false;
        var significatorActivated = false;

        foreach (var hierarchyPeriod in hierarchy)
        {
            if (!relevance.TryGetValue(hierarchyPeriod.Planet, out var relevantPlanet))
            {
                continue;
            }

            score += relevantPlanet.Weight;
            triggerPlanets.Add(relevantPlanet.Planet);
            evidenceKeys.Add($"dasha:{hierarchyPeriod.PeriodId}");
            evidenceKeys.Add($"dasha-level:{hierarchyPeriod.Level.ToString().ToLowerInvariant()}");

            foreach (var key in relevantPlanet.EvidenceKeys)
            {
                evidenceKeys.Add(key);
            }

            if (relevantPlanet.Weight >= HighRelevanceWeight)
            {
                highRelevanceActivated = true;
            }

            if (relevantPlanet.Weight >= SignificatorWeight &&
                relevantPlanet.Weight < HighRelevanceWeight)
            {
                significatorActivated = true;
            }
        }

        if (score == 0)
        {
            return null;
        }

        if (triggerPlanets.Count >= 2)
        {
            score += 2;
        }

        if (highRelevanceActivated && significatorActivated)
        {
            score += 1;
        }

        var strength = score switch
        {
            >= 8 => "VeryStrong",
            >= 5 => "Strong",
            >= 2 => "Candidate",
            _ => null
        };

        if (strength is null)
        {
            return null;
        }

        var mahadashaPeriodId = hierarchy
            .FirstOrDefault(item => item.Level == DashaLevel.Mahadasha)?.PeriodId
            ?? period.PeriodId;

        var antardashaPeriodId = hierarchy
            .FirstOrDefault(item => item.Level == DashaLevel.Antardasha)?.PeriodId
            ?? period.PeriodId;

        return new WindowScore(
            new PredictionTimingWindow
            {
                StartDate = period.StartDate,
                EndDate = period.EndDate,
                Strength = strength,
                TriggerPlanets = triggerPlanets.OrderBy(name => name).ToArray(),
                EvidenceKeys = evidenceKeys.ToArray()
            },
            score,
            StrengthRank(strength),
            CalculateProximity(period, referenceDate),
            mahadashaPeriodId,
            antardashaPeriodId);
    }

    private static List<WindowScore> CollapseRedundantWindows(
        IReadOnlyList<WindowScore> ranked)
    {
        var selected = new List<WindowScore>();

        foreach (var window in ranked)
        {
            var existingIndex = selected.FindIndex(existing =>
                SharesMdAdActivation(existing, window) &&
                HasSubstantialOverlap(existing.Window, window.Window));

            if (existingIndex < 0)
            {
                selected.Add(window);
                continue;
            }

            if (Compare(window, selected[existingIndex]) < 0)
            {
                selected[existingIndex] = window;
            }
        }

        return selected
            .OrderByDescending(window => window.ActivationScore)
            .ThenByDescending(window => window.StrengthRank)
            .ThenBy(window => window.Proximity)
            .ThenBy(window => window.Window.StartDate)
            .ToList();
    }

    private static List<WindowScore> SelectBestWindows(
        IReadOnlyList<WindowScore> ranked,
        int maxWindows)
    {
        if (ranked.Count == 0 || maxWindows <= 0)
        {
            return [];
        }

        var selected = ranked
            .Take(maxWindows)
            .ToList();

        var earliestMeaningful = ranked
            .OrderBy(window => window.Proximity)
            .ThenBy(window => window.Window.StartDate)
            .FirstOrDefault();

        if (earliestMeaningful is not null &&
            !selected.Any(window => SameWindow(window, earliestMeaningful)) &&
            selected.Count > 0)
        {
            selected[^1] = earliestMeaningful;
            selected = selected
                .GroupBy(window => WindowKey(window), StringComparer.OrdinalIgnoreCase)
                .Select(group => group.First())
                .OrderByDescending(window => window.ActivationScore)
                .ThenByDescending(window => window.StrengthRank)
                .ThenBy(window => window.Proximity)
                .ThenBy(window => window.Window.StartDate)
                .Take(maxWindows)
                .ToList();
        }

        return selected;
    }

    private static bool SharesMdAdActivation(WindowScore left, WindowScore right)
    {
        return left.MahadashaPeriodId.Equals(right.MahadashaPeriodId, StringComparison.OrdinalIgnoreCase) &&
               left.AntardashaPeriodId.Equals(right.AntardashaPeriodId, StringComparison.OrdinalIgnoreCase);
    }

    private static bool HasSubstantialOverlap(
        PredictionTimingWindow left,
        PredictionTimingWindow right)
    {
        var overlapStart = left.StartDate > right.StartDate ? left.StartDate : right.StartDate;
        var overlapEnd = left.EndDate < right.EndDate ? left.EndDate : right.EndDate;

        if (overlapEnd <= overlapStart)
        {
            return false;
        }

        var overlap = overlapEnd - overlapStart;
        var shorterDuration = left.EndDate - left.StartDate;
        if (right.EndDate - right.StartDate < shorterDuration)
        {
            shorterDuration = right.EndDate - right.StartDate;
        }

        if (shorterDuration <= TimeSpan.Zero)
        {
            return false;
        }

        return overlap.TotalSeconds / shorterDuration.TotalSeconds >= SubstantialOverlapThreshold;
    }

    private static int Compare(WindowScore left, WindowScore right)
    {
        var activation = right.ActivationScore.CompareTo(left.ActivationScore);
        if (activation != 0)
        {
            return activation;
        }

        var strength = right.StrengthRank.CompareTo(left.StrengthRank);
        if (strength != 0)
        {
            return strength;
        }

        var proximity = left.Proximity.CompareTo(right.Proximity);
        if (proximity != 0)
        {
            return proximity;
        }

        return left.Window.StartDate.CompareTo(right.Window.StartDate);
    }

    private static bool SameWindow(WindowScore left, WindowScore right)
    {
        return WindowKey(left).Equals(WindowKey(right), StringComparison.OrdinalIgnoreCase);
    }

    private static string WindowKey(WindowScore window)
    {
        return string.Join(
            "|",
            window.MahadashaPeriodId,
            window.AntardashaPeriodId,
            window.Window.StartDate.UtcDateTime.Ticks,
            window.Window.EndDate.UtcDateTime.Ticks,
            window.Window.Strength);
    }

    private static int StrengthRank(string strength)
    {
        return strength switch
        {
            "VeryStrong" => 3,
            "Strong" => 2,
            "Candidate" => 1,
            _ => 0
        };
    }

    private static TimeSpan CalculateProximity(
        DashaPeriod period,
        DateTimeOffset referenceDate)
    {
        return period.StartDate <= referenceDate
            ? TimeSpan.Zero
            : period.StartDate - referenceDate;
    }

    private static IReadOnlyList<DashaPeriod> ExpandHierarchy(
        DashaPeriod period,
        IReadOnlyDictionary<string, DashaPeriod> periodsById)
    {
        var hierarchy = new List<DashaPeriod>();
        DashaPeriod? current = period;

        while (current is not null)
        {
            hierarchy.Add(current);
            current = current.ParentPeriodId is not null &&
                      periodsById.TryGetValue(current.ParentPeriodId, out var parent)
                ? parent
                : null;
        }

        hierarchy.Reverse();
        return hierarchy;
    }

    private sealed record RelevantPlanet(
        string Planet,
        int InitialWeight,
        HashSet<string> ReasonKeys,
        HashSet<string> EvidenceKeys)
    {
        public int Weight { get; set; } = InitialWeight;
    }

    private sealed record TimingSelectionOptions(
        int? FutureHorizonYears,
        int MaxWindows);

    private sealed record WindowScore(
        PredictionTimingWindow Window,
        int ActivationScore,
        int StrengthRank,
        TimeSpan Proximity,
        string MahadashaPeriodId,
        string AntardashaPeriodId);
}