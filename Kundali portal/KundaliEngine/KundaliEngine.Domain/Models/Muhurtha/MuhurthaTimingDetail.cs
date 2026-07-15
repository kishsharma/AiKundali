namespace KundaliEngine.Domain.Models.Muhurtha;

public class MuhurthaTimingDetail
{
    public string Name { get; set; } = string.Empty;

    public DateTimeOffset? StartTime { get; set; }

    public DateTimeOffset? EndTime { get; set; }
}
