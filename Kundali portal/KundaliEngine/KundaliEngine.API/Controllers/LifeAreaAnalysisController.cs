using System.Security.Claims;
using KundaliEngine.API.DTOs.LifeArea;
using KundaliEngine.API.Services.Auth;
using KundaliEngine.API.Services.Dhruva;
using KundaliEngine.API.Services.LifeArea;
using KundaliEngine.Astrology.Astronomy;
using KundaliEngine.Astrology.Services.Astrology;
using KundaliEngine.Astrology.Services.Dasha;
using KundaliEngine.Domain.Enums;
using KundaliEngine.Domain.Models;
using KundaliEngine.Infrastructure.Data;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http.Timeouts;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace KundaliEngine.API.Controllers;

[ApiController]
[Authorize]
[Route("api/life-area-analysis")]
public class LifeAreaAnalysisController : ControllerBase
{
    private static readonly TimeZoneInfo IndiaTimeZone = TimeZoneInfo.FindSystemTimeZoneById("India Standard Time");

    private readonly KundaliDbContext _dbContext;
    private readonly LifeAreaAnalysisService _analysis;
    private readonly LifeAreaInsightService _insight;
    private readonly IDhruvaUsageService _dhruvaUsage;
    private readonly ChartService _chartService;
    private readonly IEphemerisProvider _ephemerisProvider;
    private readonly DashaService _dashaService;

    public LifeAreaAnalysisController(
        KundaliDbContext dbContext,
        LifeAreaAnalysisService analysis,
        LifeAreaInsightService insight,
        IDhruvaUsageService dhruvaUsage,
        ChartService chartService,
        IEphemerisProvider ephemerisProvider,
        DashaService dashaService)
    {
        _dbContext = dbContext;
        _analysis = analysis;
        _insight = insight;
        _dhruvaUsage = dhruvaUsage;
        _chartService = chartService;
        _ephemerisProvider = ephemerisProvider;
        _dashaService = dashaService;
    }

    [HttpGet("areas")]
    public async Task<ActionResult<IReadOnlyList<LifeAreaCatalogItemDto>>> Areas(
        [FromQuery] string? locale,
        CancellationToken cancellationToken)
    {
        var user = await GetCurrentUserAsync(cancellationToken);
        if (user is null)
        {
            return Unauthorized();
        }

        return Ok(_analysis.GetCatalog(locale ?? user.PreferredLanguage));
    }

    [HttpGet("{kundaliId:guid}")]
    public async Task<ActionResult<LifeAreaReportDto>> Report(
        Guid kundaliId,
        [FromQuery] string area,
        [FromQuery] string? locale,
        CancellationToken cancellationToken)
    {
        var read = await ReadReportAsync(kundaliId, area, locale, cancellationToken);
        return read.Report is null ? ToError(read) : Ok(read.Report);
    }

    [HttpPost("{kundaliId:guid}/insight")]
    [RequestTimeout("DhruvaNarration")]
    public async Task<ActionResult<LifeAreaInsightDto>> Insight(
        Guid kundaliId,
        [FromQuery] string area,
        [FromQuery] string? locale,
        CancellationToken cancellationToken)
    {
        var user = await GetCurrentUserAsync(cancellationToken);
        if (user is null)
        {
            return Unauthorized();
        }

        var allowance = await _dhruvaUsage.GetAllowanceAsync(user, cancellationToken);
        if (!allowance.DhruvaAiEnabled)
        {
            return StatusCode(StatusCodes.Status403Forbidden, new
            {
                message = "AI insights are included with Premium and Premium Plus."
            });
        }

        var read = await ReadReportAsync(kundaliId, area, locale, cancellationToken);
        if (read.Report is null)
        {
            return ToError(read);
        }

        try
        {
            var insight = await _insight.WriteAsync(read.Report, read.Language, cancellationToken);
            return Ok(new LifeAreaInsightDto { Insight = insight });
        }
        catch (DhruvaNarrationUnavailableException)
        {
            return StatusCode(StatusCodes.Status502BadGateway, new { message = "A personal note is unavailable right now." });
        }
    }

    private async Task<ReportRead> ReadReportAsync(
        Guid kundaliId,
        string? area,
        string? locale,
        CancellationToken cancellationToken)
    {
        if (string.IsNullOrWhiteSpace(area))
        {
            return new ReportRead(400, "A life area is required.", null, "en");
        }

        var user = await GetCurrentUserAsync(cancellationToken);
        if (user is null)
        {
            return new ReportRead(401, null, null, "en");
        }

        var kundali = await _dbContext.Kundalis
            .AsNoTracking()
            .FirstOrDefaultAsync(item => item.Id == kundaliId && item.UserId == user.Id, cancellationToken);
        if (kundali is null)
        {
            return new ReportRead(404, null, null, "en");
        }

        if (kundali.Latitude is null || kundali.Longitude is null)
        {
            return new ReportRead(400, "This chart is missing a birth location.", null, "en");
        }

        var birthLocal = kundali.DateOfBirth.ToDateTime(kundali.TimeOfBirth);
        var birthUtc = TimeZoneInfo.ConvertTimeToUtc(DateTime.SpecifyKind(birthLocal, DateTimeKind.Unspecified), IndiaTimeZone);
        var latitude = Convert.ToDouble(kundali.Latitude.Value);
        var longitude = Convert.ToDouble(kundali.Longitude.Value);
        var planets = _chartService.GenerateChart(birthUtc, latitude, longitude);
        var ascendantLongitude = _ephemerisProvider.GetAscendant(birthUtc, new GeoLocation(latitude, longitude)).Longitude;
        var normalized = NormalizeLongitude(ascendantLongitude);
        var ascendantSign = (ZodiacSign)((int)(normalized / 30.0) + 1);
        var ascendantDegree = normalized % 30.0;
        var chart = new KundaliChart
        {
            AscendantSign = ascendantSign,
            AscendantDegree = (int)Math.Floor(ascendantDegree),
            PlanetPositions = planets
        };

        LifeAreaTiming? timing = null;
        var moon = planets.FirstOrDefault(item => item.Planet == Planet.Moon);
        if (moon is not null)
        {
            var birthDasha = _dashaService.GetBirthMahadasha(moon);
            var current = _dashaService.CalculateCurrentDasha(birthDasha.Planet, birthDasha.RemainingYears, birthUtc, DateTime.UtcNow);
            timing = new LifeAreaTiming
            {
                Mahadasha = current.CurrentMahadasha?.Planet.ToString(),
                Antardasha = current.CurrentAntardasha?.SubPlanet.ToString(),
                Windows = current.CurrentPratyantardashaTimeline
                    .Where(item => item.EndDate > DateTime.UtcNow)
                    .OrderBy(item => item.StartDate)
                    .Take(8)
                    .Select(item => new LifeAreaWindow
                    {
                        Planet = item.InnerPlanet.ToString(),
                        Start = item.StartDate,
                        End = item.EndDate
                    })
                    .ToList()
            };
        }

        var language = PreferredLanguageHelper.Normalize(locale) ?? PreferredLanguageHelper.Normalize(user.PreferredLanguage) ?? "en";
        try
        {
            return new ReportRead(200, null, _analysis.Analyze(chart, ascendantDegree, area, language, timing), language);
        }
        catch (ArgumentException ex)
        {
            return new ReportRead(400, ex.Message, null, language);
        }
    }

    private ActionResult ToError(ReportRead read) => read.Status switch
    {
        401 => Unauthorized(),
        404 => NotFound(),
        _ => BadRequest(new { message = read.Message })
    };

    private async Task<KundaliEngine.Domain.Entities.User?> GetCurrentUserAsync(CancellationToken cancellationToken)
    {
        var claim = User.FindFirstValue(ClaimTypes.NameIdentifier);
        if (!Guid.TryParse(claim, out var userId))
        {
            return null;
        }

        var user = await _dbContext.Users.AsNoTracking().FirstOrDefaultAsync(item => item.Id == userId, cancellationToken);
        return user is { IsActive: true } ? user : null;
    }

    private sealed record ReportRead(int Status, string? Message, LifeAreaReportDto? Report, string Language);

    private static double NormalizeLongitude(double longitude)
    {
        var normalized = longitude % 360.0;
        return normalized < 0 ? normalized + 360.0 : normalized;
    }
}
