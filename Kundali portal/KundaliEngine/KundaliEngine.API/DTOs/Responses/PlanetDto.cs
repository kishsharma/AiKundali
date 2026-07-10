namespace KundaliEngine.API.DTOs.Responses
{
    public class PlanetDto
    {
        public string Planet { get; set; } = string.Empty;
        public string Sign { get; set; } = string.Empty;
        public int Degree { get; set; }
        public int Minute { get; set; }
        public int House { get; set; }
        public double Longitude { get; set; }
        public double Latitude { get; set; }
        public bool IsCombust { get; set; }
        public bool IsRetrograde { get; set; }
        public string DetailedStrength { get; set; } = string.Empty;
        public List<string> AspectedBy { get; set; } = new();
        public bool HasBeneficAspect { get; set; }
        public bool HasMaleficAspect { get; set; }
        public string ConditionTone { get; set; } = "Neutral";
        public string Nakshatra { get; set; } = string.Empty;
        public int Pada { get; set; }
    }
}
