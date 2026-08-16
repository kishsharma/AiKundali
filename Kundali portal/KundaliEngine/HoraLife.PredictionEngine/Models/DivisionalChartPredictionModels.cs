namespace HoraLife.PredictionEngine.Models;

public sealed record DivisionalChartPredictionData
{
    public required string ChartType { get; init; }

    public required string AscendantSign { get; init; }

    public IReadOnlyList<DivisionalHousePredictionData> Houses { get; init; }
        = Array.Empty<DivisionalHousePredictionData>();

    public IReadOnlyList<DivisionalPlanetPredictionData> Planets { get; init; }
        = Array.Empty<DivisionalPlanetPredictionData>();
}

public sealed record DivisionalHousePredictionData
{
    public required int HouseNumber { get; init; }

    public required string Sign { get; init; }

    public required string HouseLord { get; init; }

    public IReadOnlyList<string> Occupants { get; init; }
        = Array.Empty<string>();
}

public sealed record DivisionalPlanetPredictionData
{
    public required string Planet { get; init; }

    public required string Sign { get; init; }

    public required int HouseNumber { get; init; }
}

internal sealed record DivisionalQuestionFocusMetadata
{
    public required string ChartType { get; init; }

    public IReadOnlyList<int> HouseNumbers { get; init; }
        = Array.Empty<int>();

    public IReadOnlyList<string> SignificatorPlanets { get; init; }
        = Array.Empty<string>();
}

internal sealed record DivisionalHouseFocusEvidence
{
    public required int HouseNumber { get; init; }

    public required string Sign { get; init; }

    public required string HouseLord { get; init; }

    public DivisionalPlanetPredictionData? HouseLordPlanet { get; init; }

    public IReadOnlyList<DivisionalPlanetPredictionData> Occupants { get; init; }
        = Array.Empty<DivisionalPlanetPredictionData>();

    public string HouseEvidenceKey { get; init; } = string.Empty;

    public string HouseLordEvidenceKey { get; init; } = string.Empty;

    public IReadOnlyList<string> OccupantEvidenceKeys { get; init; }
        = Array.Empty<string>();
}

internal sealed record DivisionalChartFocusEvidence
{
    public required string ChartType { get; init; }

    public IReadOnlyList<DivisionalHouseFocusEvidence> Houses { get; init; }
        = Array.Empty<DivisionalHouseFocusEvidence>();

    public IReadOnlyList<DivisionalPlanetPredictionData> NaturalSignificators { get; init; }
        = Array.Empty<DivisionalPlanetPredictionData>();

    public IReadOnlyList<string> NaturalSignificatorEvidenceKeys { get; init; }
        = Array.Empty<string>();
}
