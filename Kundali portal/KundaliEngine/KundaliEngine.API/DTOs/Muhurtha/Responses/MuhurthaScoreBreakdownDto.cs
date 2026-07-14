namespace KundaliEngine.API.DTOs.Muhurtha.Responses;

public class MuhurthaScoreBreakdownDto
{
    public int GenericTimeRulesScore { get; set; }

    public int PanchangaRulesScore { get; set; }

    public int PersonalHoroscopeRulesScore { get; set; }

    public int MuhurthaChartRulesScore { get; set; }

    public int TotalScore { get; set; }

    public bool HardRejected { get; set; }
}
