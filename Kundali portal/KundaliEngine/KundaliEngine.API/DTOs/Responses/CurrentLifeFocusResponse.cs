namespace KundaliEngine.API.DTOs.Responses;

public class CurrentLifeFocusResponse
{
    public Guid AnalysisId { get; set; }

    public DateTimeOffset AnalysisDate { get; set; }

    public CurrentLifeFocusDashaDto ActiveDasha { get; set; } = new();

    public List<CurrentLifeFocusAreaDto> Areas { get; set; } = new();

    public string Question { get; set; } = "Which area best matches what you are currently experiencing?";
}

public class CurrentLifeFocusDashaDto
{
    public string Mahadasha { get; set; } = string.Empty;

    public string Antardasha { get; set; } = string.Empty;
}

public class CurrentLifeFocusAreaDto
{
    public string Key { get; set; } = string.Empty;

    public string Title { get; set; } = string.Empty;

    public int ActivationScore { get; set; }

    public string ActivationLevel { get; set; } = string.Empty;

    public string Tone { get; set; } = string.Empty;

    public int Confidence { get; set; }

    public List<int> ActivatedHouses { get; set; } = new();

    public List<string> ShortReasons { get; set; } = new();
}

public class FocusedLifeAreaResponse
{
    public string AreaKey { get; set; } = string.Empty;

    public string Title { get; set; } = string.Empty;

    public string ActivationLevel { get; set; } = string.Empty;

    public string Tone { get; set; } = string.Empty;

    public string Summary { get; set; } = string.Empty;

    public FocusedLifeAreaNarrativeDto Narrative { get; set; } = new();

    
    public FocusedDecisionGuidanceDto DecisionGuidance { get; set; } = new();

    public List<string> CalculationBasis { get; set; } = new();

    public List<string> PossibleExperiences { get; set; } = new();

    public List<string> SupportiveFactors { get; set; } = new();

    public List<string> PressureFactors { get; set; } = new();

    public List<FocusedActivatedHouseDto> ActivatedHouses { get; set; } = new();

    public string FollowUpQuestion { get; set; } = string.Empty;

    public List<CurrentLifeFocusOptionDto> FollowUpOptions { get; set; } = new();
}


public class FocusedDecisionGuidanceDto
{
    public string QuestionType { get; set; } = string.Empty;

    public string VerdictLabel { get; set; } = string.Empty;

    public string Recommendation { get; set; } = string.Empty;

    public string Why { get; set; } = string.Empty;

    public string BestApproach { get; set; } = string.Empty;

    public List<string> SuitableFor { get; set; } = new();

    public List<string> Cautions { get; set; } = new();
}

public class FocusedLifeAreaNarrativeDto
{
    public string Verdict { get; set; } = string.Empty;

    public string CurrentSituation { get; set; } = string.Empty;

    public string AstrologicalReason { get; set; } = string.Empty;

    public string FutureChange { get; set; } = string.Empty;

    public string ImportantPeriods { get; set; } = string.Empty;

    public string ExpectedOutcome { get; set; } = string.Empty;

    public List<string> Evidence { get; set; } = new();
}

public class FocusedActivatedHouseDto
{
    public int House { get; set; }

    public string Role { get; set; } = string.Empty;

    public int ActivationScore { get; set; }
}

public class CurrentLifeFocusOptionDto
{
    public string Key { get; set; } = string.Empty;

    public string Label { get; set; } = string.Empty;
}
