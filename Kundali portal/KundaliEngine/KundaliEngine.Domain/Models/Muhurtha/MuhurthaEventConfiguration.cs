using KundaliEngine.Domain.Enums;

namespace KundaliEngine.Domain.Models.Muhurtha;

public class MuhurthaEventConfiguration
{
    public MuhurthaEventType EventType { get; set; }

    public string DisplayName { get; set; } = string.Empty;

    public List<int> ImportantHouses { get; set; } = new();

    public bool IsActive { get; set; } = true;
}
