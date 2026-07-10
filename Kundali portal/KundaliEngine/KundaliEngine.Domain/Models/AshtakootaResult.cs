using System.Collections.Generic;

namespace KundaliEngine.Domain.Models
{
    public class AshtakootaResult
    {
        public double TotalScore { get; set; }
        public double MaxScore { get; set; } = 36;
        public List<KutaScore> Scores { get; set; } = new();
    }

    public class KutaScore
    {
        public string Name { get; set; } = string.Empty;
        public double Score { get; set; }
        public double MaxScore { get; set; }
        public string Grade { get; set; } = string.Empty;
        public string Detail { get; set; } = string.Empty;
    }
}
