namespace KundaliEngine.API.DTOs.Muhurtha.Responses;

public class MuhurthaTimingDetailDto
{
    public string Name { get; set; } = string.Empty;

    public DateTimeOffset? StartTime { get; set; }

    public DateTimeOffset? EndTime { get; set; }
}
