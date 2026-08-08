using HoraLife.PredictionEngine.Facts.HouseLordAspect;
using HoraLife.PredictionEngine.Metadata;
using HoraLife.PredictionEngine.Enums;


namespace HoraLife.PredictionEngine.Interpretation;

internal sealed record HouseLordPlacementInterpretation
{
    public required int SourceHouse { get; init; }

    public required string LordPlanet { get; init; }

    public required int TargetHouse { get; init; }

    public required IReadOnlyList<string> SourceSignificationKeys { get; init; }

    public required IReadOnlyList<string> TargetSignificationKeys { get; init; }

    public required string EvidenceKey { get; init; }
}

internal sealed class HouseLordPlacementInterpreterService
{
    private readonly PredictionMetadataProvider _metadataProvider;

    public HouseLordPlacementInterpreterService(PredictionMetadataProvider metadataProvider)
    {
        _metadataProvider = metadataProvider;
    }

    public HouseLordPlacementInterpretation Interpret( HouseLordPlacementFact fact)
    {
        ArgumentNullException.ThrowIfNull(fact);

        var sourceHouse = _metadataProvider.GetHouse(fact.SourceHouse);
        var targetHouse = _metadataProvider.GetHouse(fact.TargetHouse);

        return new HouseLordPlacementInterpretation
        {
            SourceHouse = fact.SourceHouse,
            LordPlanet = fact.LordPlanet,
            TargetHouse = fact.TargetHouse,

            SourceSignificationKeys = sourceHouse.CoreSignifications
                .Select(x => x.Key)
                .ToArray(),

            TargetSignificationKeys = targetHouse.CoreSignifications
                .Select(x => x.Key)
                .ToArray(),

            EvidenceKey = fact.EvidenceKey
        };
    }

    public IReadOnlyList<HouseLordPlacementInterpretation> InterpretForLifeArea(PredictionLifeArea lifeArea, IReadOnlyList<HouseLordPlacementFact> facts)
    {
        ArgumentNullException.ThrowIfNull(facts);

        var lifeAreaMetadata = _metadataProvider.GetLifeArea(lifeArea);

        var primaryHouses = lifeAreaMetadata.PrimaryHouses.ToHashSet();

        return facts
            .Where(fact => primaryHouses.Contains(fact.SourceHouse))
            .Select(Interpret)
            .ToArray();
    }
}
