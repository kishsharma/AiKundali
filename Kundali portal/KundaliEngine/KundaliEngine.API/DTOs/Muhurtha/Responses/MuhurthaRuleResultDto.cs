using KundaliEngine.Domain.Enums;

namespace KundaliEngine.API.DTOs.Muhurtha.Responses;

public class MuhurthaRuleResultDto
{
    public string RuleKey { get; set; } = string.Empty;

    public MuhurthaRuleStatus Status { get; set; }

    public MuhurthaRuleSeverity Severity { get; set; }

    public int ScoreImpact { get; set; }

    public bool HardRejected { get; set; }

    public DateTimeOffset? StartTime { get; set; }

    public DateTimeOffset? EndTime { get; set; }

    public string Summary { get; set; } = string.Empty;

    public string Recommendation { get; set; } = string.Empty;

    public Dictionary<string, string> Metadata { get; set; } = new();
}
