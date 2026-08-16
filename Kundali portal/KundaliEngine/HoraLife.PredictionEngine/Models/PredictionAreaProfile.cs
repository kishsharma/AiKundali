using HoraLife.PredictionEngine.Enums;

namespace HoraLife.PredictionEngine.Models;

public sealed record PredictionAreaProfile
{
    public required PredictionLifeArea LifeArea { get; init; }

    public required IReadOnlyList<int> PrimaryHouses { get; init; }

    public IReadOnlyList<int> SupportingHouses { get; init; }
        = Array.Empty<int>();

    public IReadOnlyList<string> NaturalSignificators { get; init; }
        = Array.Empty<string>();

    public IReadOnlyList<PredictionCustomQuestionTimingProfile> CustomQuestionTiming { get; init; }
        = Array.Empty<PredictionCustomQuestionTimingProfile>();
}

public sealed record PredictionCustomQuestionTimingProfile
{
    public required string FocusKey { get; init; }

    public int? FutureHorizonYears { get; init; }

    public int MaxWindows { get; init; } = 5;
}