namespace KundaliEngine.API.Services.ReportPdf.Html;

public sealed class ReportLifeChapter
{
    public required string Title { get; init; }

    public required string Tone { get; init; }

    public required string Summary { get; init; }

    public string? Window { get; init; }
}
