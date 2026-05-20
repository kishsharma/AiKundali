using System.Collections.Generic;

namespace KundaliEngine.API.DTOs.Responses
{
    public class HouseDto
    {
        public int HouseNumber { get; set; }
        public string Sign { get; set; } = string.Empty;
        public List<string> Occupants { get; set; } = new List<string>();

        // Scoring details
        public int Score { get; set; }
        public List<string> Factors { get; set; } = new List<string>();
        public List<string> DominantThemes { get; set; } = new List<string>();
    }
}