using System.Collections.Concurrent;
using KundaliEngine.API.DTOs.Responses;
using KundaliEngine.Astrology.Services.Astrology;
using KundaliEngine.Domain.Enums;
using KundaliEngine.Domain.Models;

namespace KundaliEngine.API.Services.Horoscope;

public interface IHoroscopeRashifalService
{
    HoroscopeRashifalResponse Generate(HoroscopeHorizon horizon, DateOnly anchorDate, string locale = "en");
}

public sealed class HoroscopeRashifalService : IHoroscopeRashifalService
{
    /// <summary>Fixed location bucket so all users get identical newspaper text.</summary>
    private const double FixedLatitude = 28.6139;
    private const double FixedLongitude = 77.2090;
    private static readonly TimeSpan FixedOffset = TimeSpan.FromHours(5.5);

    private static readonly ConcurrentDictionary<string, HoroscopeRashifalResponse> Cache = new();

    private readonly ChartService _chartService;
    private readonly ILogger<HoroscopeRashifalService> _logger;

    public HoroscopeRashifalService(ChartService chartService, ILogger<HoroscopeRashifalService> logger)
    {
        _chartService = chartService;
        _logger = logger;
    }

    public HoroscopeRashifalResponse Generate(HoroscopeHorizon horizon, DateOnly anchorDate, string locale = "en")
    {
        if (anchorDate == default)
        {
            throw new ArgumentException("anchorDate is required.");
        }

        locale = NormalizeLocale(locale);
        var (from, to) = ResolvePeriod(horizon, anchorDate);
        var cacheKey = $"{horizon}|{from:yyyy-MM-dd}|{to:yyyy-MM-dd}|IN-DEL|{locale}";
        if (Cache.TryGetValue(cacheKey, out var cached))
        {
            return Clone(cached);
        }

        var analysisLocal = (horizon == HoroscopeHorizon.Weekly ? from : horizon == HoroscopeHorizon.Monthly
            ? new DateOnly(anchorDate.Year, anchorDate.Month, Math.Min(15, DateTime.DaysInMonth(anchorDate.Year, anchorDate.Month)))
            : anchorDate).ToDateTime(new TimeOnly(12, 0));
        var analysisUtc = new DateTimeOffset(
            DateTime.SpecifyKind(analysisLocal, DateTimeKind.Unspecified),
            FixedOffset).UtcDateTime;

        var transitRaw = _chartService.GenerateChart(analysisUtc, FixedLatitude, FixedLongitude);
        var weights = HoroscopeFactorMatrix.ForHorizon(horizon);

        var items = new List<HoroscopeRashifalItemDto>();
        foreach (ZodiacSign moonRashi in Enum.GetValues(typeof(ZodiacSign)))
        {
            if ((int)moonRashi < 1 || (int)moonRashi > 12)
            {
                continue;
            }

            var fromMoon = HoroscopePeriodService.RehouseFromReference(transitRaw, moonRashi);
            // Newspaper mode: no natal dasha — pass Sun as neutral placeholders (ignored for MD/AD align bonuses when planet !=).
            var gochara = HoroscopeGocharaScorer.Score(
                fromMoon,
                fromMoon,
                Planet.Sun,
                Planet.Moon,
                weights);

            // Prefer Moon-reference hits only (dedupe Lagna twin).
            var moonHits = gochara.Hits
                .Where(h => h.Reference == "Moon" || h.Reference.StartsWith("Moon", StringComparison.Ordinal))
                .GroupBy(h => $"{h.Planet}|{h.House}")
                .Select(g => g.OrderByDescending(h => ToneRank(h.Tone)).First())
                .OrderByDescending(h => ToneRank(h.Tone))
                .Take(6)
                .ToList();

            if (moonHits.Count == 0)
            {
                moonHits = gochara.Hits.Take(4).ToList();
            }

            var support = moonHits.Count(h => h.Tone == "Supportive") * 10;
            var pressure = moonHits.Count(h => h.Tone == "Challenging") * 10;
            var score = Math.Clamp(50 + support - pressure, 0, 100);
            var tone = score >= 60 ? "Supportive" : score <= 40 ? "Challenging" : "Mixed";

            var highlights = moonHits
                .Where(h => h.Tone != "Mixed")
                .Select(h => HoroscopeRashifalLocalization.LocalizeTransit(
                    HoroscopeRashifalTemplates.LineFor(h.Planet, h.House, h.Tone), locale))
                .Distinct()
                .Take(4)
                .ToList();

            var rashiName = moonRashi.ToString();
            var english = HoroscopeRashifalTemplates.EnglishNames.TryGetValue(moonRashi, out var en) ? en : rashiName;

            var areas = HoroscopeFactorMatrix.Areas.Select(area =>
            {
                var areaHits = moonHits.Where(h => area.Houses.Contains(h.House)).ToList();
                var areaSupport = areaHits.Count(h => h.Tone == "Supportive");
                var areaPressure = areaHits.Count(h => h.Tone == "Challenging");
                var areaTone = areaSupport > areaPressure
                    ? "Supportive"
                    : areaPressure > areaSupport
                        ? "Challenging"
                        : tone;
                return new HoroscopeRashifalAreaDto
                {
                    Key = area.Key,
                    Tone = areaTone,
                    Line = HoroscopeRashifalLocalization.LocalizeArea(
                        area.Key, areaTone, HoroscopeRashifalTemplates.AreaLine(area.Key, areaTone), locale)
                };
            }).ToList();

            items.Add(new HoroscopeRashifalItemDto
            {
                Rashi = rashiName,
                EnglishName = english,
                Tone = tone,
                Score = score,
                Summary = HoroscopeRashifalLocalization.BuildSummary(
                    english,
                    tone,
                    // BuildSummary localizes the original English catalog lines. Keep these separate from the
                    // localized display list so it never attempts to translate text twice.
                    moonHits.Where(h => h.Tone != "Mixed")
                        .Select(h => HoroscopeRashifalTemplates.LineFor(h.Planet, h.House, h.Tone))
                        .Distinct().Take(4).ToList(),
                    locale),
                Areas = areas,
                GocharaHighlights = highlights
            });
        }

        var response = new HoroscopeRashifalResponse
        {
            Horizon = horizon,
            From = from,
            To = to,
            Disclaimer = HoroscopeRashifalLocalization.Disclaimer(locale),
            Items = items.OrderBy(i => Enum.TryParse<ZodiacSign>(i.Rashi, out var s) ? (int)s : 99).ToList()
        };

        Cache[cacheKey] = Clone(response);
        _logger.LogInformation(
            "HoroscopeRashifalGenerated horizon={Horizon} from={From} to={To} items={Count}",
            horizon,
            from,
            to,
            response.Items.Count);

        return Clone(response);
    }

    private static int ToneRank(string tone) => tone switch
    {
        "Challenging" => 3,
        "Supportive" => 2,
        _ => 1
    };

    private static string NormalizeLocale(string? locale) => locale?.Trim().ToLowerInvariant() switch
    {
        "hi" or "hi-in" => "hi",
        "kn" or "kn-in" => "kn",
        _ => "en"
    };

    private static (DateOnly From, DateOnly To) ResolvePeriod(HoroscopeHorizon horizon, DateOnly anchor) =>
        horizon switch
        {
            HoroscopeHorizon.Weekly => (StartOfWeek(anchor), StartOfWeek(anchor).AddDays(6)),
            HoroscopeHorizon.Monthly => (new DateOnly(anchor.Year, anchor.Month, 1),
                new DateOnly(anchor.Year, anchor.Month, 1).AddMonths(1).AddDays(-1)),
            _ => (anchor, anchor)
        };

    private static DateOnly StartOfWeek(DateOnly date)
    {
        var diff = ((int)date.DayOfWeek + 6) % 7;
        return date.AddDays(-diff);
    }

    private static HoroscopeRashifalResponse Clone(HoroscopeRashifalResponse source) => new()
    {
        Horizon = source.Horizon,
        From = source.From,
        To = source.To,
        Disclaimer = source.Disclaimer,
        Items = source.Items.Select(item => new HoroscopeRashifalItemDto
        {
            Rashi = item.Rashi,
            EnglishName = item.EnglishName,
            Tone = item.Tone,
            Score = item.Score,
            Summary = item.Summary,
            GocharaHighlights = item.GocharaHighlights.ToList(),
            Areas = item.Areas.Select(a => new HoroscopeRashifalAreaDto
            {
                Key = a.Key,
                Tone = a.Tone,
                Line = a.Line
            }).ToList()
        }).ToList()
    };
}
