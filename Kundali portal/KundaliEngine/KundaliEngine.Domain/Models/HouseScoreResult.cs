using KundaliEngine.Domain.Enums;

namespace KundaliEngine.Domain.Models
{
    public class HouseScoreResult
    {
        public HouseNumber HouseNumber { get; set; }
        public string HouseName { get; set; } = string.Empty;
        public int Score { get; set; }
        public List<string> Factors { get; set; } = new List<string>();
        public List<string> DominantThemes { get; set; } = new List<string>();
    }
}
