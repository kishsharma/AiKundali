using System.Security.Claims;
using KundaliEngine.API.DTOs.Requests;
using KundaliEngine.API.DTOs.Responses;
using KundaliEngine.API.Services.CurrentLifeFocus;
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
    private readonly IRetrogradeTransitService _retrogradeTransitService;
    private readonly ILogger<GocharController> _logger;

    public GocharController(
        ICurrentLifeFocusService currentLifeFocusService,
        IFocusedLifeAreaAnalysisService focusedLifeAreaAnalysisService,
        IRetrogradeTransitService retrogradeTransitService,
        ILogger<GocharController> logger)
    {
        _currentLifeFocusService = currentLifeFocusService;
        _focusedLifeAreaAnalysisService = focusedLifeAreaAnalysisService;
        _retrogradeTransitService = retrogradeTransitService;
        _logger = logger;
    }

    [HttpGet("retrogrades/current")]
    public ActionResult<CurrentRetrogradesResponse> CurrentRetrogrades([FromQuery] bool includeNodes = false)
    {
        return Ok(_retrogradeTransitService.GetCurrentRetrogrades(includeNodes));
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

    private Guid? GetCurrentUserId()
    {
        var claimValue = User.FindFirstValue(ClaimTypes.NameIdentifier);
        return Guid.TryParse(claimValue, out var userId) ? userId : null;
    }
}
