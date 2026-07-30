using KundaliEngine.API.DTOs.Learning;

namespace KundaliEngine.API.DTOs.Responses;

public class DashboardSummaryDto
{
    public DashboardUserDto User { get; set; } = new();

    public DashboardPanchangDto TodayPanchang { get; set; } = new();

    public DashboardCollectionStatsDto CollectionStats { get; set; } = new();

    public SavedKundaliListItemDto? LatestKundali { get; set; }

    public CurrentDashaDto? CurrentDasha { get; set; }

    public LearningLevelSummary? LearningSummary { get; set; }

    public IReadOnlyList<LearningLevelSummary> LearningLevels { get; set; } = Array.Empty<LearningLevelSummary>();

    public DashboardRecentReportDto? RecentReport { get; set; }

    public IReadOnlyList<DashboardMuhurthaPreviewDto> UpcomingMuhurthas { get; set; } = Array.Empty<DashboardMuhurthaPreviewDto>();

    public IReadOnlyList<DashboardInsightDto> DailyInsights { get; set; } = Array.Empty<DashboardInsightDto>();
}

public class DashboardUserDto
{
    public Guid Id { get; set; }

    public string FullName { get; set; } = string.Empty;

    public string Email { get; set; } = string.Empty;

    public string Role { get; set; } = string.Empty;
}

public class DashboardPanchangDto
{
    public DateTime Date { get; set; }

    public string Tithi { get; set; } = string.Empty;

    public string Paksha { get; set; } = string.Empty;

    public string Nakshatra { get; set; } = string.Empty;

    public int Pada { get; set; }

    public string Yoga { get; set; } = string.Empty;

    public string Karana { get; set; } = string.Empty;

    public string Vaar { get; set; } = string.Empty;

    public string Maas { get; set; } = string.Empty;

    public string? SunRiseTime { get; set; }

    public string? SunSetTime { get; set; }

    public string LocationName { get; set; } = string.Empty;
}

public class DashboardCollectionStatsDto
{
    public int SavedKundalis { get; set; }

    public int SavedMatches { get; set; }

    public int SavedMuhurthas { get; set; }

    public int Reports { get; set; }

    public int StudyLevels { get; set; }
}

public class DashboardRecentReportDto
{
    public Guid Id { get; set; }

    public Guid KundaliId { get; set; }

    public string ReportType { get; set; } = string.Empty;

    public string ReportStatus { get; set; } = string.Empty;

    public string KundaliName { get; set; } = string.Empty;

    public DateTime CreatedAt { get; set; }
}

public class DashboardMuhurthaPreviewDto
{
    public DateTime Date { get; set; }

    public string Title { get; set; } = string.Empty;

    public string TimeWindow { get; set; } = string.Empty;

    public string Quality { get; set; } = string.Empty;
}

public class DashboardInsightDto
{
    public string Eyebrow { get; set; } = string.Empty;

    public string Title { get; set; } = string.Empty;

    public string Text { get; set; } = string.Empty;

    public string RouterLink { get; set; } = string.Empty;

    public string Action { get; set; } = string.Empty;

    public string Tone { get; set; } = string.Empty;
}
