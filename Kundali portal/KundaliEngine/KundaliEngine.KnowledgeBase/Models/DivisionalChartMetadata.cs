namespace KundaliEngine.KnowledgeBase.Models;

public sealed class DivisionalChartMetadataDocument
{
    public string SchemaVersion { get; set; } = string.Empty;

    public string SchemaName { get; set; } = string.Empty;

    public List<DivisionalChartDefinition> DivisionalCharts { get; set; } = new();

    public Dictionary<string, PredictionAreaDefinition> PredictionAreas { get; set; } = new(StringComparer.OrdinalIgnoreCase);
}

public sealed class DivisionalChartDefinition
{
    public string Code { get; set; } = string.Empty;

    public string Name { get; set; } = string.Empty;

    public string? SanskritName { get; set; }

    public int Division { get; set; }

    public string? Group { get; set; }

    public string? Priority { get; set; }

    public List<string> PrimaryDomains { get; set; } = new();

    public string? PredictionRole { get; set; }

    public List<string> CrossValidation { get; set; } = new();

    public string? BirthTimeRequirement { get; set; }
}

public sealed class PredictionAreaDefinition
{
    public List<string> PrimaryCharts { get; set; } = new();

    public List<string> SupportingCharts { get; set; } = new();

    public List<string> Timing { get; set; } = new();

    public List<int> RequiredHouses { get; set; } = new();

    public List<string> RequiredPlanets { get; set; } = new();

    public string? Note { get; set; }
}
