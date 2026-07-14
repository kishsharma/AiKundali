using KundaliEngine.Domain.Enums;

namespace KundaliEngine.API.DTOs.Muhurtha.Requests;

public class FindMuhurthaRequestDto
{
    public MuhurthaEventType EventType { get; set; }

    public MuhurthaRegionalProfileType RegionalProfile { get; set; } = MuhurthaRegionalProfileType.General;

    public MuhurthaLocationDto Location { get; set; } = new();

    public DateTimeOffset RequestedStart { get; set; }

    public DateTimeOffset RequestedEnd { get; set; }

    public TimeSpan? DailyStartTime { get; set; }

    public TimeSpan? DailyEndTime { get; set; }

    public MuhurthaHoroscopeReferenceDto? Horoscope { get; set; }

    public MuhurthaUserFiltersDto Filters { get; set; } = new();
}
