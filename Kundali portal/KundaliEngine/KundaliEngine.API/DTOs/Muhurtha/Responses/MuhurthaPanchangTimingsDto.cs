namespace KundaliEngine.API.DTOs.Muhurtha.Responses;

public class MuhurthaPanchangTimingsDto
{
    public MuhurthaTimingDetailDto Tithi { get; set; } = new();

    public MuhurthaTimingDetailDto Nakshatra { get; set; } = new();

    public MuhurthaTimingDetailDto Karana { get; set; } = new();

    public MuhurthaTimingDetailDto Yoga { get; set; } = new();

    public MuhurthaTimingDetailDto ChandraRashi { get; set; } = new();

    public MuhurthaTimingDetailDto Lagna { get; set; } = new();
}
