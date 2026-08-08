namespace HoraLife.PredictionEngine.Facts.PlanetAspect;

internal sealed record PlanetAspectFact
{
    public required string Planet { get; init; }

    public required bool HasBeneficAspect { get; init; }

    public required bool HasMaleficAspect { get; init; }

    public required IReadOnlyList<string> AspectedBy { get; init; }

    public required string EvidenceKey { get; init; }
}