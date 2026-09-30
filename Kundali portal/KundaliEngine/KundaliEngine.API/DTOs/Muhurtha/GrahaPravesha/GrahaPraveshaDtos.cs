namespace KundaliEngine.API.DTOs.Muhurtha.GrahaPravesha;

public class GrahaPraveshaLocationDto
{
    public string City { get; set; } = string.Empty;

    public double Latitude { get; set; }

    public double Longitude { get; set; }

    public string Timezone { get; set; } = "Asia/Kolkata";
}

public class GrahaPraveshaBirthChartDto
{
    public string? Name { get; set; }

    public DateTime? BirthDateTime { get; set; }

    public double? Latitude { get; set; }

    public double? Longitude { get; set; }

    public string? Timezone { get; set; }
}

public class GrahaPraveshaSearchRequestDto
{
    public GrahaPraveshaLocationDto Location { get; set; } = new();

    public DateOnly FromDate { get; set; }

    public DateOnly ToDate { get; set; }

    public string CalendarSystem { get; set; } = "AMANTA";

    public GrahaPraveshaBirthChartDto? BirthChart { get; set; }
}

public class GrahaPraveshaEvidenceDto
{
    public string RuleId { get; set; } = string.Empty;

    public string Category { get; set; } = string.Empty;

    public string Input { get; set; } = string.Empty;

    public string Result { get; set; } = string.Empty;

    public string Severity { get; set; } = string.Empty;

    public string Message { get; set; } = string.Empty;

    public string Source { get; set; } = string.Empty;

    public int ScoreImpact { get; set; }
}

public class GrahaPraveshaPeriodDto
{
    public string RuleKey { get; set; } = string.Empty;

    public string Name { get; set; } = string.Empty;

    public string Start { get; set; } = string.Empty;

    public string End { get; set; } = string.Empty;

    public DateTimeOffset StartTime { get; set; }

    public DateTimeOffset EndTime { get; set; }
}

public class GrahaPraveshaWindowDto
{
    public string Date { get; set; } = string.Empty;

    public string Start { get; set; } = string.Empty;

    public string End { get; set; } = string.Empty;

    public DateTimeOffset StartTime { get; set; }

    public DateTimeOffset EndTime { get; set; }

    public string Status { get; set; } = string.Empty;

    public int Score { get; set; }

    public string Lagna { get; set; } = string.Empty;

    public string Tithi { get; set; } = string.Empty;

    public string Paksha { get; set; } = string.Empty;

    public string Nakshatra { get; set; } = string.Empty;

    public string Yoga { get; set; } = string.Empty;

    public string Karana { get; set; } = string.Empty;

    public string Vara { get; set; } = string.Empty;

    public string MoonSign { get; set; } = string.Empty;

    public int KalashaCount { get; set; }

    public bool KalashaEligible { get; set; }

    public bool VastuTableMatched { get; set; }

    public List<GrahaPraveshaEvidenceDto> Evidence { get; set; } = new();

    public List<GrahaPraveshaPeriodDto> TimingRestrictions { get; set; } = new();
}

public class GrahaPraveshaSearchResponseDto
{
    public string EventType { get; set; } = "GRAHA_PRAVESHA";

    public string ProfileId { get; set; } = string.Empty;

    public string ProfileVersion { get; set; } = string.Empty;

    public string CalendarSystem { get; set; } = string.Empty;

    public GrahaPraveshaLocationDto Location { get; set; } = new();

    public GrahaPraveshaRangeDto SearchRange { get; set; } = new();

    public List<GrahaPraveshaWindowDto> Results { get; set; } = new();

    public Dictionary<string, int> RejectionCounts { get; set; } = new();

    public List<string> KnownGaps { get; set; } = new();
}

public class GrahaPraveshaRangeDto
{
    public string From { get; set; } = string.Empty;

    public string To { get; set; } = string.Empty;
}

public class GrahaPraveshaDayResponseDto
{
    public string EventType { get; set; } = "GRAHA_PRAVESHA";

    public string ProfileId { get; set; } = string.Empty;

    public string ProfileVersion { get; set; } = string.Empty;

    public string CalendarSystem { get; set; } = string.Empty;

    public string Date { get; set; } = string.Empty;

    public GrahaPraveshaLocationDto Location { get; set; } = new();

    public DateTimeOffset Sunrise { get; set; }

    public DateTimeOffset Sunset { get; set; }

    public List<GrahaPraveshaPeriodDto> TimingRestrictions { get; set; } = new();

    public List<GrahaPraveshaWindowDto> Intervals { get; set; } = new();

    public List<string> KnownGaps { get; set; } = new();
}
