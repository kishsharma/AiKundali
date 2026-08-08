namespace KundaliEngine.API.DTOs.Responses;

public sealed class CurrentRetrogradesResponse
{
    public DateTime CalculatedAtUtc { get; set; }

    public bool IncludeNodes { get; set; }

    public string Note { get; set; } = string.Empty;

    public List<RetrogradePlanetDto> Planets { get; set; } = new();
}

