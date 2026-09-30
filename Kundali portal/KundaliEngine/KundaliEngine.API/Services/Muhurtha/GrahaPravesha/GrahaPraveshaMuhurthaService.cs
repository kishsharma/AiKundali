using KundaliEngine.API.DTOs.Muhurtha.GrahaPravesha;
using KundaliEngine.API.Services.Muhurtha.Models;
using KundaliEngine.Domain.Enums;
using KundaliEngine.Domain.Models.Muhurtha;

namespace KundaliEngine.API.Services.Muhurtha.GrahaPravesha;

public interface IGrahaPraveshaMuhurthaService
{
    GrahaPraveshaSearchResponseDto Search(GrahaPraveshaSearchRequestDto request);

    GrahaPraveshaDayResponseDto AnalyzeDate(DateOnly date, GrahaPraveshaLocationDto location, string calendarSystem, GrahaPraveshaBirthChartDto? birthChart);
}

public sealed class GrahaPraveshaMuhurthaService : IGrahaPraveshaMuhurthaService
{
    private const int MaxDays = 62;

    private readonly IGrahaPraveshaProfileProvider _profileProvider;
    private readonly MuhurthaMetadataService _metadataService;
    private readonly IMuhurthaSunriseSunsetService _sunriseSunsetService;
    private readonly IMuhurthaGenericTimeWindowService _timeWindowService;
    private readonly IPanchangSnapshotReader _snapshotReader;
    private readonly GrahaPraveshaRuleEngine _ruleEngine;
    private readonly IGrahaPraveshaBirthChartEvaluator _birthChartEvaluator;

    public GrahaPraveshaMuhurthaService(
        IGrahaPraveshaProfileProvider profileProvider,
        MuhurthaMetadataService metadataService,
        IMuhurthaSunriseSunsetService sunriseSunsetService,
        IMuhurthaGenericTimeWindowService timeWindowService,
        IPanchangSnapshotReader snapshotReader,
        GrahaPraveshaRuleEngine ruleEngine,
        IGrahaPraveshaBirthChartEvaluator birthChartEvaluator)
    {
        _profileProvider = profileProvider;
        _metadataService = metadataService;
        _sunriseSunsetService = sunriseSunsetService;
        _timeWindowService = timeWindowService;
        _snapshotReader = snapshotReader;
        _ruleEngine = ruleEngine;
        _birthChartEvaluator = birthChartEvaluator;
    }

    public GrahaPraveshaSearchResponseDto Search(GrahaPraveshaSearchRequestDto request)
    {
        Validate(request.Location, request.FromDate, request.ToDate, request.CalendarSystem);
        var profile = _profileProvider.GetProfile();
        var results = new List<GrahaPraveshaWindowDto>();
        var rejectionCounts = new Dictionary<string, int>(StringComparer.OrdinalIgnoreCase);

        foreach (var date in MuhurthaIntervalMath.EnumerateDates(request.FromDate, request.ToDate))
        {
            var day = BuildDay(date, request.Location, request.CalendarSystem, request.BirthChart, profile, includeRejected: true);
            foreach (var interval in day.Intervals.Where(item => item.Status == "REJECTED"))
            {
                foreach (var fail in interval.Evidence.Where(item => item.Result == "FAIL"))
                {
                    rejectionCounts[fail.RuleId] = rejectionCounts.GetValueOrDefault(fail.RuleId) + 1;
                }
            }

            results.AddRange(Merge(day.Intervals.Where(item =>
                item.Status != "REJECTED" &&
                item.EndTime - item.StartTime >= profile.MinimumWindow)));
        }

        return new GrahaPraveshaSearchResponseDto
        {
            ProfileId = profile.ProfileId,
            ProfileVersion = profile.Version,
            CalendarSystem = request.CalendarSystem.Trim().ToUpperInvariant(),
            Location = request.Location,
            SearchRange = new GrahaPraveshaRangeDto
            {
                From = request.FromDate.ToString("yyyy-MM-dd"),
                To = request.ToDate.ToString("yyyy-MM-dd")
            },
            Results = results,
            RejectionCounts = rejectionCounts,
            KnownGaps = profile.KnownGaps
        };
    }

    public GrahaPraveshaDayResponseDto AnalyzeDate(
        DateOnly date,
        GrahaPraveshaLocationDto location,
        string calendarSystem,
        GrahaPraveshaBirthChartDto? birthChart)
    {
        Validate(location, date, date, calendarSystem);
        var profile = _profileProvider.GetProfile();
        return BuildDay(date, location, calendarSystem, birthChart, profile, includeRejected: true);
    }

    private GrahaPraveshaDayResponseDto BuildDay(
        DateOnly date,
        GrahaPraveshaLocationDto location,
        string calendarSystem,
        GrahaPraveshaBirthChartDto? birthChart,
        GrahaPraveshaProfile profile,
        bool includeRejected)
    {
        var offset = GrahaPraveshaTime.GetOffset(location.Timezone, date);
        var muhurthaLocation = new MuhurthaLocation
        {
            Name = location.City,
            Latitude = location.Latitude,
            Longitude = location.Longitude,
            Timezone = location.Timezone
        };
        var (sunrise, sunset) = _sunriseSunsetService.Calculate(date, muhurthaLocation, offset);
        if (sunset <= sunrise)
        {
            sunset = sunrise.AddHours(12);
        }

        var dayContext = new MuhurthaDayContext
        {
            Date = date,
            Sunrise = sunrise,
            Sunset = sunset,
            DayStart = sunrise,
            DayEnd = sunset,
            Location = muhurthaLocation
        };

        var gaps = new List<string>(profile.KnownGaps);
        IReadOnlyList<MuhurthaTimeWindow> windows;
        try
        {
            windows = _timeWindowService.BuildWindows(dayContext);
        }
        catch (Exception ex)
        {
            windows = Array.Empty<MuhurthaTimeWindow>();
            gaps.Add($"Prohibited-period windows could not be calculated: {ex.GetType().Name}.");
        }

        var cutOutKeys = new HashSet<string>(profile.CutOutWindowKeys, StringComparer.OrdinalIgnoreCase);
        var prohibited = windows
            .Where(window => cutOutKeys.Contains(window.RuleKey))
            .Select(window => new TimeRange(window.StartTime, window.EndTime))
            .ToList();
        var prohibitedWindows = windows.Where(window => cutOutKeys.Contains(window.RuleKey)).ToList();
        var timing = prohibitedWindows.Select(MapPeriod).ToList();
        var vastuRules = _metadataService.GetVastuEligibilityRules(MuhurthaEventType.GruhaPravesha);
        var cache = new Dictionary<long, PanchangSnapshot>();

        PanchangSnapshot Read(DateTimeOffset instant)
        {
            var key = instant.ToUnixTimeSeconds();
            if (!cache.TryGetValue(key, out var snapshot))
            {
                snapshot = _snapshotReader.Read(instant, location.Latitude, location.Longitude);
                cache[key] = snapshot;
            }

            return snapshot;
        }

        var atoms = PanchangBoundaryWalker.Split(
            sunrise,
            sunset,
            instant => Read(instant).Key,
            TimeSpan.FromSeconds(1));

        var intervals = new List<GrahaPraveshaWindowDto>();
        foreach (var atom in atoms)
        {
            var snapshot = Read(atom.Start);
            var source = new TimeRange(atom.Start, atom.End);
            var free = MuhurthaIntervalMath.Subtract(source, prohibited);
            var blocked = MuhurthaIntervalMath.Intersections(source, prohibited);

            foreach (var piece in free.Where(piece => piece.End - piece.Start >= TimeSpan.FromMinutes(1)))
            {
                intervals.Add(EvaluatePiece(date, piece, snapshot, Array.Empty<string>(), profile, vastuRules, birthChart, location, timing));
            }

            if (!includeRejected)
            {
                continue;
            }

            foreach (var piece in blocked.Where(piece => piece.End - piece.Start >= TimeSpan.FromMinutes(1)))
            {
                var overlapKeys = prohibitedWindows
                    .Where(window => window.StartTime < piece.End && window.EndTime > piece.Start)
                    .Select(window => window.RuleKey)
                    .Distinct(StringComparer.OrdinalIgnoreCase)
                    .ToList();
                intervals.Add(EvaluatePiece(date, piece, snapshot, overlapKeys, profile, vastuRules, birthChart, location, timing));
            }
        }

        return new GrahaPraveshaDayResponseDto
        {
            ProfileId = profile.ProfileId,
            ProfileVersion = profile.Version,
            CalendarSystem = calendarSystem.Trim().ToUpperInvariant(),
            Date = date.ToString("yyyy-MM-dd"),
            Location = location,
            Sunrise = sunrise,
            Sunset = sunset,
            TimingRestrictions = timing,
            Intervals = intervals.OrderBy(item => item.StartTime).ToList(),
            KnownGaps = gaps
        };
    }

    private GrahaPraveshaWindowDto EvaluatePiece(
        DateOnly date,
        TimeRange piece,
        PanchangSnapshot snapshot,
        IReadOnlyList<string> overlapKeys,
        GrahaPraveshaProfile profile,
        IReadOnlyList<MuhurthaVastuEligibilityRuleConfiguration> vastuRules,
        GrahaPraveshaBirthChartDto? birthChart,
        GrahaPraveshaLocationDto location,
        List<GrahaPraveshaPeriodDto> timing)
    {
        var vara = piece.Start.DayOfWeek.ToString();
        var vastu = VastuTableMatcher.Match(vastuRules, snapshot.Paksha, snapshot.Tithi, vara, snapshot.MoonSign);
        var kalashaCount = KalashaChakraEvaluator.CountInclusive(snapshot.SunNakshatraIndex, snapshot.MoonNakshatraIndex);
        var facts = new GrahaPraveshaFacts
        {
            Tithi = snapshot.Tithi,
            Paksha = snapshot.Paksha,
            Nakshatra = snapshot.Nakshatra,
            Yoga = snapshot.Yoga,
            Karana = snapshot.Karana,
            Vara = vara,
            MoonSign = snapshot.MoonSign,
            Lagna = snapshot.Lagna,
            KalashaCount = kalashaCount,
            VastuTableMatched = vastu.Matched,
            VastuApplicability = vastu.Applicability,
            VastuAhuti = vastu.Ahuti,
            OverlappingWindows = overlapKeys
        };

        var outcome = _ruleEngine.Evaluate(facts, profile);
        var evidence = outcome.Evidence.ToList();
        var score = outcome.Score;
        var hardRejected = outcome.HardRejected;
        var blocksRecommended = outcome.BlocksRecommended;

        if (HasBirthChart(birthChart))
        {
            var planetary = _birthChartEvaluator.Evaluate(
                birthChart!.BirthDateTime!.Value,
                string.IsNullOrWhiteSpace(birthChart.Timezone) ? location.Timezone : birthChart.Timezone!,
                birthChart.Latitude!.Value,
                birthChart.Longitude!.Value,
                piece.Start,
                location.Latitude,
                location.Longitude,
                out var scoreDelta);
            evidence.AddRange(planetary);
            if (!hardRejected)
            {
                score = Math.Clamp(score + scoreDelta, 0, 100);
            }
        }
        else
        {
            evidence.Add(new GrahaPraveshaEvidence
            {
                RuleId = "GP_PLANETARY_NOT_SUPPLIED",
                Category = "PLANETARY",
                Input = "none",
                Result = "SKIPPED",
                Severity = "INFO",
                Message = "Planetary conditions were not evaluated because no birth chart was supplied.",
                Source = "Graha Pravesha Rule Profile"
            });
        }

        var status = GrahaPraveshaRuleEngine.ResolveStatus(profile, score, hardRejected, blocksRecommended || !outcome.KalashaEligible);
        return new GrahaPraveshaWindowDto
        {
            Date = date.ToString("yyyy-MM-dd"),
            Start = piece.Start.ToString("HH:mm"),
            End = piece.End.ToString("HH:mm"),
            StartTime = piece.Start,
            EndTime = piece.End,
            Status = status,
            Score = score,
            Lagna = snapshot.Lagna,
            Tithi = snapshot.Tithi,
            Paksha = snapshot.Paksha,
            Nakshatra = snapshot.Nakshatra,
            Yoga = snapshot.Yoga,
            Karana = snapshot.Karana,
            Vara = vara,
            MoonSign = snapshot.MoonSign,
            KalashaCount = kalashaCount,
            KalashaEligible = outcome.KalashaEligible,
            VastuTableMatched = vastu.Matched,
            Evidence = evidence.Select(MapEvidence).ToList(),
            TimingRestrictions = timing
        };
    }

    private static List<GrahaPraveshaWindowDto> Merge(IEnumerable<GrahaPraveshaWindowDto> windows)
    {
        var merged = new List<GrahaPraveshaWindowDto>();
        foreach (var window in windows.OrderBy(item => item.StartTime))
        {
            var last = merged.LastOrDefault();
            if (last is not null
                && last.EndTime == window.StartTime
                && last.Status == window.Status
                && last.Lagna == window.Lagna
                && last.Tithi == window.Tithi
                && last.Nakshatra == window.Nakshatra
                && last.Yoga == window.Yoga
                && last.Karana == window.Karana
                && last.Score == window.Score)
            {
                last.EndTime = window.EndTime;
                last.End = window.End;
                continue;
            }

            merged.Add(window);
        }

        return merged;
    }

    private static GrahaPraveshaPeriodDto MapPeriod(MuhurthaTimeWindow window)
    {
        return new GrahaPraveshaPeriodDto
        {
            RuleKey = window.RuleKey,
            Name = window.DisplayName,
            Start = window.StartTime.ToString("HH:mm"),
            End = window.EndTime.ToString("HH:mm"),
            StartTime = window.StartTime,
            EndTime = window.EndTime
        };
    }

    private static GrahaPraveshaEvidenceDto MapEvidence(GrahaPraveshaEvidence evidence)
    {
        return new GrahaPraveshaEvidenceDto
        {
            RuleId = evidence.RuleId,
            Category = evidence.Category,
            Input = evidence.Input,
            Result = evidence.Result,
            Severity = evidence.Severity,
            Message = evidence.Message,
            Source = evidence.Source,
            ScoreImpact = evidence.ScoreImpact
        };
    }

    private static bool HasBirthChart(GrahaPraveshaBirthChartDto? birthChart)
    {
        return birthChart?.BirthDateTime is not null
            && birthChart.Latitude is not null
            && birthChart.Longitude is not null;
    }

    private static void Validate(GrahaPraveshaLocationDto location, DateOnly from, DateOnly to, string calendarSystem)
    {
        if (location is null)
        {
            throw new ArgumentException("Location is required.");
        }

        if (string.IsNullOrWhiteSpace(location.Timezone))
        {
            throw new ArgumentException("Location timezone is required.");
        }

        if (to < from)
        {
            throw new ArgumentException("ToDate must be on or after FromDate.");
        }

        if (to.DayNumber - from.DayNumber > MaxDays)
        {
            throw new ArgumentException($"Date range cannot exceed {MaxDays} days.");
        }

        var system = calendarSystem?.Trim().ToUpperInvariant();
        if (system is not ("AMANTA" or "PURNIMANTA"))
        {
            throw new ArgumentException("CalendarSystem must be AMANTA or PURNIMANTA.");
        }
    }
}
