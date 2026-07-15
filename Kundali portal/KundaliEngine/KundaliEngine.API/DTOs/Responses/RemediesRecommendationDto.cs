namespace KundaliEngine.API.DTOs.Responses
{
    public class RemediesRecommendationDto
    {
        public RemedySelectionDto? Primary { get; set; }
        public RemedySelectionDto? Alternative { get; set; }
        public List<RemedyCategoryAnalysisDto> Categories { get; set; } = new();
        public List<RemedyAvoidRecommendationDto> Avoid { get; set; } = new();
        public string Summary { get; set; } = string.Empty;
        public string Disclaimer { get; set; } = "Traditional guidance only. Wear high-impact gemstones only after review by a qualified astrologer.";
    }

    public class RemedySelectionDto
    {
        public string RemedyType { get; set; } = string.Empty;
        public string RecommendationRole { get; set; } = string.Empty;
        public string Planet { get; set; } = string.Empty;
        public string Name { get; set; } = string.Empty;
        public string SanskritName { get; set; } = string.Empty;
        public string EnglishName { get; set; } = string.Empty;
        public string Category { get; set; } = string.Empty;
        public string StrengthLevel { get; set; } = string.Empty;
        public int Confidence { get; set; }
        public string Suitability { get; set; } = string.Empty;
        public string Reason { get; set; } = string.Empty;
        public List<string> Factors { get; set; } = new();
        public RemedyCatalogDto Remedies { get; set; } = new();
    }

    public class RemedyCategoryAnalysisDto
    {
        public string Category { get; set; } = string.Empty;
        public string Planet { get; set; } = string.Empty;
        public string RemedyType { get; set; } = "Gemstone";
        public string RecommendedRemedy { get; set; } = string.Empty;
        public int Score { get; set; }
        public string Suitability { get; set; } = string.Empty;
        public List<string> Reasons { get; set; } = new();
    }

    public class RemedyAvoidRecommendationDto
    {
        public string Planet { get; set; } = string.Empty;
        public string RemedyType { get; set; } = "Gemstone";
        public string RemedyName { get; set; } = string.Empty;
        public string Reason { get; set; } = string.Empty;
        public List<string> Factors { get; set; } = new();
    }

    public class RemedyCatalogDto
    {
        public GemstoneRemedyCatalogDto Gemstone { get; set; } = new();
        public YantraRemedyDto Yantra { get; set; } = new();
        public List<string> Daan { get; set; } = new();
        public JapaRemedyDto Japa { get; set; } = new();
        public List<object> AdditionalRemedies { get; set; } = new();
    }

    public class GemstoneRemedyCatalogDto
    {
        public RemedyStoneDto Primary { get; set; } = new();
        public List<RemedyStoneDto> Alternatives { get; set; } = new();
        public WearingRulesDto WearingRules { get; set; } = new();
        public string Caution { get; set; } = string.Empty;
    }

    public class RemedyStoneDto
    {
        public string Name { get; set; } = string.Empty;
        public string SanskritName { get; set; } = string.Empty;
        public string EnglishName { get; set; } = string.Empty;
        public string Purpose { get; set; } = string.Empty;
        public List<string> RecommendedFor { get; set; } = new();
    }

    public class WearingRulesDto
    {
        public string Finger { get; set; } = string.Empty;
        public string DayToWear { get; set; } = string.Empty;
        public string Time { get; set; } = string.Empty;
        public List<string> Metal { get; set; } = new();
    }

    public class YantraRemedyDto
    {
        public string Name { get; set; } = string.Empty;
        public string Purpose { get; set; } = string.Empty;
        public List<string> ApplicableWhen { get; set; } = new();
    }

    public class JapaRemedyDto
    {
        public string Mantra { get; set; } = string.Empty;
        public int Count { get; set; }
        public string Day { get; set; } = string.Empty;
    }
}
