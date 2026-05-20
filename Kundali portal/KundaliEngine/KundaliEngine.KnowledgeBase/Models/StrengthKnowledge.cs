namespace KundaliEngine.KnowledgeBase.Models
{
    public class StrengthKnowledge
    {
        public string Name { get; set; } = string.Empty;
        public int Rank { get; set; }
        public string Category { get; set; } = string.Empty;
        public string Meaning { get; set; } = string.Empty;
        public List<string> Indicators { get; set; } = new List<string>();
        public List<string> InterpretationHints { get; set; } = new List<string>();
        public string PredictionModifier { get; set; } = string.Empty;
    }
}
