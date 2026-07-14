using KundaliEngine.Domain.Enums;

namespace KundaliEngine.Domain.Models.Muhurtha;

public class MuhurthaRuleResult
{
    public string RuleKey { get; set; } = string.Empty;

    public MuhurthaRuleStatus Status { get; set; } = MuhurthaRuleStatus.NotEvaluated;

    public MuhurthaRuleSeverity Severity { get; set; } = MuhurthaRuleSeverity.Neutral;

    public int ScoreImpact { get; set; }

    public bool HardRejected { get; set; }

    public DateTimeOffset? StartTime { get; set; }

    public DateTimeOffset? EndTime { get; set; }

    public string Summary { get; set; } = string.Empty;

    public string Recommendation { get; set; } = string.Empty;

    public Dictionary<string, string> Metadata { get; set; } = new();
}
