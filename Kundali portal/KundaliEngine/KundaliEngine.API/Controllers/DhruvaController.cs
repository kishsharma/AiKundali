using System.Security.Claims;
using KundaliEngine.API.Services.Admin;
using KundaliEngine.API.Services.Dhruva;
using KundaliEngine.Infrastructure.Data;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http.Timeouts;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace KundaliEngine.API.Controllers;

[ApiController]
[Authorize]
[Route("api/dhruva")]
public class DhruvaController : ControllerBase
{
    private readonly IDhruvaNarrationService _narrationService;
    private readonly IDhruvaUsageService _dhruvaUsage;
    private readonly IUserAnalysisService _userAnalysis;
    private readonly KundaliDbContext _dbContext;

    public DhruvaController(
        IDhruvaNarrationService narrationService,
        IDhruvaUsageService dhruvaUsage,
        IUserAnalysisService userAnalysis,
        KundaliDbContext dbContext)
    {
        _narrationService = narrationService;
        _dhruvaUsage = dhruvaUsage;
        _userAnalysis = userAnalysis;
        _dbContext = dbContext;
    }

    [HttpGet("allowance")]
    public async Task<ActionResult<DhruvaAllowance>> GetAllowance(CancellationToken cancellationToken)
    {
        var user = await GetCurrentUserAsync(cancellationToken);
        if (user is null)
        {
            return Unauthorized();
        }

        return Ok(await _dhruvaUsage.GetAllowanceAsync(user, cancellationToken));
    }

    [HttpPost("narrate")]
    [RequestTimeout("DhruvaNarration")]
    public async Task<ActionResult<DhruvaNarrateResponseDto>> Narrate(
        [FromBody] DhruvaNarrateRequestDto request,
        CancellationToken cancellationToken)
    {
        if (!DhruvaNarrationService.IsSupportedMode(request.Mode))
        {
            return BadRequest(new { message = "Choose janma_story, dasha_focus, compatibility, or qa." });
        }

        if (request.Chart.ValueKind is System.Text.Json.JsonValueKind.Undefined or System.Text.Json.JsonValueKind.Null)
        {
            return BadRequest(new { message = "A calculated chart is required." });
        }

        if (string.Equals(request.Mode, "qa", StringComparison.OrdinalIgnoreCase)
            && string.IsNullOrWhiteSpace(request.Question))
        {
            return BadRequest(new { message = "A question is required." });
        }

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
                message = "AI mode is included with Premium and Premium Plus."
            });
        }

        if (!await _dhruvaUsage.TryConsumeLifeQuestionAsync(user, cancellationToken))
        {
            return StatusCode(StatusCodes.Status403Forbidden, new
            {
                message = DhruvaUsageService.LifeQuestionLimitMessage(allowance.LifeQuestionDailyLimit ?? DhruvaUsageService.FreeLifeQuestionsPerDay)
            });
        }

        request.Locale = DhruvaNarrationService.ResolveResponseLanguage(user.PreferredLanguage, request.Locale);

        try
        {
            var narration = await _narrationService.NarrateAsync(request, cancellationToken);
            await RecordQuestionAsync(user.Id, true, request.Mode, request.Question, narration.PromptTokens, narration.CompletionTokens, narration.TotalTokens, cancellationToken);
            return Ok(narration);
        }
        catch (DhruvaNarrationUnavailableException)
        {
            return StatusCode(StatusCodes.Status502BadGateway, new { message = "Chart narration is unavailable right now." });
        }
    }

    private async Task RecordQuestionAsync(
        Guid userId,
        bool isAiMode,
        string kind,
        string? question,
        int promptTokens,
        int completionTokens,
        int totalTokens,
        CancellationToken cancellationToken)
    {
        try
        {
            await _userAnalysis.RecordQuestionAsync(userId, isAiMode, kind, question, promptTokens, completionTokens, totalTokens, cancellationToken);
        }
        catch (Exception exception)
        {
            Console.Error.WriteLine($"Dhruva question log failed: {exception.Message}");
        }
    }

    private async Task<KundaliEngine.Domain.Entities.User?> GetCurrentUserAsync(CancellationToken cancellationToken)
    {
        var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
        if (!Guid.TryParse(userId, out var parsedUserId))
        {
            return null;
        }

        var user = await _dbContext.Users.FirstOrDefaultAsync(item => item.Id == parsedUserId, cancellationToken);
        return user == null || !user.IsActive ? null : user;
    }
}
