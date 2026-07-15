namespace KundaliEngine.API.DTOs.Responses
{
    public class YogaDto
    {
        public string Key { get; set; } = string.Empty;
        public string Name { get; set; } = string.Empty;
        public string SanskritName { get; set; } = string.Empty;
        public string Category { get; set; } = string.Empty;
        public string Nature { get; set; } = string.Empty;
        public string Description { get; set; } = string.Empty;
        public string Impact { get; set; } = string.Empty;
        public List<YogaImpactAreaDto> ImpactAreas { get; set; } = new();
        public List<string> References { get; set; } = new();
        public string Strength { get; set; } = "Neutral";
        public List<string> InvolvedPlanets { get; set; } = new();
        public List<int> InvolvedHouses { get; set; } = new();
        public string Reason { get; set; } = string.Empty;
    }

    public class YogaImpactAreaDto
    {
        public string Category { get; set; } = string.Empty;
        public List<string> Statements { get; set; } = new();
    }
}
