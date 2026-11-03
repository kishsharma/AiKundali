using System.Security.Claims;
using KundaliEngine.API.DTOs.Requests;
using KundaliEngine.API.DTOs.Responses;
using KundaliEngine.API.Services.Horoscope;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace KundaliEngine.API.Controllers;

[Authorize]
[Route("api/horoscope")]
[ApiController]
public sealed class HoroscopeController : ControllerBase
{
    private readonly IHoroscopePeriodService _horoscopePeriodService;
    private readonly IHoroscopeRashifalService _horoscopeRashifalService;
    private readonly ILogger<HoroscopeController> _logger;

    public HoroscopeController(
        IHoroscopePeriodService horoscopePeriodService,
        IHoroscopeRashifalService horoscopeRashifalService,
        ILogger<HoroscopeController> logger)
    {
        _horoscopePeriodService = horoscopePeriodService;
        _horoscopeRashifalService = horoscopeRashifalService;
        _logger = logger;
    }

    [HttpGet("rashifal/{horizon}")]
    public ActionResult<HoroscopeRashifalResponse> Rashifal(
        string horizon,
        [FromQuery] DateOnly? anchorDate = null,
        [FromQuery] string locale = "en")
    {
        try
        {
            var parsed = ParseHorizon(horizon);
            var date = anchorDate ?? DateOnly.FromDateTime(DateTime.UtcNow);
            return Ok(_horoscopeRashifalService.Generate(parsed, date, locale));
        }
        catch (ArgumentException ex)
        {
            _logger.LogWarning(ex, "Horoscope rashifal validation failed.");
            return BadRequest(ex.Message);
        }
    }

    private static HoroscopeHorizon ParseHorizon(string horizon) =>
        horizon?.Trim().ToLowerInvariant() switch
        {
            "daily" => HoroscopeHorizon.Daily,
            "weekly" => HoroscopeHorizon.Weekly,
            "monthly" => HoroscopeHorizon.Monthly,
            _ => throw new ArgumentException("horizon must be daily, weekly, or monthly.")
        };

    [HttpPost("period")]
    public Task<ActionResult<HoroscopePeriodResponse>> Period([FromBody] HoroscopePeriodRequest request, CancellationToken cancellationToken)
        => Generate(request, cancellationToken);

    [HttpPost("daily")]
    public Task<ActionResult<HoroscopePeriodResponse>> Daily([FromBody] HoroscopePeriodRequest request, CancellationToken cancellationToken)
    {
        request.Horizon = HoroscopeHorizon.Daily;
        return Generate(request, cancellationToken);
    }

    [HttpPost("weekly")]
    public Task<ActionResult<HoroscopePeriodResponse>> Weekly([FromBody] HoroscopePeriodRequest request, CancellationToken cancellationToken)
    {
        request.Horizon = HoroscopeHorizon.Weekly;
        return Generate(request, cancellationToken);
    }

    [HttpPost("monthly")]
    public Task<ActionResult<HoroscopePeriodResponse>> Monthly([FromBody] HoroscopePeriodRequest request, CancellationToken cancellationToken)
    {
        request.Horizon = HoroscopeHorizon.Monthly;
        return Generate(request, cancellationToken);
    }

    private async Task<ActionResult<HoroscopePeriodResponse>> Generate(
        HoroscopePeriodRequest request,
        CancellationToken cancellationToken)
    {
        try
        {
            var response = await _horoscopePeriodService.GenerateAsync(
                request,
                GetCurrentUserId(),
                cancellationToken);
            return Ok(response);
        }
        catch (KeyNotFoundException ex)
        {
            _logger.LogWarning(ex, "Horoscope request failed because Kundali was not found.");
            return NotFound(ex.Message);
        }
        catch (ArgumentException ex)
        {
            _logger.LogWarning(ex, "Horoscope request validation failed.");
            return BadRequest(ex.Message);
        }
        catch (InvalidOperationException ex)
        {
            _logger.LogWarning(ex, "Horoscope request could not be completed.");
            return BadRequest(ex.Message);
        }
    }

    private Guid? GetCurrentUserId()
    {
        var value = User.FindFirstValue(ClaimTypes.NameIdentifier) ?? User.FindFirstValue("sub");
        return Guid.TryParse(value, out var userId) ? userId : null;
    }
}
