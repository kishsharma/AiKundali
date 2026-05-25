using System.Collections.Generic;

namespace KundaliEngine.API.DTOs.Responses
{
    public class HouseDto
    {
        public int HouseNumber { get; set; }
        public string HouseName { get; set; }

        public List<string> LifeAreas { get; set; }
        public string Sign { get; set; } = string.Empty;
        public string HouseOwner { get; set; } = string.Empty;
        public string HouseSwamy { get; set; } = string.Empty;
        public List<string> Occupants { get; set; } = new List<string>();
        public bool HasBeneficAspect { get; set; }
        public bool HasMaleficAspect { get; set; }

        // Scoring details
        public int Score { get; set; }
        public List<string> Factors { get; set; } = new List<string>();
        public List<string> DominantThemes { get; set; } = new List<string>();


    }
}
