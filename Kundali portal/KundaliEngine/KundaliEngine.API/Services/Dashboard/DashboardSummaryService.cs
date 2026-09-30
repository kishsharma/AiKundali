using KundaliEngine.API.DTOs.Learning;
using KundaliEngine.API.DTOs.Responses;
using KundaliEngine.API.Services.Entitlements;
using KundaliEngine.API.Services.Learning;
using KundaliEngine.Astrology.Services.Astrology;
using KundaliEngine.Astrology.Services.Dasha;
using KundaliEngine.Astrology.Services.Panchang;
using KundaliEngine.Domain.Entities;
using KundaliEngine.Domain.Enums;
using KundaliEngine.Domain.Subscriptions;
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
    private readonly IEntitlementService _entitlementService;

    public DashboardSummaryService(
        KundaliDbContext dbContext,
        ChartService chartService,
        PanchangService panchangService,
        DashaService dashaService,
        ILearningContentService learningContentService,
        IEntitlementService entitlementService)
    {
        _dbContext = dbContext;
        _chartService = chartService;
        _panchangService = panchangService;
        _dashaService = dashaService;
        _learningContentService = learningContentService;
        _entitlementService = entitlementService;
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
        var usage = await BuildUsageStatsAsync(user, kundalis.Count, savedCompatibilityCount, cancellationToken);

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
            CollectionStats = usage,
            LatestKundali = latestKundali is null ? null : MapSavedKundali(latestKundali),
            CurrentDasha = latestKundali is null ? null : BuildCurrentDasha(latestKundali),
            LearningSummary = learningLevels.FirstOrDefault(),
            LearningLevels = learningLevels,
            RecentReport = reports.FirstOrDefault() is { } recentReport ? MapRecentReport(recentReport) : null,
            UpcomingMuhurthas = Array.Empty<DashboardMuhurthaPreviewDto>(),
            DailyInsights = BuildDailyInsights(panchang, learningLevels.FirstOrDefault())
        };
    }

    private async Task<DashboardCollectionStatsDto> BuildUsageStatsAsync(
        User user,
        int savedKundalis,
        int savedMatches,
        CancellationToken cancellationToken)
    {
        var planCode = _entitlementService.GetEffectivePlanCode(user);
        var emailIncluded = PlanCodes.IsPaid(planCode);
        var kundaliSaveLimit = Math.Max(0, user.KundaliSaveLimit);
        var matchSaveLimit = Math.Max(0, _entitlementService.GetMatchSaveLimit(user));
        var dayStartUtc = IndiaDayStartUtc();

        var pdfGenerated = await _dbContext.AuditLogs.CountAsync(log =>
            log.UserId == user.Id
            && log.Action == "PdfExported"
            && log.Status == "Success", cancellationToken);
        var pdfUsedToday = await _dbContext.AuditLogs.CountAsync(log =>
            log.UserId == user.Id
            && log.Action == "PdfExported"
            && log.Status == "Success"
            && log.CreatedAt >= dayStartUtc, cancellationToken);
        var emailsSent = await _dbContext.CommunicationLogs.CountAsync(log =>
            log.TriggeredByUserId == user.Id
            && log.Channel == "Email"
            && log.Status == "Sent", cancellationToken);

        int? pdfDailyLimit = emailIncluded ? null : 1;
        int? pdfLeftToday = pdfDailyLimit is null ? null : Math.Max(0, pdfDailyLimit.Value - pdfUsedToday);

        return new DashboardCollectionStatsDto
        {
            PlanCode = planCode,
            SavedKundalis = savedKundalis,
            KundaliSaveLimit = kundaliSaveLimit,
            KundaliSavesLeft = Math.Max(0, kundaliSaveLimit - savedKundalis),
            SavedMatches = savedMatches,
            MatchSaveLimit = matchSaveLimit,
            MatchSavesLeft = Math.Max(0, matchSaveLimit - savedMatches),
            PdfGenerated = pdfGenerated,
            PdfUsedToday = pdfUsedToday,
            PdfDailyLimit = pdfDailyLimit,
            PdfLeftToday = pdfLeftToday,
            EmailsSent = emailsSent,
            EmailIncluded = emailIncluded
        };
    }

    private static DateTime IndiaDayStartUtc()
    {
        var indiaNow = DateTime.UtcNow.AddHours(5.5);
        return DateTime.SpecifyKind(indiaNow.Date.AddHours(-5.5), DateTimeKind.Utc);
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
