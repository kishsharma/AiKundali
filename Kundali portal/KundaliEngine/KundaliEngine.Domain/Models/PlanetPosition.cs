using KundaliEngine.Domain.Enums;

namespace KundaliEngine.Domain.Models
{
    public class PlanetPosition
    {
        public Planet Planet { get; set; }
        public string PlanetName { get; set; }
        public double Longitude { get; set; }
        public double Latitude { get; set; }
        public ZodiacSign Sign { get; set; }
        public double DegreeInSign { get; set; }
        public int Degree { get; set; }
        public int Minute { get; set; }
        public Nakshatra Nakshatra { get; set; }
        public int Pada { get; set; }
        public HouseNumber House { get; set; }
        public bool IsRetrograde { get; set; }
        public string DetailedStrength { get; set; } = string.Empty;
        public bool IsCombust { get; set; }
        
    }
}
