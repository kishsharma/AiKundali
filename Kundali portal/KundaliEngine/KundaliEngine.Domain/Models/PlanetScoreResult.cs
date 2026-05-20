using KundaliEngine.Domain.Enums;

namespace KundaliEngine.Domain.Models
{
    public class PlanetScoreResult
    {
        public Planet Planet { get; set; }
        public int Score { get; set; }
        public List<string> Factors { get; set; } = new List<string>();
    }
}
