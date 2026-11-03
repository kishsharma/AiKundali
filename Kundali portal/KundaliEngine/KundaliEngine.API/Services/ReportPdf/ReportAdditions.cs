using System.Globalization;
using System.Text.Json;
using HoraLife.PredictionEngine.Enums;
using HoraLife.PredictionEngine.Models;
using HoraLife.PredictionEngine.Services;
using KundaliEngine.API.DTOs.Responses;
using KundaliEngine.API.Prediction.DTOs;
using KundaliEngine.API.Prediction.Mappers;
using KundaliEngine.API.Services.ReportPdf.Html;

namespace KundaliEngine.API.Services.ReportPdf;

public sealed class ReportPersonalityProfile
{
    public string CorePersonality { get; init; } = string.Empty;
    public string EmotionalPattern { get; init; } = string.Empty;
    public IReadOnlyList<string> NaturalStrengths { get; init; } = Array.Empty<string>();
    public IReadOnlyList<string> AwarenessAreas { get; init; } = Array.Empty<string>();
    public string PressureResponse { get; init; } = string.Empty;
    public string GrowthGuidance { get; init; } = string.Empty;
}

public sealed class ReportPdfAdditions
{
    public ReportPersonalityProfile? Personality { get; init; }
    public IReadOnlyList<ReportLifeAreaReading> LifeAreaReadings { get; init; } = Array.Empty<ReportLifeAreaReading>();
    public IReadOnlyList<ReportTwoYearForecast> TwoYearForecast { get; init; } = Array.Empty<ReportTwoYearForecast>();
}

public sealed class ReportLifeAreaReading
{
    public string LifeArea { get; init; } = string.Empty;
    public string Tone { get; init; } = string.Empty;
    public string Summary { get; init; } = string.Empty;
    public IReadOnlyList<string> Evidence { get; init; } = Array.Empty<string>();
    public IReadOnlyList<string> Windows { get; init; } = Array.Empty<string>();
}

public sealed class ReportTwoYearForecast
{
    public string LifeArea { get; init; } = string.Empty;
    public string Tone { get; init; } = string.Empty;
    public string Summary { get; init; } = string.Empty;
    public IReadOnlyList<string> Windows { get; init; } = Array.Empty<string>();
}

public sealed class ReportAdditionsBuilder
{
    private static readonly string[] StrengthOrder = ["leadership", "initiative", "analysis", "communication", "stability", "discipline", "empathy", "creativity", "responsibility"];

    private readonly PredictionEngineService _engine;
    private readonly PredictionContextMapper _mapper;

    public ReportAdditionsBuilder(PredictionEngineService engine, PredictionContextMapper mapper)
    {
        _engine = engine;
        _mapper = mapper;
    }

    public ReportPersonalityProfile BuildPersonality(GenerateKundaliResponse report)
    {
        var summary = report.Summary;
        var planets = report.Planets ?? new List<PlanetDto>();
        var ascendant = summary?.Ascendant ?? string.Empty;
        var moon = FindPlanet(planets, "Moon");
        var sun = FindPlanet(planets, "Sun");
        var mars = FindPlanet(planets, "Mars");
        var mercury = FindPlanet(planets, "Mercury");
        var saturn = FindPlanet(planets, "Saturn");
        var venus = FindPlanet(planets, "Venus");

        var fire = ContainsAny(ascendant, "Aries", "Leo", "Sagittarius");
        var earth = ContainsAny(ascendant, "Taurus", "Virgo", "Capricorn");
        var waterMoon = ContainsAny(summary?.MoonSign, "Cancer", "Scorpio", "Pisces") || ContainsAny(moon?.Sign, "Cancer", "Scorpio", "Pisces");
        var airMoon = ContainsAny(summary?.MoonSign, "Gemini", "Libra", "Aquarius") || ContainsAny(moon?.Sign, "Gemini", "Libra", "Aquarius");
        var strongSun = IsStrong(sun);
        var strongMars = IsStrong(mars) || mars?.House == 1;
        var strongMercury = IsStrong(mercury);
        var strongSaturn = IsStrong(saturn);
        var strongVenus = IsStrong(venus);
        var responsibility = strongSaturn || HasHouse(report, 10) || HasHouse(report, 6);

        var scores = new Dictionary<string, int>(StringComparer.OrdinalIgnoreCase)
        {
            ["leadership"] = (fire ? 2 : 0) + (strongSun ? 3 : 0),
            ["initiative"] = (fire ? 2 : 0) + (strongMars ? 3 : 0),
            ["analysis"] = (earth ? 2 : 0) + (strongMercury ? 3 : 0),
            ["communication"] = (airMoon ? 2 : 0) + (strongMercury ? 2 : 0),
            ["stability"] = (earth ? 3 : 0) + (strongSaturn ? 2 : 0),
            ["discipline"] = strongSaturn ? 3 : 0,
            ["empathy"] = waterMoon ? 3 : 0,
            ["creativity"] = strongVenus ? 3 : 0,
            ["responsibility"] = responsibility ? 3 : 0
        };

        var top = scores.Where(item => item.Value > 0)
            .OrderByDescending(item => item.Value)
            .ThenBy(item => Array.IndexOf(StrengthOrder, item.Key))
            .Select(item => item.Key)
            .ToArray();
        var primary = top.FirstOrDefault() ?? "balance";
        var secondary = top.Skip(1).FirstOrDefault() ?? primary;
        var sensitive = waterMoon || ContainsAny(summary?.MoonSign, "Cancer", "Scorpio", "Pisces");

        return new ReportPersonalityProfile
        {
            CorePersonality = CoreText(primary, secondary),
            EmotionalPattern = sensitive
                ? "The emotional nature appears sensitive and protective. Feelings may be strong, although they may not always be expressed immediately."
                : airMoon
                    ? "The emotional nature may process experiences through thought, conversation, and the search for perspective."
                    : "The emotional nature appears steady and measured, with a preference for practical ways to restore balance.",
            NaturalStrengths = BuildStrengths(top),
            AwarenessAreas = BuildAwareness(scores, sensitive),
            PressureResponse = strongMars
                ? "Under pressure, the person may respond by taking action quickly. If progress is blocked, frustration may increase before a clearer plan is formed."
                : strongSaturn
                    ? "Under pressure, the person may become quiet and focus on carrying the responsibility alone. A clear plan and regular recovery time can help."
                    : "Under pressure, the person may need time to understand the situation before responding. Calm communication can help restore balance.",
            GrowthGuidance = BuildGrowth(primary, secondary, sensitive)
        };
    }

    public IReadOnlyList<ReportLifeAreaReading> BuildLifeAreaReadings(GenerateKundaliResponse report)
    {
        return report.Predictions
            .Where(item => !string.IsNullOrWhiteSpace(item.LifeArea))
            .Select(item => new ReportLifeAreaReading
            {
                LifeArea = item.LifeArea,
                Tone = item.Synthesis?.OverallTone ?? item.Claims.FirstOrDefault()?.Tone ?? "Mixed",
                Summary = BuildPredictionSummary(item),
                Evidence = item.Claims.SelectMany(claim => claim.EvidenceKeys).Where(key => !string.IsNullOrWhiteSpace(key)).Distinct(StringComparer.OrdinalIgnoreCase).Take(5).ToArray(),
                Windows = item.Claims.Where(claim => claim.StartDate.HasValue && claim.EndDate.HasValue)
                    .Select(claim => FormatWindow(claim.StartDate!.Value, claim.EndDate!.Value))
                    .Distinct()
                    .Take(3)
                    .ToArray()
            })
            .ToArray();
    }

    public IReadOnlyList<ReportTwoYearForecast> BuildTwoYearForecast(JsonElement birthProfile, GenerateKundaliResponse report)
    {
        var context = _mapper.Map(report);
        var referenceDate = DateTimeOffset.UtcNow;
        var endDate = referenceDate.AddYears(2);
        var birthDate = ReadBirthDate(birthProfile);
        var specs = new[]
        {
            (PredictionLifeArea.Career, "career_timing"),
            (PredictionLifeArea.Finance, "income_timing"),
            (PredictionLifeArea.Relationships, "marriage_timing"),
            (PredictionLifeArea.Education, "education_timing"),
            (PredictionLifeArea.Family, "family_timing"),
            (PredictionLifeArea.Property, "property_timing"),
            (PredictionLifeArea.Health, "health_timing")
        };

        var result = new List<ReportTwoYearForecast>();
        foreach (var (lifeArea, focusKey) in specs)
        {
            PredictionResult? prediction;
            try
            {
                prediction = _engine.Generate(new PredictionRequest
                {
                    Mode = PredictionMode.CustomQuestion,
                    LifeArea = lifeArea,
                    FocusKey = focusKey,
                    TimeScope = PredictionTimeScope.Future,
                    ReferenceDate = referenceDate,
                    BirthDate = birthDate
                }, context);
            }
            catch (NotSupportedException)
            {
                continue;
            }

            var windows = prediction.Answer?.TimingWindows
                .Where(window => window.EndDate > referenceDate && window.StartDate < endDate)
                .Select(window => FormatWindow(
                    window.StartDate < referenceDate ? referenceDate : window.StartDate,
                    window.EndDate > endDate ? endDate : window.EndDate,
                    window.Strength))
                .Distinct()
                .Take(3)
                .ToArray() ?? Array.Empty<string>();
            if (windows.Length == 0)
            {
                continue;
            }

            result.Add(new ReportTwoYearForecast
            {
                LifeArea = lifeArea.ToString(),
                Tone = prediction.Answer?.Outcome ?? "Mixed",
                Summary = $"{lifeArea} has future timing evidence within the next two years.",
                Windows = windows
            });
        }

        return result;
    }

    private static string BuildPredictionSummary(PredictionResponseDto item)
    {
        var tone = item.Synthesis?.OverallTone ?? item.Claims.FirstOrDefault()?.Tone ?? "Mixed";
        return tone switch
        {
            "Supportive" => $"{item.LifeArea} shows supportive indicators in the current report evidence.",
            "Challenging" => $"{item.LifeArea} shows areas requiring care in the current report evidence.",
            _ => $"{item.LifeArea} shows a mixed pattern of support and attention in the current report evidence."
        };
    }

    private static IReadOnlyList<string> BuildStrengths(IReadOnlyList<string> traits)
    {
        var map = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase)
        {
            ["leadership"] = "Taking responsibility and providing direction",
            ["initiative"] = "Acting decisively when action is needed",
            ["analysis"] = "Examining details and solving practical problems",
            ["communication"] = "Explaining ideas and adapting through conversation",
            ["stability"] = "Building steady and dependable results",
            ["discipline"] = "Remaining committed during demanding periods",
            ["empathy"] = "Understanding and supporting other people",
            ["creativity"] = "Using creativity and relationship awareness constructively",
            ["responsibility"] = "Following through on important duties"
        };
        return traits.Take(4).Select(trait => map[trait]).ToArray();
    }

    private static IReadOnlyList<string> BuildAwareness(IReadOnlyDictionary<string, int> scores, bool sensitive)
    {
        var areas = new List<string>();
        if (scores.GetValueOrDefault("initiative") >= 3) areas.Add("Reacting quickly when progress is blocked");
        if (scores.GetValueOrDefault("responsibility") >= 3) areas.Add("Taking on more responsibility than necessary");
        if (scores.GetValueOrDefault("analysis") >= 3) areas.Add("Waiting for perfect certainty before acting");
        if (sensitive) areas.Add("Absorbing surrounding stress or keeping feelings private");
        return areas.Take(3).ToArray();
    }

    private static string CoreText(string primary, string secondary) =>
        $"The chart emphasizes {TraitLabel(primary)} and {TraitLabel(secondary)}. The person may naturally approach important situations through these qualities.";

    private static string BuildGrowth(string primary, string secondary, bool sensitive) =>
        sensitive
            ? "Use the chart's strengths with emotional awareness: communicate needs earlier, keep practical priorities clear, and allow trusted people to provide support."
            : $"Develop {TraitLabel(primary)} while balancing it with {TraitLabel(secondary)}. Small, consistent actions can turn these qualities into sustainable progress.";

    private static string TraitLabel(string trait) => trait switch
    {
        "leadership" => "leadership",
        "initiative" => "initiative",
        "analysis" => "careful analysis",
        "communication" => "communication",
        "stability" => "stability",
        "discipline" => "discipline",
        "empathy" => "empathy",
        "creativity" => "creativity",
        "responsibility" => "responsibility",
        _ => "balance"
    };

    private static PlanetDto? FindPlanet(IEnumerable<PlanetDto> planets, string name) =>
        planets.FirstOrDefault(planet => planet.Planet.Equals(name, StringComparison.OrdinalIgnoreCase));

    private static bool IsStrong(PlanetDto? planet) =>
        planet is not null && (planet.DetailedStrength.Contains("Strong", StringComparison.OrdinalIgnoreCase)
            || planet.DetailedStrength.Contains("Exalted", StringComparison.OrdinalIgnoreCase)
            || planet.DetailedStrength.Contains("Own", StringComparison.OrdinalIgnoreCase)
            || planet.ConditionTone.Equals("Supportive", StringComparison.OrdinalIgnoreCase));

    private static bool HasHouse(GenerateKundaliResponse report, int number) => report.Houses?.Any(house => house.HouseNumber == number && house.Score >= 60) == true;

    private static bool ContainsAny(string? value, params string[] values) =>
        !string.IsNullOrWhiteSpace(value) && values.Any(item => value.Contains(item, StringComparison.OrdinalIgnoreCase));

    private static string FormatWindow(DateTimeOffset start, DateTimeOffset end, string? strength = null) =>
        $"{start:dd MMM yyyy} - {end:dd MMM yyyy}{(string.IsNullOrWhiteSpace(strength) ? string.Empty : $" ({strength})")}";

    private static DateTimeOffset? ReadBirthDate(JsonElement birthProfile)
    {
        if (birthProfile.ValueKind != JsonValueKind.Object) return null;
        foreach (var name in new[] { "birthDate", "dateOfBirth", "BirthDate", "DateOfBirth" })
        {
            if (birthProfile.TryGetProperty(name, out var value)
                && value.ValueKind == JsonValueKind.String
                && DateTimeOffset.TryParse(value.GetString(), CultureInfo.InvariantCulture, DateTimeStyles.AssumeUniversal, out var parsed))
            {
                return parsed;
            }
        }
        return null;
    }
}
