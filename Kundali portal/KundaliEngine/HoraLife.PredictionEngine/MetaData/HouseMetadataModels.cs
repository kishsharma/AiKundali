namespace HoraLife.PredictionEngine.Metadata;

internal sealed record HouseMetadataRoot
{
    public string SchemaVersion { get; init; } = "1.0";
    public string KnowledgeVersion { get; init; } = "1.0";
    public string Tradition { get; init; } = string.Empty;

    public IReadOnlyList<HouseMetadataSource> Sources { get; init; }
        = Array.Empty<HouseMetadataSource>();

    public IReadOnlyList<HouseClassificationMetadata> ClassificationDefinitions { get; init; }
        = Array.Empty<HouseClassificationMetadata>();

    public IReadOnlyList<PurusharthaMetadata> PurusharthaDefinitions { get; init; }
        = Array.Empty<PurusharthaMetadata>();

    public IReadOnlyList<HouseSignificationMetadata> Houses { get; init; }
        = Array.Empty<HouseSignificationMetadata>();
}

internal sealed record HouseMetadataSource
{
    public required string Key { get; init; }
    public required string Name { get; init; }
}

internal sealed record HouseClassificationMetadata
{
    public required string Key { get; init; }
    public required string Name { get; init; }

    public IReadOnlyList<int> Houses { get; init; }
        = Array.Empty<int>();

    public IReadOnlyList<string> Aliases { get; init; }
        = Array.Empty<string>();

    public IReadOnlyList<string> TraditionalAssociations { get; init; }
        = Array.Empty<string>();

    public string Meaning { get; init; } = string.Empty;

    public IReadOnlyList<string> SourceRefs { get; init; }
        = Array.Empty<string>();
}

internal sealed record PurusharthaMetadata
{
    public required string Key { get; init; }

    public IReadOnlyList<int> Houses { get; init; }
        = Array.Empty<int>();

    public string Meaning { get; init; } = string.Empty;
}

internal sealed record HouseSignificationMetadata
{
    public required int HouseNumber { get; init; }

    public required string Key { get; init; }

    public required string CanonicalName { get; init; }

    public IReadOnlyList<string> Aliases { get; init; }
        = Array.Empty<string>();

    public required string EnglishName { get; init; }

    public required string Purushartha { get; init; }

    public IReadOnlyList<string> Classifications { get; init; }
        = Array.Empty<string>();

    public IReadOnlyList<SignificationMetadata> CoreSignifications { get; init; }
        = Array.Empty<SignificationMetadata>();

    public IReadOnlyList<SignificationMetadata> ExtendedSignifications { get; init; }
        = Array.Empty<SignificationMetadata>();

    public IReadOnlyList<string> SourceRefs { get; init; }
        = Array.Empty<string>();
}

internal sealed record SignificationMetadata
{
    public required string Key { get; init; }

    public required string Label { get; init; }
}