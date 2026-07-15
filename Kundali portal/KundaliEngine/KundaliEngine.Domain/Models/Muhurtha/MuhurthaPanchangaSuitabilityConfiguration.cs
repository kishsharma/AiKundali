using KundaliEngine.Domain.Enums;

namespace KundaliEngine.Domain.Models.Muhurtha;

public class MuhurthaPanchangaSuitabilityConfiguration
{
    public MuhurthaEventType EventType { get; set; }

    public List<string> PositiveTithis { get; set; } = new();

    public List<string> AvoidTithis { get; set; } = new();

    public List<string> PositiveNakshatras { get; set; } = new();

    public List<string> AvoidNakshatras { get; set; } = new();

    public List<string> PositiveYogas { get; set; } = new();

    public List<string> AvoidYogas { get; set; } = new();

    public List<string> PositiveKaranas { get; set; } = new();

    public List<string> AvoidKaranas { get; set; } = new();
}
