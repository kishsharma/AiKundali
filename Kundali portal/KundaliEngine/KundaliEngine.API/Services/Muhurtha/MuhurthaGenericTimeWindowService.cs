using KundaliEngine.API.Services.Muhurtha.Models;
using KundaliEngine.Domain.Enums;

namespace KundaliEngine.API.Services.Muhurtha;

public class MuhurthaGenericTimeWindowService : IMuhurthaGenericTimeWindowService
{
    private static readonly Dictionary<DayOfWeek, int> RahuSegments = new()
    {
        [DayOfWeek.Sunday] = 8,
        [DayOfWeek.Monday] = 2,
        [DayOfWeek.Tuesday] = 7,
        [DayOfWeek.Wednesday] = 5,
        [DayOfWeek.Thursday] = 6,
        [DayOfWeek.Friday] = 4,
        [DayOfWeek.Saturday] = 3
    };

    private static readonly Dictionary<DayOfWeek, int> YamagandaSegments = new()
    {
        [DayOfWeek.Sunday] = 5,
        [DayOfWeek.Monday] = 4,
        [DayOfWeek.Tuesday] = 3,
        [DayOfWeek.Wednesday] = 2,
        [DayOfWeek.Thursday] = 1,
        [DayOfWeek.Friday] = 7,
        [DayOfWeek.Saturday] = 6
    };

    private static readonly Dictionary<DayOfWeek, int> GulikaSegments = new()
    {
        [DayOfWeek.Sunday] = 7,
        [DayOfWeek.Monday] = 6,
        [DayOfWeek.Tuesday] = 5,
        [DayOfWeek.Wednesday] = 4,
        [DayOfWeek.Thursday] = 3,
        [DayOfWeek.Friday] = 2,
        [DayOfWeek.Saturday] = 1
    };

    private static readonly HashSet<string> GoodChoghadiya = new(StringComparer.OrdinalIgnoreCase)
    {
        "Amrit",
        "Shubh",
        "Labh",
        "Char"
    };

    public IReadOnlyList<MuhurthaTimeWindow> BuildWindows(MuhurthaDayContext dayContext)
    {
        var windows = new List<MuhurthaTimeWindow>();
        var weekday = dayContext.Sunrise.DayOfWeek;
        var dayLength = dayContext.Sunset - dayContext.Sunrise;
        var segmentLength = TimeSpan.FromTicks(dayLength.Ticks / 8);

        windows.Add(BuildSegment("rahu_kalam", "Rahu Kalam", dayContext, RahuSegments[weekday], segmentLength, MuhurthaRuleSeverity.StrongAvoid, -12));
        windows.Add(BuildSegment("yamaganda", "Yamaganda", dayContext, YamagandaSegments[weekday], segmentLength, MuhurthaRuleSeverity.SoftAvoid, -6));
        windows.Add(BuildSegment("gulika_kalam", "Gulika Kalam", dayContext, GulikaSegments[weekday], segmentLength, MuhurthaRuleSeverity.SoftAvoid, -5));
        windows.Add(BuildAbhijit(dayContext));
        windows.AddRange(BuildChoghadiya(dayContext, segmentLength));

        return windows
            .Where(window => window.EndTime > dayContext.DayStart && window.StartTime < dayContext.DayEnd)
            .ToList();
    }

    private static MuhurthaTimeWindow BuildSegment(
        string ruleKey,
        string displayName,
        MuhurthaDayContext dayContext,
        int segmentNumber,
        TimeSpan segmentLength,
        MuhurthaRuleSeverity severity,
        int scoreImpact)
    {
        var start = dayContext.Sunrise + TimeSpan.FromTicks(segmentLength.Ticks * (segmentNumber - 1));
        return new MuhurthaTimeWindow
        {
            RuleKey = ruleKey,
            DisplayName = displayName,
            StartTime = start,
            EndTime = start + segmentLength,
            DefaultSeverity = severity,
            DefaultScoreImpact = scoreImpact
        };
    }

    private static MuhurthaTimeWindow BuildAbhijit(MuhurthaDayContext dayContext)
    {
        var midpoint = dayContext.Sunrise + TimeSpan.FromTicks((dayContext.Sunset - dayContext.Sunrise).Ticks / 2);
        return new MuhurthaTimeWindow
        {
            RuleKey = "abhijit_muhurtha",
            DisplayName = "Abhijit Muhurtha",
            StartTime = midpoint - TimeSpan.FromMinutes(24),
            EndTime = midpoint + TimeSpan.FromMinutes(24),
            DefaultSeverity = MuhurthaRuleSeverity.Positive,
            DefaultScoreImpact = 8,
            IsPositive = true
        };
    }

    private static IEnumerable<MuhurthaTimeWindow> BuildChoghadiya(MuhurthaDayContext dayContext, TimeSpan segmentLength)
    {
        var names = GetDayChoghadiyaSequence(dayContext.Sunrise.DayOfWeek);
        for (var i = 0; i < 8; i++)
        {
            var name = names[i];
            var start = dayContext.Sunrise + TimeSpan.FromTicks(segmentLength.Ticks * i);
            var isGood = GoodChoghadiya.Contains(name);
            yield return new MuhurthaTimeWindow
            {
                RuleKey = "choghadiya",
                DisplayName = $"{name} Choghadiya",
                StartTime = start,
                EndTime = start + segmentLength,
                DefaultSeverity = isGood ? MuhurthaRuleSeverity.Positive : MuhurthaRuleSeverity.SoftAvoid,
                DefaultScoreImpact = isGood ? 4 : -2,
                IsPositive = isGood
            };
        }
    }

    private static string[] GetDayChoghadiyaSequence(DayOfWeek dayOfWeek)
    {
        return dayOfWeek switch
        {
            DayOfWeek.Sunday => new[] { "Udveg", "Char", "Labh", "Amrit", "Kaal", "Shubh", "Rog", "Udveg" },
            DayOfWeek.Monday => new[] { "Amrit", "Kaal", "Shubh", "Rog", "Udveg", "Char", "Labh", "Amrit" },
            DayOfWeek.Tuesday => new[] { "Rog", "Udveg", "Char", "Labh", "Amrit", "Kaal", "Shubh", "Rog" },
            DayOfWeek.Wednesday => new[] { "Labh", "Amrit", "Kaal", "Shubh", "Rog", "Udveg", "Char", "Labh" },
            DayOfWeek.Thursday => new[] { "Shubh", "Rog", "Udveg", "Char", "Labh", "Amrit", "Kaal", "Shubh" },
            DayOfWeek.Friday => new[] { "Char", "Labh", "Amrit", "Kaal", "Shubh", "Rog", "Udveg", "Char" },
            DayOfWeek.Saturday => new[] { "Kaal", "Shubh", "Rog", "Udveg", "Char", "Labh", "Amrit", "Kaal" },
            _ => new[] { "Char", "Labh", "Amrit", "Kaal", "Shubh", "Rog", "Udveg", "Char" }
        };
    }
}
