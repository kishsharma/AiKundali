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
}