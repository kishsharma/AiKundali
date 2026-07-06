namespace KundaliEngine.Astrology.Services.KundaliYoga
{
    public interface IYogaRule
    {
        string Key { get; }
        YogaRuleMatch? Evaluate(YogaEvaluationContext context);
    }

    public class YogaRuleMatch
    {
        public string Key { get; set; } = string.Empty;
        public string Strength { get; set; } = "Neutral";
        public List<string> InvolvedPlanets { get; set; } = new();
        public List<int> InvolvedHouses { get; set; } = new();
        public string Reason { get; set; } = string.Empty;
    }
}
