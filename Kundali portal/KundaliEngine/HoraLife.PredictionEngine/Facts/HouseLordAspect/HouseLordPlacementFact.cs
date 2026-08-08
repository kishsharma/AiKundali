namespace HoraLife.PredictionEngine.Facts.HouseLordAspect;

internal sealed record HouseLordPlacementFact
{
    public required int SourceHouse { get; init; }

    public required string LordPlanet { get; init; }

    public required int TargetHouse { get; init; }

    public required string EvidenceKey { get; init; }
}