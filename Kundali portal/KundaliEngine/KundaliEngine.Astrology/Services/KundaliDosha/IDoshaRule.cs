using KundaliEngine.Astrology.Services.KundaliYoga;

namespace KundaliEngine.Astrology.Services.KundaliDosha
{
    public interface IDoshaRule
    {
        string Key { get; }
        DoshaRuleMatch? Evaluate(YogaEvaluationContext context);
    }

    public class DoshaRuleMatch
    {
        public string Key { get; set; } = string.Empty;
        public string Severity { get; set; } = "Low";
        public bool IsPresent { get; set; } = true;
        public List<string> InvolvedPlanets { get; set; } = new();
        public List<int> InvolvedHouses { get; set; } = new();
        public string Reason { get; set; } = string.Empty;
    }
}
