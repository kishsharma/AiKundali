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
    }
}
