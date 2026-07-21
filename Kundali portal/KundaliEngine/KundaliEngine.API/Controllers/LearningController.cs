using KundaliEngine.API.Services.Learning;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace KundaliEngine.API.Controllers;

[ApiController]
[Authorize]
[Route("api/learning")]
public sealed class LearningController : ControllerBase
{
    private readonly ILearningContentService _learningContentService;
    private readonly ILogger<LearningController> _logger;

    public LearningController(
        ILearningContentService learningContentService,
        ILogger<LearningController> logger)
    {
        _learningContentService = learningContentService;
        _logger = logger;
    }

    [HttpGet("levels")]
    public async Task<IActionResult> GetLevels(CancellationToken cancellationToken)
    {
        var levels = await _learningContentService.GetLevelsAsync(cancellationToken);
        return Ok(levels);
    }

    [HttpGet("levels/{levelCode}")]
    public async Task<IActionResult> GetLevel(string levelCode, CancellationToken cancellationToken)
    {
        var level = await _learningContentService.GetLevelAsync(levelCode, cancellationToken);
        return level is null
            ? NotFound(new { message = $"Learning level '{levelCode}' was not found." })
            : Ok(level);
    }

    [HttpGet("sessions/{sessionGuid:guid}")]
    public async Task<IActionResult> GetSession(
        Guid sessionGuid,
        [FromQuery] string? language,
        CancellationToken cancellationToken)
    {
        Response.Headers.CacheControl = "private, no-store";
        Response.Headers.Pragma = "no-cache";
        Response.Headers.Expires = "0";

        try
        {
            var response = await _learningContentService.GetSessionAsync(sessionGuid, language, cancellationToken);
            return response is null
                ? NotFound(new { message = "Learning session was not found." })
                : Ok(response);
        }
        catch (InvalidOperationException ex)
        {
            _logger.LogWarning(ex, "Learning session {SessionGuid} could not be returned.", sessionGuid);
            return BadRequest(new { message = ex.Message });
        }
    }
}
