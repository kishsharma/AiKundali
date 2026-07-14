using KundaliEngine.Domain.Enums;

namespace KundaliEngine.Domain.Models.Muhurtha;

public class MuhurthaRequest
{
    public MuhurthaEventType EventType { get; set; }

    public MuhurthaRegionalProfileType RegionalProfile { get; set; } = MuhurthaRegionalProfileType.General;

    public MuhurthaLocation Location { get; set; } = new();

    public DateTimeOffset RequestedStart { get; set; }

    public DateTimeOffset RequestedEnd { get; set; }

    public TimeSpan? DailyStartTime { get; set; }

    public TimeSpan? DailyEndTime { get; set; }

    public MuhurthaHoroscopeReference? Horoscope { get; set; }

    public MuhurthaUserFilters Filters { get; set; } = new();
}
