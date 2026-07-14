using KundaliEngine.Domain.Enums;

namespace KundaliEngine.Domain.Models.Muhurtha;

public class MuhurthaVastuEligibilityRuleConfiguration
{
    public bool Enabled { get; set; } = true;

    public List<MuhurthaEventType> ApplicableEventTypes { get; set; } = new();

    public string Paksha { get; set; } = string.Empty;

    public string Tithi { get; set; } = string.Empty;

    public List<string> Weekdays { get; set; } = new();

    public List<string> MoonSigns { get; set; } = new();

    public List<string> Applicability { get; set; } = new();

    public string Ahuti { get; set; } = string.Empty;

    public int ScoreImpact { get; set; } = 10;

    public string Source { get; set; } = string.Empty;
}
