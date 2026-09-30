namespace KundaliEngine.API.DTOs.LifeArea;

public sealed class LifeAreaCatalogItemDto
{
    public string Key { get; set; } = string.Empty;

    public string Name { get; set; } = string.Empty;

    public IReadOnlyList<string> PrimaryCharts { get; set; } = Array.Empty<string>();

    public IReadOnlyList<string> SupportingCharts { get; set; } = Array.Empty<string>();

    public IReadOnlyList<int> RequiredHouses { get; set; } = Array.Empty<int>();

    public IReadOnlyList<string> RequiredPlanets { get; set; } = Array.Empty<string>();
}

public sealed class LifeAreaFactorDto
{
    public string Subject { get; set; } = string.Empty;

    public string NatalSign { get; set; } = string.Empty;

    public string NatalDignity { get; set; } = string.Empty;

    public string SpecialistChart { get; set; } = string.Empty;

    public string SpecialistSign { get; set; } = string.Empty;

    public string SpecialistDignity { get; set; } = string.Empty;

    public string Tone { get; set; } = string.Empty;

    public string Reading { get; set; } = string.Empty;
}

public sealed class LifeAreaReportDto
{
    public string AreaKey { get; set; } = string.Empty;

    public string AreaName { get; set; } = string.Empty;

    public string Verdict { get; set; } = string.Empty;

    public string VerdictText { get; set; } = string.Empty;

    public IReadOnlyList<string> ChartsConsulted { get; set; } = Array.Empty<string>();

    public string SpecialistChart { get; set; } = string.Empty;

    public IReadOnlyList<LifeAreaFactorDto> Factors { get; set; } = Array.Empty<LifeAreaFactorDto>();

    public string CrossValidation { get; set; } = string.Empty;

    public string Timing { get; set; } = string.Empty;

    public IReadOnlyList<LifeAreaPeriodDto> Periods { get; set; } = Array.Empty<LifeAreaPeriodDto>();

    public string? Note { get; set; }

    public string PlainMeaning { get; set; } = string.Empty;
}

public sealed class LifeAreaInsightDto
{
    public string Insight { get; set; } = string.Empty;
}

public sealed class LifeAreaPeriodDto
{
    public string Label { get; set; } = string.Empty;

    public string Planet { get; set; } = string.Empty;

    public string Tone { get; set; } = string.Empty;

    public string Reading { get; set; } = string.Empty;
}

public sealed class LifeAreaWindow
{
    public string Planet { get; init; } = string.Empty;

    public DateTime Start { get; init; }

    public DateTime End { get; init; }
}

public sealed class LifeAreaTiming
{
    public string? Mahadasha { get; init; }

    public string? Antardasha { get; init; }

    public IReadOnlyList<LifeAreaWindow> Windows { get; init; } = Array.Empty<LifeAreaWindow>();
}
