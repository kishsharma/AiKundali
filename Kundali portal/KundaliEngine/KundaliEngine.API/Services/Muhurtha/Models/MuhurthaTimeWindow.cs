using KundaliEngine.Domain.Enums;

namespace KundaliEngine.API.Services.Muhurtha.Models;

public class MuhurthaTimeWindow
{
    public string RuleKey { get; set; } = string.Empty;

    public string DisplayName { get; set; } = string.Empty;

    public DateTimeOffset StartTime { get; set; }

    public DateTimeOffset EndTime { get; set; }

    public MuhurthaRuleSeverity DefaultSeverity { get; set; } = MuhurthaRuleSeverity.Neutral;

    public int DefaultScoreImpact { get; set; }

    public bool IsPositive { get; set; }

    public string Summary { get; set; } = string.Empty;

    public string Recommendation { get; set; } = string.Empty;

    public Dictionary<string, string> Metadata { get; set; } = new();
}
