using KundaliEngine.API.DTOs.Learning;
using KundaliEngine.API.DTOs.Responses;
using KundaliEngine.API.Services.Learning;
using KundaliEngine.Astrology.Services.Astrology;
using KundaliEngine.Astrology.Services.Dasha;
using KundaliEngine.Astrology.Services.Panchang;
using KundaliEngine.Domain.Entities;
using KundaliEngine.Domain.Enums;
using KundaliEngine.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;

namespace KundaliEngine.API.Services.Dashboard;

public sealed class DashboardSummaryService : IDashboardSummaryService
{
    private static readonly TimeZoneInfo IndiaTimeZone = TimeZoneInfo.FindSystemTimeZoneById("India Standard Time");
    private const double DefaultLatitude = 20.5937;
    private const double DefaultLongitude = 78.9629;
    private const string DefaultLocationName = "India";

    private readonly KundaliDbContext _dbContext;
    private readonly ChartService _chartService;
    private readonly PanchangService _panchangService;
    private readonly DashaService _dashaService;
    private readonly ILearningContentService _learningContentService;

    public DashboardSummaryService(
        KundaliDbContext dbContext,
        ChartService chartService,
        PanchangService panchangService,
        DashaService dashaService,
        ILearningContentService learningContentService)
    {
        _dbContext = dbContext;
        _chartService = chartService;
        _panchangService = panchangService;
        _dashaService = dashaService;
        _learningContentService = learningContentService;
    }

    public async Task<DashboardSummaryDto?> GetSummaryAsync(Guid userId, CancellationToken cancellationToken)
    {
        var user = await _dbContext.Users
            .AsNoTracking()
            .FirstOrDefaultAsync(item => item.Id == userId, cancellationToken);

        if (user is null)
        {
            return null;
        }

        var kundalis = await _dbContext.Kundalis
            .AsNoTracking()
            .Where(item => item.UserId == userId)
            .OrderByDescending(item => item.CreatedAt)
            .ToListAsync(cancellationToken);

        var latestKundali = kundalis.FirstOrDefault();
        var reports = await _dbContext.KundaliReports
            .AsNoTracking()
            .Include(item => item.Kundali)
            .Where(item => item.Kundali != null && item.Kundali.UserId == userId)
            .OrderByDescending(item => item.CreatedAt)
            .ToListAsync(cancellationToken);
        var savedCompatibilityCount = await _dbContext.SavedCompatibilities
            .AsNoTracking()
            .CountAsync(item => item.UserId == userId, cancellationToken);

        var learningLevels = await _learningContentService.GetLevelsAsync(cancellationToken);
        var panchang = BuildTodayPanchang(latestKundali);

        return new DashboardSummaryDto
        {
            User = new DashboardUserDto
            {
                Id = user.Id,
                FullName = user.FullName,
                Email = user.Email,
                Role = user.Role
            },
            TodayPanchang = panchang,
            CollectionStats = new DashboardCollectionStatsDto
            {
                SavedKundalis = kundalis.Count,
                SavedMatches = savedCompatibilityCount,
                SavedMuhurthas = 0,
                Reports = reports.Count,
                StudyLevels = learningLevels.Count
            },
            LatestKundali = latestKundali is null ? null : MapSavedKundali(latestKundali),
            CurrentDasha = latestKundali is null ? null : BuildCurrentDasha(latestKundali),
            LearningSummary = learningLevels.FirstOrDefault(),
            LearningLevels = learningLevels,
            RecentReport = reports.FirstOrDefault() is { } recentReport ? MapRecentReport(recentReport) : null,
            UpcomingMuhurthas = BuildUpcomingMuhurthas(),
            DailyInsights = BuildDailyInsights(panchang, learningLevels.FirstOrDefault())
        };
    }

    private DashboardPanchangDto BuildTodayPanchang(Kundali? latestKundali)
    {
        var nowIndia = TimeZoneInfo.ConvertTimeFromUtc(DateTime.UtcNow, IndiaTimeZone);
        var calculationLocal = new DateTime(nowIndia.Year, nowIndia.Month, nowIndia.Day, 12, 0, 0, DateTimeKind.Unspecified);
        var calculationUtc = TimeZoneInfo.ConvertTimeToUtc(calculationLocal, IndiaTimeZone);
        var latitude = latestKundali?.Latitude is null ? DefaultLatitude : Convert.ToDouble(latestKundali.Latitude.Value);
        var longitude = latestKundali?.Longitude is null ? DefaultLongitude : Convert.ToDouble(latestKundali.Longitude.Value);
        var locationName = string.IsNullOrWhiteSpace(latestKundali?.BirthPlace)
            ? DefaultLocationName
            : latestKundali!.BirthPlace;

        var planets = _chartService.GenerateChart(calculationUtc, latitude, longitude);
        var sun = planets.First(item => item.Planet == Planet.Sun);
        var moon = planets.First(item => item.Planet == Planet.Moon);
        var details = _panchangService.Calculate(calculationUtc, sun.Longitude, moon.Longitude);

        return new DashboardPanchangDto
        {
            Date = calculationLocal,
            Tithi = details.Tithi,
            Paksha = details.Paksha,
            Nakshatra = details.Nakshatra,
            Pada = details.Pada,
            Yoga = details.Yoga,
            Karana = details.Karana,
            Vaar = details.Vaar,
            Maas = details.Maas,
            LocationName = locationName
        };
    }

    private CurrentDashaDto? BuildCurrentDasha(Kundali kundali)
    {
        if (kundali.Latitude is null || kundali.Longitude is null)
        {
            return null;
        }

        var birthLocal = kundali.DateOfBirth.ToDateTime(kundali.TimeOfBirth);
        var birthUtc = TimeZoneInfo.ConvertTimeToUtc(
            DateTime.SpecifyKind(birthLocal, DateTimeKind.Unspecified),
            IndiaTimeZone);
        var planets = _chartService.GenerateChart(
            birthUtc,
            Convert.ToDouble(kundali.Latitude.Value),
            Convert.ToDouble(kundali.Longitude.Value));
        var moon = planets.First(item => item.Planet == Planet.Moon);
        var birthDasha = _dashaService.GetBirthMahadasha(moon);
        var current = _dashaService.CalculateCurrentDasha(
            birthDasha.Planet,
            birthDasha.RemainingYears,
            birthUtc,
            DateTime.UtcNow);

        return new CurrentDashaDto
        {
            Mahadasha = current.CurrentMahadasha.Planet.ToString(),
            Antardasha = current.CurrentAntardasha.SubPlanet.ToString(),
            StartDate = current.CurrentMahadasha.StartDate,
            EndDate = current.CurrentMahadasha.EndDate,
            AntardashaStartDate = current.CurrentAntardasha.StartDate,
            AntardashaEndDate = current.CurrentAntardasha.EndDate,
            Summary = string.Empty
        };
    }

    private static SavedKundaliListItemDto MapSavedKundali(Kundali kundali)
    {
        return new SavedKundaliListItemDto
        {
            Id = kundali.Id,
            FullName = kundali.FullName,
            DateOfBirth = kundali.DateOfBirth,
            TimeOfBirth = kundali.TimeOfBirth,
            BirthPlace = kundali.BirthPlace,
            CreatedAt = kundali.CreatedAt,
            DisplayName = $"{kundali.FullName} - {kundali.DateOfBirth:yyyy-MM-dd} - {kundali.BirthPlace}"
        };
    }

    private static DashboardRecentReportDto MapRecentReport(KundaliReport report)
    {
        return new DashboardRecentReportDto
        {
            Id = report.Id,
            KundaliId = report.KundaliId,
            ReportType = report.ReportType,
            ReportStatus = report.ReportStatus,
            KundaliName = report.Kundali?.FullName ?? string.Empty,
            CreatedAt = report.CreatedAt
        };
    }

    private static IReadOnlyList<DashboardMuhurthaPreviewDto> BuildUpcomingMuhurthas()
    {
        var today = DateTime.Today;
        return new List<DashboardMuhurthaPreviewDto>
        {
            new() { Date = today.AddDays(6), Title = "Griha Pravesh", TimeWindow = "Morning window", Quality = "Excellent" },
            new() { Date = today.AddDays(13), Title = "Vehicle Purchase", TimeWindow = "Late morning", Quality = "Good" },
            new() { Date = today.AddDays(17), Title = "Annaprashan", TimeWindow = "Before noon", Quality = "Good" }
        };
    }

    private static IReadOnlyList<DashboardInsightDto> BuildDailyInsights(
        DashboardPanchangDto panchang,
        LearningLevelSummary? learningSummary)
    {
        var learningTitle = learningSummary?.Title ?? "Jyotish foundations";
        var learningText = learningSummary is null
            ? "Open structured lessons to build a steady Jyotish study habit."
            : $"{learningSummary.SessionCount} sessions, about {learningSummary.EstimatedHours} hours of structured study.";

        return new List<DashboardInsightDto>
        {
            new()
            {
                Eyebrow = "Panchang Insight",
                Title = $"{panchang.Tithi} under {panchang.Nakshatra}",
                Text = "Use the day context before planning rituals, travel, purchases, or important conversations.",
                RouterLink = "/app/panchang",
                Action = "View Panchang",
                Tone = "moon"
            },
            new()
            {
                Eyebrow = "Learning Tip",
                Title = learningTitle,
                Text = learningText,
                RouterLink = "/app/learning",
                Action = "Open Study",
                Tone = "study"
            },
            new()
            {
                Eyebrow = "Muhurtha Focus",
                Title = "Plan with cleaner timing",
                Text = "Use Muhurtha before major starts: home, vehicle, ceremonies, business work, and travel.",
                RouterLink = "/app/panchang/muhurtha",
                Action = "Find Muhurtha",
                Tone = "muhurtha"
            }
        };
    }
}
