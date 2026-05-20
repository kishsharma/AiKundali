namespace KundaliEngine.API.DTOs.Responses
{
    public class OverallScoreDto
    {
        public int Overall { get; set; }

        public int Stability { get; set; }

        public int Growth { get; set; }

        public int Challenge { get; set; }

        public List<string> DominantStrengths { get; set; }

        public List<string> DominantWeaknesses { get; set; }
    }
}