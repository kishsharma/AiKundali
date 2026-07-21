namespace KundaliEngine.API.DTOs.Requests;

public class CurrentLifeFocusRequest
{
    public Guid KundaliId { get; set; }

    public DateTimeOffset AnalysisDate { get; set; }
}

public class FocusedLifeAreaRequest
{
    public string SelectedAreaKey { get; set; } = string.Empty;

    public string? SpecificConcernKey { get; set; }
}
