namespace KundaliEngine.Domain.Models.Muhurtha;

public class MuhurthaPanchangTimings
{
    public MuhurthaTimingDetail Tithi { get; set; } = new();

    public MuhurthaTimingDetail Nakshatra { get; set; } = new();

    public MuhurthaTimingDetail Karana { get; set; } = new();

    public MuhurthaTimingDetail Yoga { get; set; } = new();

    public MuhurthaTimingDetail ChandraRashi { get; set; } = new();

    public MuhurthaTimingDetail Lagna { get; set; } = new();
}
