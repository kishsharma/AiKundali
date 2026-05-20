using System.Text.Json.Serialization;

namespace KundaliEngine.KnowledgeBase.Models
{
    public class PlanetKnowledgeRoot
    {
        [JsonPropertyName("planets")]
        public List<PlanetKnowledge> Planets { get; set; } = new List<PlanetKnowledge>();
    }

    public class HouseKnowledgeRoot
    {
        [JsonPropertyName("houses")]
        public List<HouseKnowledge> Houses { get; set; } = new List<HouseKnowledge>();
    }

    public class StrengthKnowledgeRoot
    {
        [JsonPropertyName("strengths")]
        public List<StrengthKnowledge> Strengths { get; set; } = new List<StrengthKnowledge>();
    }
}
