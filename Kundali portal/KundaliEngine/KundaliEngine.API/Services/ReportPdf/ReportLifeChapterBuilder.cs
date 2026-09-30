using System.Globalization;
using System.Text.Json;
using HoraLife.PredictionEngine.Enums;
using HoraLife.PredictionEngine.Models;
using HoraLife.PredictionEngine.Services;
using KundaliEngine.API.DTOs.Responses;
using KundaliEngine.API.Prediction.Mappers;
using KundaliEngine.API.Services.ReportPdf.Html;

namespace KundaliEngine.API.Services.ReportPdf;

public sealed class ReportLifeChapterBuilder
{
    private readonly PredictionEngineService _engine;
    private readonly PredictionContextMapper _mapper;

    private static readonly ChapterSpec[] Chapters =
    [
        new("Career", PredictionLifeArea.Career, "career_possibility", "career_timing", null),
        new("Income", PredictionLifeArea.Finance, "income_possibility", "income_timing", null),
        new("Savings", PredictionLifeArea.Finance, "savings_possibility", "savings_timing", null),
        new("Property", PredictionLifeArea.Property, "property_possibility", "property_timing", null),
        new("Study", PredictionLifeArea.Education, "education_possibility", "education_timing", null),
        new("Marriage", PredictionLifeArea.Relationships, "marriage_possibility", "marriage_timing", null),
        new("Children", PredictionLifeArea.Education, "children_possibility", "children_timing", "This is a chart indication, not a medical result."),
        new("Family", PredictionLifeArea.Family, "family_possibility", "family_timing", null),
        new("Vitality", PredictionLifeArea.Health, "health_possibility", "health_timing", "This describes chart pressure, not a diagnosis.")
    ];

    public ReportLifeChapterBuilder(PredictionEngineService engine, PredictionContextMapper mapper)
    {
        _engine = engine;
        _mapper = mapper;
    }

    public IReadOnlyList<ReportLifeChapter> Build(JsonElement birthProfile, GenerateKundaliResponse report)
    {
        var context = _mapper.Map(report);
        var referenceDate = DateTimeOffset.UtcNow;
        var birthDate = ReadBirthDate(birthProfile);
        var chapters = new List<ReportLifeChapter>();

        foreach (var spec in Chapters)
        {
            var possibility = Ask(spec.LifeArea, spec.PossibilityKey, PredictionTimeScope.Current, referenceDate, birthDate, context);
            var timing = Ask(spec.LifeArea, spec.TimingKey, PredictionTimeScope.Future, referenceDate, birthDate, context);
            if (possibility?.Answer is null)
            {
                continue;
            }

            chapters.Add(new ReportLifeChapter
            {
                Title = spec.Title,
                Tone = ToneLabel(possibility.Answer.Outcome),
                Summary = BuildSummary(spec, possibility.Answer, timing?.Answer),
                Window = FormatWindow(timing?.Answer, referenceDate)
            });
        }

        return chapters;
    }

    private PredictionResult? Ask(
        PredictionLifeArea lifeArea,
        string focusKey,
        PredictionTimeScope timeScope,
        DateTimeOffset referenceDate,
        DateTimeOffset? birthDate,
        PredictionContext context)
    {
        try
        {
            return _engine.Generate(new PredictionRequest
            {
                Mode = PredictionMode.CustomQuestion,
                LifeArea = lifeArea,
                FocusKey = focusKey,
                TimeScope = timeScope,
                ReferenceDate = referenceDate,
                BirthDate = birthDate
            }, context);
        }
        catch (NotSupportedException)
        {
            return null;
        }
    }

    private static string BuildSummary(ChapterSpec spec, PredictionAnswer possibility, PredictionAnswer? timing)
    {
        var hasPressure = possibility.ReasonKeys.Any(key =>
            key.Contains("weak", StringComparison.OrdinalIgnoreCase) ||
            key.Contains("malefic", StringComparison.OrdinalIgnoreCase) ||
            key.Contains("challenging", StringComparison.OrdinalIgnoreCase));

        var reading = possibility.Outcome switch
        {
            "Supported" => hasPressure
                ? $"{spec.Title} looks supportive overall, with a stable base and some manageable pressure."
                : $"{spec.Title} looks supportive overall, with the core foundation holding well.",
            "Challenging" => $"{spec.Title} shows more pressure than support in the core foundation right now.",
            _ => $"{spec.Title} shows a mixed pattern of support and pressure."
        };

        var ageNote = string.Empty;
        if (timing?.ReasonKeys.Any(key => key.EndsWith(".age.before_ready", StringComparison.Ordinal)) == true)
        {
            ageNote = "Timing starts at the 18th birthday. ";
        }
        else if (timing?.ReasonKeys.Any(key => key.EndsWith(".age.later_years", StringComparison.Ordinal)) == true)
        {
            ageNote = "Timing includes later-year periods for this age. ";
        }

        var caveat = string.IsNullOrWhiteSpace(spec.Caveat) ? string.Empty : " " + spec.Caveat;
        return ageNote + reading + caveat;
    }

    private static string? FormatWindow(PredictionAnswer? timing, DateTimeOffset referenceDate)
    {
        var window = timing?.TimingWindows
            .Where(item => item.EndDate > item.StartDate && item.StartDate >= referenceDate)
            .OrderBy(item => item.StartDate)
            .FirstOrDefault();

        if (window is null)
        {
            return null;
        }

        return $"{window.StartDate.ToString("dd MMM yyyy", CultureInfo.InvariantCulture)} - {window.EndDate.ToString("dd MMM yyyy", CultureInfo.InvariantCulture)}";
    }

    private static string ToneLabel(string outcome)
    {
        return outcome switch
        {
            "Supported" => "Supported",
            "Challenging" => "Needs care",
            _ => "Mixed"
        };
    }

    private static DateTimeOffset? ReadBirthDate(JsonElement birthProfile)
    {
        if (birthProfile.ValueKind != JsonValueKind.Object)
        {
            return null;
        }

        foreach (var name in new[] { "birthDate", "dateOfBirth", "BirthDate", "DateOfBirth" })
        {
            if (!birthProfile.TryGetProperty(name, out var value) || value.ValueKind != JsonValueKind.String)
            {
                continue;
            }

            var text = value.GetString();
            if (DateTimeOffset.TryParse(text, CultureInfo.InvariantCulture, DateTimeStyles.AssumeUniversal, out var parsed))
            {
                return parsed;
            }
        }

        return null;
    }

    private sealed record ChapterSpec(
        string Title,
        PredictionLifeArea LifeArea,
        string PossibilityKey,
        string TimingKey,
        string? Caveat);
}
