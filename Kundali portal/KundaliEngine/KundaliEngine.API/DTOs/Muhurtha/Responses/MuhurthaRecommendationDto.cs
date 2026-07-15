using KundaliEngine.Domain.Enums;

namespace KundaliEngine.API.DTOs.Muhurtha.Responses;

public class MuhurthaRecommendationDto
{
    public string SlotId { get; set; } = string.Empty;

    public DateTimeOffset StartTime { get; set; }

    public DateTimeOffset EndTime { get; set; }

    public int Score { get; set; }

    public MuhurthaVerdict Verdict { get; set; }

    public bool HardRejected { get; set; }

    public List<string> Strengths { get; set; } = new();

    public List<string> Warnings { get; set; } = new();

    public List<MuhurthaRuleResultDto> RuleResults { get; set; } = new();

    public MuhurthaPanchangTimingsDto PanchangTimings { get; set; } = new();

    public MuhurthaScoreBreakdownDto ScoreBreakdown { get; set; } = new();
}



