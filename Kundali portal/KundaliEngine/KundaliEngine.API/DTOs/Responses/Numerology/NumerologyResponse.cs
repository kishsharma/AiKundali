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

        public NumerologyLoShuResponse LoShu { get; set; } = new();

        public NumerologyPersonalYearResponse PersonalYear { get; set; } = new();

        public NumerologyPersonalYearResponse NextPersonalYear { get; set; } = new();

        public string? Name { get; set; }

        public int? NameCompound { get; set; }

        public int? NameNumber { get; set; }

        public string? NamePlanet { get; set; }

        public string? NameCalculation { get; set; }

        public string? NameMulankMatch { get; set; }

        public string? NameBhagyankMatch { get; set; }

        public NumerologyValueCheckResponse? Mobile { get; set; }

        public NumerologyValueCheckResponse? Business { get; set; }

    }

    public class NumerologyValueCheckResponse
    {
        public string Label { get; set; } = string.Empty;

        public int Compound { get; set; }

        public int Number { get; set; }

        public string Planet { get; set; } = string.Empty;

        public string Calculation { get; set; } = string.Empty;

        public string MulankMatch { get; set; } = string.Empty;

        public string BhagyankMatch { get; set; } = string.Empty;

        public List<string> Suggestions { get; set; } = [];
    }

    public class NumerologyLoShuResponse
    {
        public List<NumerologyLoShuCellResponse> Cells { get; set; } = [];

        public List<int> Missing { get; set; } = [];

        public List<NumerologyRepeatResponse> Repeated { get; set; } = [];
    }

    public class NumerologyLoShuCellResponse
    {
        public int Number { get; set; }

        public int Count { get; set; }
    }

    public class NumerologyRepeatResponse
    {
        public int Number { get; set; }

        public int Count { get; set; }
    }

    public class NumerologyPersonalYearResponse
    {
        public int Year { get; set; }

        public int Number { get; set; }

        public string Planet { get; set; } = string.Empty;

        public string Calculation { get; set; } = string.Empty;
    }
}
