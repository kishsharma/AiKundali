namespace HoraLife.PredictionEngine.Models;

public sealed record HousePredictionData
{
    public required int HouseNumber { get; init; }

    public required string Sign { get; init; }

    public required string HouseLord { get; init; }

    public IReadOnlyList<string> Occupants { get; init; }
        = Array.Empty<string>();

    public IReadOnlyList<string> AspectedBy { get; init; }
        = Array.Empty<string>();

    public bool HasBeneficAspect { get; init; }

    public bool HasMaleficAspect { get; init; }

    public int? StrengthScore { get; init; }

    public string StrengthLevel { get; init; } = string.Empty;

    public IReadOnlyList<string> DominantThemes { get; init; }
        = Array.Empty<string>();

    public int? BavPoints { get; init; }

    public int? SavPoints { get; init; }
}

public sealed record PlanetPredictionData
{
    public required string Planet { get; init; }

    public required string Sign { get; init; }

    public required int HouseNumber { get; init; }

    public required double Longitude { get; init; }

    public int Degree { get; init; }

    public int Minute { get; init; }

    public string Nakshatra { get; init; } = string.Empty;

    public int Pada { get; init; }

    public bool IsCombust { get; init; }

    public bool IsRetrograde { get; init; }

    public IReadOnlyList<string> AspectedBy { get; init; }
        = Array.Empty<string>();

    public bool HasBeneficAspect { get; init; }

    public bool HasMaleficAspect { get; init; }

    public int? StrengthScore { get; init; }

    public string StrengthLevel { get; init; } = string.Empty;

    public string DetailedStrength { get; init; } = string.Empty;

    public string ConditionTone { get; init; } = string.Empty;

    public string FunctionalNature { get; init; } = string.Empty;

    public PlanetBalaPredictionData Bala { get; init; } = new();
}

public sealed record PlanetBalaPredictionData
{
    public string SthanaBala { get; init; } = string.Empty;

    public string DigBala { get; init; } = string.Empty;

    public string KalaBala { get; init; } = string.Empty;

    public string CheshtaBala { get; init; } = string.Empty;

    public string NaisargikaBala { get; init; } = string.Empty;

    public string DrikBala { get; init; } = string.Empty;
}