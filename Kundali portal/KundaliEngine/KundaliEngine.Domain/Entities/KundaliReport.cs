namespace KundaliEngine.Domain.Entities;

public class KundaliReport
{
    public Guid Id { get; set; }

    public Guid KundaliId { get; set; }

    public string ReportType { get; set; } = string.Empty;

    public string ReportStatus { get; set; } = "Generated";

    public string FactsJson { get; set; } = string.Empty;

    public string? ScoresJson { get; set; }

    public string? SummaryJson { get; set; }

    public string ReportVersion { get; set; } = "v1";

    public DateTime CreatedAt { get; set; }

    public Kundali? Kundali { get; set; }
}
