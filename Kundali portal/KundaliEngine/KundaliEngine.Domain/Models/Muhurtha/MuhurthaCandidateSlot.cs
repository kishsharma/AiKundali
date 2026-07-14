using KundaliEngine.Domain.Enums;

namespace KundaliEngine.Domain.Models.Muhurtha;

public class MuhurthaCandidateSlot
{
    public DateTimeOffset StartTime { get; set; }

    public DateTimeOffset EndTime { get; set; }

    public MuhurthaCandidateStatus Status { get; set; } = MuhurthaCandidateStatus.Pending;

    public int Score { get; set; }

    public bool HardRejected { get; set; }

    public List<MuhurthaRuleResult> RuleResults { get; set; } = new();

    public Dictionary<string, string> Metadata { get; set; } = new();
}
