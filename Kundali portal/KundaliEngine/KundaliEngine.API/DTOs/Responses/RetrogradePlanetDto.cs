namespace KundaliEngine.API.DTOs.Responses;

public sealed class RetrogradePlanetDto
{
    public string Planet { get; set; } = string.Empty;

    public string CurrentSign { get; set; } = string.Empty;

    public string RetrogradeFromSign { get; set; } = string.Empty;

    public string RetrogradeTowardSign { get; set; } = string.Empty;

    public int Degree { get; set; }

    public int Minute { get; set; }

    public double DegreeInSign { get; set; }

    public double Longitude { get; set; }

    public bool IsNode { get; set; }

    public string Motion { get; set; } = "Retrograde";

    public string Information { get; set; } = string.Empty;
}

