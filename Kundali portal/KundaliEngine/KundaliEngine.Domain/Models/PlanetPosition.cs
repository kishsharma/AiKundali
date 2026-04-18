using KundaliEngine.Domain.Enums;

namespace KundaliEngine.Domain.Models
{
    public class PlanetPosition
    {
        public Planet Planet { get; set; }
        public double Longitude { get; set; }
        public double Latitude { get; set; }
        public RashiZodiacSign Sign { get; set; }
        public Nakshatra? Nakshatra { get; set; }
        public int? Pada { get; set; }
        public int Degree { get; set; }
        public int Minute { get; set; }
        public HouseNumber? House { get; set; }
    }
}
