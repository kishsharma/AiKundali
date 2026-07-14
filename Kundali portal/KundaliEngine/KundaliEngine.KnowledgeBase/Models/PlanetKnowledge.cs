using System.Text.Json;

namespace KundaliEngine.KnowledgeBase.Models
{
    public class PlanetKnowledge
    {
        public string Name { get; set; } = string.Empty;
        public string SanskritName { get; set; } = string.Empty;
        public string Nature { get; set; } = string.Empty;
        public string Classification { get; set; } = string.Empty;
        public string Gender { get; set; } = string.Empty;
        public string Tattva { get; set; } = string.Empty;
        public string Guna { get; set; } = string.Empty;
        public List<string> Karakatva { get; set; } = new List<string>();
        public List<string> OwnSigns { get; set; } = new List<string>();
        public string Exaltation { get; set; } = string.Empty;
        public string Debilitation { get; set; } = string.Empty;
        public string Moolatrikona { get; set; } = string.Empty;
        public List<string> Friends { get; set; } = new List<string>();
        public List<string> Enemies { get; set; } = new List<string>();
        public List<string> Neutral { get; set; } = new List<string>();
        public string PredictionNature { get; set; } = string.Empty;
        public string PositiveOutcome { get; set; } = string.Empty;
        public string NegativeOutcome { get; set; } = string.Empty;
        public string DashaPrediction { get; set; } = string.Empty;
        public string ShortDashaTheme { get; set; } = string.Empty;
        public GemstoneRecommendationKnowledge GemstoneRecommendation { get; set; } = new GemstoneRecommendationKnowledge();
        public PlanetRemediesKnowledge Remedies { get; set; } = new PlanetRemediesKnowledge();
    }

    public class GemstoneRecommendationKnowledge
    {
        public GemstoneRecommendationItemKnowledge PrimaryGemstone { get; set; } = new GemstoneRecommendationItemKnowledge();
        public List<GemstoneAlternativeRecommendationKnowledge> Alternatives { get; set; } = new List<GemstoneAlternativeRecommendationKnowledge>();
        public string ChandraRashiUse { get; set; } = string.Empty;
        public string RecommendationCaution { get; set; } = string.Empty;
    }

    public class GemstoneRecommendationItemKnowledge
    {
        public string Name { get; set; } = string.Empty;
        public string SanskritName { get; set; } = string.Empty;
        public string EnglishName { get; set; } = string.Empty;
        public string Category { get; set; } = string.Empty;
        public string StrengthLevel { get; set; } = string.Empty;
        public string RecommendationRole { get; set; } = string.Empty;
        public string PlanetaryAction { get; set; } = string.Empty;
        public string SuitabilityRule { get; set; } = string.Empty;
        public List<string> BestUsedWhen { get; set; } = new List<string>();
        public List<string> AvoidWhen { get; set; } = new List<string>();
    }

    public class GemstoneAlternativeRecommendationKnowledge
    {
        public string Name { get; set; } = string.Empty;
        public string Category { get; set; } = string.Empty;
        public string StrengthLevel { get; set; } = string.Empty;
        public string RecommendationRole { get; set; } = string.Empty;
        public List<string> UseWhen { get; set; } = new List<string>();
    }

    public class PlanetRemediesKnowledge
    {
        public GemstoneRemedyKnowledge Gemstone { get; set; } = new GemstoneRemedyKnowledge();
        public YantraRemedyKnowledge Yantra { get; set; } = new YantraRemedyKnowledge();
        public List<string> Daan { get; set; } = new List<string>();
        public JapaRemedyKnowledge Japa { get; set; } = new JapaRemedyKnowledge();
        public List<JsonElement> AdditionalRemedies { get; set; } = new List<JsonElement>();
    }

    public class GemstoneRemedyKnowledge
    {
        public RatnaRemedyKnowledge Primary { get; set; } = new RatnaRemedyKnowledge();
        public List<UpratnaRemedyKnowledge> Alternatives { get; set; } = new List<UpratnaRemedyKnowledge>();
        public WearingRulesKnowledge WearingRules { get; set; } = new WearingRulesKnowledge();
        public string Caution { get; set; } = string.Empty;
    }

    public class RatnaRemedyKnowledge
    {
        public string Name { get; set; } = string.Empty;
        public string SanskritName { get; set; } = string.Empty;
        public string EnglishName { get; set; } = string.Empty;
        public string Purpose { get; set; } = string.Empty;
        public List<string> RecommendedFor { get; set; } = new List<string>();
    }

    public class UpratnaRemedyKnowledge
    {
        public string Name { get; set; } = string.Empty;
        public string Purpose { get; set; } = string.Empty;
    }

    public class WearingRulesKnowledge
    {
        public string Finger { get; set; } = string.Empty;
        public string DayToWear { get; set; } = string.Empty;
        public string Time { get; set; } = string.Empty;
        public List<string> Metal { get; set; } = new List<string>();
    }

    public class YantraRemedyKnowledge
    {
        public string Name { get; set; } = string.Empty;
        public string Purpose { get; set; } = string.Empty;
        public List<string> ApplicableWhen { get; set; } = new List<string>();
    }

    public class JapaRemedyKnowledge
    {
        public string Mantra { get; set; } = string.Empty;
        public int Count { get; set; }
        public string Day { get; set; } = string.Empty;
    }
}
