namespace KundaliEngine.API.Services.Muhurtha.Models;

public class MuhurthaDayContext
{
    public DateOnly Date { get; set; }

    public DateTimeOffset Sunrise { get; set; }

    public DateTimeOffset Sunset { get; set; }

    public DateTimeOffset DayStart { get; set; }

    public DateTimeOffset DayEnd { get; set; }

    public List<MuhurthaTimeWindow> TimeWindows { get; set; } = new();
}
