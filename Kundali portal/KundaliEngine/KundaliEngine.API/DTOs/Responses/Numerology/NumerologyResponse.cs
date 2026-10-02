namespace KundaliEngine.API.DTOs.Responses.Numerology
{
    public class NumerologyResponse
    {
        public DateOnly DateOfBirth { get; set; }

        public int Mulank { get; set; }

        public int Bhagyank { get; set; }

        public string MulankPlanet { get; set; } = string.Empty;

        public string BhagyankPlanet { get; set; } = string.Empty;

        public string MulankCalculation { get; set; } = string.Empty;

        public string BhagyankCalculation { get; set; } = string.Empty;

        public string CombinationType { get; set; } = string.Empty;

        public string CombinationSummary { get; set; } = string.Empty;

        public List<string> CombinationStrengths { get; set; } = [];

        public List<string> CombinationChallenges { get; set; } = [];

        public NumerologyNumberDetailsResponse? MulankDetails { get; set; }

        public NumerologyNumberDetailsResponse? BhagyankDetails { get; set; }

    }
}
