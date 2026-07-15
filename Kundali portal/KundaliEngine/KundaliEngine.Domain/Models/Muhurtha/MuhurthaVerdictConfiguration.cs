using KundaliEngine.Domain.Enums;

namespace KundaliEngine.Domain.Models.Muhurtha;

public class MuhurthaVerdictConfiguration
{
    public int MinScore { get; set; }

    public int MaxScore { get; set; }

    public MuhurthaVerdict Verdict { get; set; }
}
