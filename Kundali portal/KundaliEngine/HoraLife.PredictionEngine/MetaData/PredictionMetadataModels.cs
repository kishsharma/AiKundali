using HoraLife.PredictionEngine.Enums;

namespace HoraLife.PredictionEngine.Metadata;

internal sealed record PredictionMetadataRoot
{
    public string SchemaVersion { get; init; } = "1.0";

    public IReadOnlyList<PredictionLifeAreaMetadata> LifeAreas { get; init; }
        = Array.Empty<PredictionLifeAreaMetadata>();
}

internal sealed record PredictionLifeAreaMetadata
{
    public required PredictionLifeArea LifeArea { get; init; }

    public required IReadOnlyList<int> PrimaryHouses { get; init; }

    public IReadOnlyList<int> SupportingHouses { get; init; }
        = Array.Empty<int>();

    public IReadOnlyList<string> NaturalSignificators { get; init; }
        = Array.Empty<string>();
}