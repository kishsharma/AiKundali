namespace HoraLife.PredictionEngine.Facts.HouseAspect;

internal sealed record HouseAspectFact
{
    public required int HouseNumber { get; init; }

    public required bool HasBeneficAspect { get; init; }

    public required bool HasMaleficAspect { get; init; }

    public required IReadOnlyList<string> AspectedBy { get; init; }

    public required string EvidenceKey { get; init; }
}