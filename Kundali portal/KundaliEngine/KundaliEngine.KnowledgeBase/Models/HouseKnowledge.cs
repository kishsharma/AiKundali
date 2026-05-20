namespace KundaliEngine.KnowledgeBase.Models
{
    public class HouseKnowledge
    {
        public int Number { get; set; }
        public string Name { get; set; } = string.Empty;
        public string SanskritName { get; set; } = string.Empty;
        public string BhavaType { get; set; } = string.Empty;
        public List<string> Karakatva { get; set; } = new List<string>();
        public List<string> LifeAreas { get; set; } = new List<string>();
        public List<string> NaturalSignifications { get; set; } = new List<string>();
        public string PredictionArea { get; set; } = string.Empty;
        public string PredictionExpression { get; set; } = string.Empty;
    }
}
