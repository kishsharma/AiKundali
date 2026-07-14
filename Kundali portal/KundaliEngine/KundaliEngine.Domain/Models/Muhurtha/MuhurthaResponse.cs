using KundaliEngine.Domain.Enums;

namespace KundaliEngine.Domain.Models.Muhurtha;

public class MuhurthaResponse
{
    public MuhurthaEventType EventType { get; set; }

    public MuhurthaRegionalProfileType RegionalProfile { get; set; }

    public MuhurthaLocation Location { get; set; } = new();

    public DateTimeOffset RequestedStart { get; set; }

    public DateTimeOffset RequestedEnd { get; set; }

    public bool UsedPersonalHoroscope { get; set; }

    public List<string> UnsupportedCalculations { get; set; } = new();

    public List<MuhurthaRecommendation> RecommendedSlots { get; set; } = new();
}
