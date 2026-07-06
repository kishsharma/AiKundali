using System.Collections.Generic;
using KundaliEngine.Domain.Enums;

namespace KundaliEngine.Domain.Models
{
    public class HouseDto
    {
        public HouseNumber HouseNumber { get; set; }
        public ZodiacSign Sign { get; set; }
        public List<Planet> Occupants { get; set; } = new List<Planet>();
        public string HouseName { get; set; }

        public List<string> LifeAreas { get; set; }

        public string HouseOwner { get; set; } = string.Empty;
        public string HouseSwamy { get; set; } = string.Empty;

        public bool HasBeneficAspect { get; set; }
        public bool HasMaleficAspect { get; set; }

        // Scoring details
        public int Score { get; set; }
        public List<string> Factors { get; set; } = new List<string>();
        public List<string> DominantThemes { get; set; } = new List<string>();
    }

   
}
