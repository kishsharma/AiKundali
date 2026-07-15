namespace KundaliEngine.Domain.Models.Muhurtha;

public class MuhurthaScoreBreakdown
{
    public int GenericTimeRulesScore { get; set; }

    public int PanchangaRulesScore { get; set; }

    public int PersonalHoroscopeRulesScore { get; set; }

    public int MuhurthaChartRulesScore { get; set; }

    public int TotalScore { get; set; }

    public bool HardRejected { get; set; }
}
