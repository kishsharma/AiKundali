using KundaliEngine.Domain.Enums;

namespace KundaliEngine.Domain.Models.Muhurtha;

public class MuhurthaRecommendation
{
    public string SlotId { get; set; } = string.Empty;

    public DateTimeOffset StartTime { get; set; }

    public DateTimeOffset EndTime { get; set; }

    public int Score { get; set; }

    public MuhurthaVerdict Verdict { get; set; }

    public bool HardRejected { get; set; }

    public List<string> Strengths { get; set; } = new();

    public List<string> Warnings { get; set; } = new();

    public List<MuhurthaRuleResult> RuleResults { get; set; } = new();

    public MuhurthaPanchangTimings PanchangTimings { get; set; } = new();

    public MuhurthaScoreBreakdown ScoreBreakdown { get; set; } = new();
}



