using System.Security.Claims;
using KundaliEngine.API.DTOs.Requests;
using KundaliEngine.API.DTOs.Responses;
using KundaliEngine.API.Services.CurrentLifeFocus;
using KundaliEngine.Astrology.Astronomy;
using KundaliEngine.Domain.Enums;
using KundaliEngine.Domain.Models;
using KundaliEngine.API.Services.Gochar;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace KundaliEngine.API.Controllers;

[Authorize]
[Route("api/gochar")]
[ApiController]
public class GocharController : ControllerBase
{
    private readonly ICurrentLifeFocusService _currentLifeFocusService;
    private readonly IFocusedLifeAreaAnalysisService _focusedLifeAreaAnalysisService;
    private const double DefaultLatitude = 12.9716;
    private const double DefaultLongitude = 77.5946;
    private const string DefaultTimezone = "Asia/Kolkata";

    private readonly IRetrogradeTransitService _retrogradeTransitService;
    private readonly IEphemerisProvider _ephemerisProvider;
    private readonly ILogger<GocharController> _logger;

    public GocharController(
        ICurrentLifeFocusService currentLifeFocusService,
        IFocusedLifeAreaAnalysisService focusedLifeAreaAnalysisService,
        IRetrogradeTransitService retrogradeTransitService,
        IEphemerisProvider ephemerisProvider,
        ILogger<GocharController> logger)
    {
        _currentLifeFocusService = currentLifeFocusService;
        _focusedLifeAreaAnalysisService = focusedLifeAreaAnalysisService;
        _retrogradeTransitService = retrogradeTransitService;
        _ephemerisProvider = ephemerisProvider;
        _logger = logger;
    }

    [HttpGet("retrogrades/current")]
    public ActionResult<CurrentRetrogradesResponse> CurrentRetrogrades([FromQuery] bool includeNodes = false)
    {
        return Ok(_retrogradeTransitService.GetCurrentRetrogrades(includeNodes));
    }



    [HttpGet("daily")]
    public ActionResult<GocharDailyResponse> Daily(
        [FromQuery] DateOnly? date = null,
        [FromQuery] double latitude = DefaultLatitude,
        [FromQuery] double longitude = DefaultLongitude,
        [FromQuery] string timezone = DefaultTimezone)
    {
        if (!IsValidLocation(latitude, longitude))
        {
            return BadRequest("Latitude must be between -90 and 90, and longitude must be between -180 and 180.");
        }

        var selectedDate = date ?? DateOnly.FromDateTime(DateTime.Today);
        var localDateTime = selectedDate.ToDateTime(new TimeOnly(6, 0));
        var utc = ConvertLocalToUtc(localDateTime, timezone);
        var location = new GeoLocation(latitude, longitude);
        var planets = _ephemerisProvider
            .GetAllPlanets(utc, location)
            .OrderBy(position => GetPlanetOrder(position.Planet))
            .Select(MapPlanet)
            .ToList();

        return Ok(new GocharDailyResponse
        {
            Date = selectedDate,
            CalculatedAtUtc = utc,
            Latitude = latitude,
            Longitude = longitude,
            Timezone = timezone,
            Ascendant = "1 - Aries",
            Planets = planets,
            Transitions = BuildTransitions(selectedDate, location, timezone)
        });
    }

    [HttpPost("current-life-focus")]
    public async Task<ActionResult<CurrentLifeFocusResponse>> CurrentLifeFocus(
        [FromBody] CurrentLifeFocusRequest request,
        CancellationToken cancellationToken)
    {
        try
        {
            var response = await _currentLifeFocusService.GenerateAsync(
                request,
                GetCurrentUserId(),
                cancellationToken);

            return Ok(response);
        }
        catch (KeyNotFoundException ex)
        {
            _logger.LogWarning(ex, "Current Life Focus request failed because Kundali was not found.");
            return NotFound(ex.Message);
        }
        catch (ArgumentException ex)
        {
            _logger.LogWarning(ex, "Current Life Focus request validation failed.");
            return BadRequest(ex.Message);
        }
        catch (InvalidOperationException ex)
        {
            _logger.LogWarning(ex, "Current Life Focus request could not be completed.");
            return BadRequest(ex.Message);
        }
    }

    [HttpPost("current-life-focus/{analysisId:guid}/focus")]
    public ActionResult<FocusedLifeAreaResponse> Focus(
        Guid analysisId,
        [FromBody] FocusedLifeAreaRequest request)
    {
        try
        {
            return Ok(_focusedLifeAreaAnalysisService.Analyze(
                analysisId,
                GetCurrentUserId(),
                request));
        }
        catch (KeyNotFoundException ex)
        {
            _logger.LogWarning(ex, "Current Life Focus analysis was missing or expired.");
            return NotFound(ex.Message);
        }
        catch (ArgumentException ex)
        {
            _logger.LogWarning(ex, "Current Life Focus focused-analysis validation failed.");
            return BadRequest(ex.Message);
        }
    }



    private List<GocharTransitionDto> BuildTransitions(DateOnly selectedDate, GeoLocation location, string timezone)
    {
        var transitions = new List<GocharTransitionDto>();
        var planets = new[] { Planet.Sun, Planet.Moon, Planet.Mercury, Planet.Venus, Planet.Mars, Planet.Jupiter, Planet.Saturn, Planet.Rahu, Planet.Ketu };
        var startDate = selectedDate.AddDays(-30);
        var endDate = selectedDate.AddDays(45);

        foreach (var planet in planets)
        {
            var previousDate = startDate;
            var previousUtc = ConvertLocalToUtc(previousDate.ToDateTime(new TimeOnly(6, 0)), timezone);
            var previousSign = _ephemerisProvider.GetPlanet(planet, previousUtc, location).Sign;

            for (var day = startDate.AddDays(1); day <= endDate; day = day.AddDays(1))
            {
                var currentUtc = ConvertLocalToUtc(day.ToDateTime(new TimeOnly(6, 0)), timezone);
                var currentSign = _ephemerisProvider.GetPlanet(planet, currentUtc, location).Sign;
                if (currentSign == previousSign)
                {
                    previousDate = day;
                    previousSign = currentSign;
                    continue;
                }

                var ingress = RefineSignIngress(planet, previousDate, day, previousSign, location, timezone);
                transitions.Add(new GocharTransitionDto
                {
                    Planet = planet.ToString(),
                    FromSign = previousSign.ToString(),
                    ToSign = currentSign.ToString(),
                    IngressDate = DateOnly.FromDateTime(ingress.DateTime),
                    IngressTime = ingress.ToString("HH:mm"),
                    IsRetrograde = _ephemerisProvider.GetPlanet(planet, ingress.UtcDateTime, location).IsRetrograde
                });

                previousDate = day;
                previousSign = currentSign;
            }
        }

        return transitions
            .OrderBy(item => item.IngressDate)
            .ThenBy(item => item.IngressTime)
            .ThenBy(item => GetPlanetOrder(Enum.Parse<Planet>(item.Planet)))
            .ToList();
    }

    private DateTimeOffset RefineSignIngress(
        Planet planet,
        DateOnly beforeDate,
        DateOnly afterDate,
        ZodiacSign fromSign,
        GeoLocation location,
        string timezone)
    {
        var offset = GetTimezoneOffset(beforeDate.ToDateTime(new TimeOnly(6, 0)), timezone);
        var start = new DateTimeOffset(beforeDate.ToDateTime(new TimeOnly(6, 0)), offset);
        var end = new DateTimeOffset(afterDate.ToDateTime(new TimeOnly(6, 0)), offset);

        for (var i = 0; i < 24; i++)
        {
            var midpoint = new DateTimeOffset(start.Ticks + ((end.Ticks - start.Ticks) / 2), start.Offset);
            var sign = _ephemerisProvider.GetPlanet(planet, midpoint.UtcDateTime, location).Sign;
            if (sign == fromSign)
            {
                start = midpoint;
            }
            else
            {
                end = midpoint;
            }
        }

        return end;
    }

    private static GocharPlanetDto MapPlanet(PlanetPosition position)
    {
        return new GocharPlanetDto
        {
            Planet = position.Planet.ToString(),
            PlanetName = position.Planet.ToString(),
            Sign = $"{(int)position.Sign} - {position.Sign}",
            SignName = position.Sign.ToString(),
            House = (int)position.Sign,
            Degree = position.Degree,
            Minute = position.Minute,
            DegreeInSign = Math.Round(position.DegreeInSign, 4),
            Longitude = Math.Round(position.Longitude, 6),
            Nakshatra = position.Nakshatra.ToString(),
            Pada = position.Pada,
            IsRetrograde = position.IsRetrograde
        };
    }

    private static int GetPlanetOrder(Planet planet)
    {
        return planet switch
        {
            Planet.Sun => 1,
            Planet.Moon => 2,
            Planet.Mars => 3,
            Planet.Mercury => 4,
            Planet.Jupiter => 5,
            Planet.Venus => 6,
            Planet.Saturn => 7,
            Planet.Rahu => 8,
            Planet.Ketu => 9,
            _ => 99
        };
    }

    private static DateTime ConvertLocalToUtc(DateTime localDateTime, string timezone)
    {
        try
        {
            var timezoneInfo = TimeZoneInfo.FindSystemTimeZoneById(timezone);
            return TimeZoneInfo.ConvertTimeToUtc(DateTime.SpecifyKind(localDateTime, DateTimeKind.Unspecified), timezoneInfo);
        }
        catch (TimeZoneNotFoundException)
        {
            return localDateTime.AddHours(-5.5);
        }
        catch (InvalidTimeZoneException)
        {
            return localDateTime.AddHours(-5.5);
        }
    }

    private static TimeSpan GetTimezoneOffset(DateTime localDateTime, string timezone)
    {
        try
        {
            var timezoneInfo = TimeZoneInfo.FindSystemTimeZoneById(timezone);
            return timezoneInfo.GetUtcOffset(DateTime.SpecifyKind(localDateTime, DateTimeKind.Unspecified));
        }
        catch (TimeZoneNotFoundException)
        {
            return TimeSpan.FromHours(5.5);
        }
        catch (InvalidTimeZoneException)
        {
            return TimeSpan.FromHours(5.5);
        }
    }

    private static bool IsValidLocation(double latitude, double longitude)
    {
        return latitude is >= -90 and <= 90 && longitude is >= -180 and <= 180;
    }

    private Guid? GetCurrentUserId()
    {
        var claimValue = User.FindFirstValue(ClaimTypes.NameIdentifier);
        return Guid.TryParse(claimValue, out var userId) ? userId : null;
    }
}


public sealed class GocharDailyResponse
{
    public DateOnly Date { get; set; }
    public DateTime CalculatedAtUtc { get; set; }
    public double Latitude { get; set; }
    public double Longitude { get; set; }
    public string Timezone { get; set; } = string.Empty;
    public string Ascendant { get; set; } = string.Empty;
    public List<GocharPlanetDto> Planets { get; set; } = new();
    public List<GocharTransitionDto> Transitions { get; set; } = new();
}

public sealed class GocharPlanetDto
{
    public string Planet { get; set; } = string.Empty;
    public string PlanetName { get; set; } = string.Empty;
    public string Sign { get; set; } = string.Empty;
    public string SignName { get; set; } = string.Empty;
    public int House { get; set; }
    public int Degree { get; set; }
    public int Minute { get; set; }
    public double DegreeInSign { get; set; }
    public double Longitude { get; set; }
    public string Nakshatra { get; set; } = string.Empty;
    public int Pada { get; set; }
    public bool IsRetrograde { get; set; }
}

public sealed class GocharTransitionDto
{
    public string Planet { get; set; } = string.Empty;
    public string FromSign { get; set; } = string.Empty;
    public string ToSign { get; set; } = string.Empty;
    public DateOnly IngressDate { get; set; }
    public string IngressTime { get; set; } = string.Empty;
    public bool IsRetrograde { get; set; }
}
