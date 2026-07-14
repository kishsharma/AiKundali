namespace KundaliEngine.Domain.Models.Muhurtha;

public class MuhurthaScoringConfiguration
{
    public int GenericTimeRulesWeight { get; set; } = 15;

    public int PanchangaRulesWeight { get; set; } = 25;

    public int PersonalHoroscopeRulesWeight { get; set; } = 30;

    public int MuhurthaChartRulesWeight { get; set; } = 30;

    public List<MuhurthaVerdictConfiguration> Verdicts { get; set; } = new();
}
