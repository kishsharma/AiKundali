namespace KundaliEngine.API.Services.Muhurtha.Models;

public class VastuMuhurthaEligibilityResult
{
    public string Paksha { get; set; } = string.Empty;

    public string Tithi { get; set; } = string.Empty;

    public string Weekday { get; set; } = string.Empty;

    public string MoonSign { get; set; } = string.Empty;

    public string Applicability { get; set; } = string.Empty;

    public string Ahuti { get; set; } = string.Empty;

    public int ScoreImpact { get; set; }

    public string Source { get; set; } = string.Empty;
}
