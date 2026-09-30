using HoraLife.PredictionEngine.Enums;

namespace HoraLife.PredictionEngine.Services;

public sealed record LifeQuestionFocus
{
    public required string FocusKey { get; init; }

    public required PredictionLifeArea LifeArea { get; init; }

    public required bool IsTiming { get; init; }

    public required int[] PrimaryHouses { get; init; }

    public required string[] Significators { get; init; }

    public required string ReasonPrefix { get; init; }

    public string? DivisionalChart { get; init; }

    public int[] DivisionalHouses { get; init; } = [];

    public string[] DivisionalSignificators { get; init; } = [];

    public int? MinimumAgeYears { get; init; }

    public int LaterAgeYears { get; init; } = 32;
}

public static class LifeQuestionCatalog
{
    private static readonly LifeQuestionFocus[] Questions =
    [
        Focus("marriage_possibility", PredictionLifeArea.Relationships, false, [7], ["Venus", "Jupiter"], "relationships", "D9", [7], ["Venus"], 18),
        Focus("marriage_timing", PredictionLifeArea.Relationships, true, [7], ["Venus", "Jupiter"], "relationships", "D9", [7], ["Venus"], 18),
        Focus("career_possibility", PredictionLifeArea.Career, false, [10], ["Sun", "Saturn", "Mercury"], "career", "D10", [10], ["Sun", "Saturn"]),
        Focus("career_timing", PredictionLifeArea.Career, true, [10], ["Sun", "Saturn", "Mercury"], "career", "D10", [10], ["Sun", "Saturn"]),
        Focus("income_possibility", PredictionLifeArea.Finance, false, [11], ["Jupiter", "Venus", "Mercury"], "finance"),
        Focus("income_timing", PredictionLifeArea.Finance, true, [11], ["Jupiter", "Venus", "Mercury"], "finance"),
        Focus("savings_possibility", PredictionLifeArea.Finance, false, [2], ["Jupiter", "Venus", "Mercury"], "finance"),
        Focus("savings_timing", PredictionLifeArea.Finance, true, [2], ["Jupiter", "Venus", "Mercury"], "finance"),
        Focus("property_possibility", PredictionLifeArea.Property, false, [4], ["Mars", "Moon"], "property", "D4", [4], ["Mars", "Moon"]),
        Focus("property_timing", PredictionLifeArea.Property, true, [4], ["Mars", "Moon"], "property", "D4", [4], ["Mars", "Moon"]),
        Focus("education_possibility", PredictionLifeArea.Education, false, [5, 4], ["Mercury", "Jupiter"], "education"),
        Focus("education_timing", PredictionLifeArea.Education, true, [5, 4], ["Mercury", "Jupiter"], "education"),
        Focus("children_possibility", PredictionLifeArea.Education, false, [5], ["Jupiter"], "education", "D7", [5], ["Jupiter"]),
        Focus("children_timing", PredictionLifeArea.Education, true, [5], ["Jupiter"], "education", "D7", [5], ["Jupiter"]),
        Focus("health_possibility", PredictionLifeArea.Health, false, [1], ["Sun", "Moon"], "health"),
        Focus("health_timing", PredictionLifeArea.Health, true, [1], ["Sun", "Moon"], "health"),
        Focus("family_possibility", PredictionLifeArea.Family, false, [4, 2], ["Moon", "Jupiter"], "family"),
        Focus("family_timing", PredictionLifeArea.Family, true, [4, 2], ["Moon", "Jupiter"], "family")
    ];

    public static LifeQuestionFocus? Find(PredictionLifeArea lifeArea, string? focusKey)
    {
        if (string.IsNullOrWhiteSpace(focusKey))
        {
            return null;
        }

        return Questions.FirstOrDefault(question =>
            question.LifeArea == lifeArea &&
            question.FocusKey.Equals(focusKey.Trim(), StringComparison.OrdinalIgnoreCase));
    }

    public static bool IsSupported(PredictionLifeArea lifeArea, string? focusKey)
    {
        return Find(lifeArea, focusKey) is not null;
    }

    private static LifeQuestionFocus Focus(
        string focusKey,
        PredictionLifeArea lifeArea,
        bool isTiming,
        int[] primaryHouses,
        string[] significators,
        string reasonPrefix,
        string? divisionalChart = null,
        int[]? divisionalHouses = null,
        string[]? divisionalSignificators = null,
        int? minimumAgeYears = null)
    {
        return new LifeQuestionFocus
        {
            FocusKey = focusKey,
            LifeArea = lifeArea,
            IsTiming = isTiming,
            PrimaryHouses = primaryHouses,
            Significators = significators,
            ReasonPrefix = reasonPrefix,
            DivisionalChart = divisionalChart,
            DivisionalHouses = divisionalHouses ?? [],
            DivisionalSignificators = divisionalSignificators ?? [],
            MinimumAgeYears = minimumAgeYears
        };
    }
}
