using KundaliEngine.Domain.Enums;

namespace KundaliEngine.Domain.Models.Muhurtha;

public class MuhurthaRegionalProfile
{
    public MuhurthaRegionalProfileType ProfileType { get; set; } = MuhurthaRegionalProfileType.General;

    public string DisplayName { get; set; } = string.Empty;

    public string Description { get; set; } = string.Empty;

    public bool IsDefault { get; set; }
}
