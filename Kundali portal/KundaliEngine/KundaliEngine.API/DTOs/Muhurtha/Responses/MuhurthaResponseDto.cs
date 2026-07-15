using KundaliEngine.API.DTOs.Muhurtha.Requests;
using KundaliEngine.Domain.Enums;

namespace KundaliEngine.API.DTOs.Muhurtha.Responses;

public class MuhurthaResponseDto
{
    public MuhurthaEventType EventType { get; set; }

    public MuhurthaRegionalProfileType RegionalProfile { get; set; }

    public MuhurthaLocationDto Location { get; set; } = new();

    public DateTimeOffset RequestedStart { get; set; }

    public DateTimeOffset RequestedEnd { get; set; }

    public bool UsedPersonalHoroscope { get; set; }

    public List<string> UnsupportedCalculations { get; set; } = new();

    public List<MuhurthaRecommendationDto> RecommendedSlots { get; set; } = new();
}
