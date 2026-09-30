using System.Security.Claims;
using HoraLife.PredictionEngine.Enums;
using HoraLife.PredictionEngine.Models;
using HoraLife.PredictionEngine.Services;
using KundaliEngine.API.Prediction.DTOs;
using KundaliEngine.API.Prediction.Mappers;
using KundaliEngine.API.Services.Admin;
using KundaliEngine.API.Services.Dhruva;
using KundaliEngine.Infrastructure.Data;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace KundaliEngine.API.Controllers;

[Route("api/[controller]")]
[ApiController]
public class PredictionController : ControllerBase
{
    private readonly PredictionContextMapper _predictionContextMapper;
    private readonly PredictionEngineService _predictionEngineService;
    private readonly IDhruvaUsageService _dhruvaUsage;
    private readonly IUserAnalysisService _userAnalysis;
    private readonly KundaliDbContext _dbContext;

    public PredictionController(
        PredictionContextMapper predictionContextMapper,
        PredictionEngineService predictionEngineService,
        IDhruvaUsageService dhruvaUsage,
        IUserAnalysisService userAnalysis,
        KundaliDbContext dbContext)
    {
        _predictionContextMapper = predictionContextMapper;
        _predictionEngineService = predictionEngineService;
        _dhruvaUsage = dhruvaUsage;
        _userAnalysis = userAnalysis;
        _dbContext = dbContext;
    }

    [Authorize]
    [HttpPost("question")]
    [ProducesResponseType(typeof(PredictionResponseDto), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    public async Task<ActionResult<PredictionResponseDto>> AskQuestion(
        [FromBody] PredictionQuestionRequestDto request,
        CancellationToken cancellationToken)
    {
        if (request is null)
        {
            return BadRequest(new
            {
                message = "Request is required."
            });
        }

        var validationError = ValidateRequest(request);
        if (validationError is not null)
        {
            return BadRequest(new
            {
                message = validationError
            });
        }

        var user = await GetCurrentUserAsync(cancellationToken);
        if (user is null)
        {
            return Unauthorized();
        }

        if (!await _dhruvaUsage.TryConsumeLifeQuestionAsync(user, cancellationToken))
        {
            var allowance = await _dhruvaUsage.GetAllowanceAsync(user, cancellationToken);
            return StatusCode(StatusCodes.Status403Forbidden, new
            {
                message = DhruvaUsageService.LifeQuestionLimitMessage(allowance.LifeQuestionDailyLimit ?? DhruvaUsageService.FreeLifeQuestionsPerDay)
            });
        }

        var predictionContext = _predictionContextMapper.Map(request.KundaliData!);
        var normalizedFocusKey = request.FocusKey.Trim().ToLowerInvariant();
        var referenceDate = request.ReferenceDate ?? DateTimeOffset.UtcNow;

        var predictionRequest = new PredictionRequest
        {
            Mode = PredictionMode.CustomQuestion,
            LifeArea = request.LifeArea,
            FocusKey = normalizedFocusKey,
            TimeScope = request.TimeScope,
            ReferenceDate = referenceDate,
            BirthDate = request.BirthDate
        };

        var result = _predictionEngineService.Generate(
            predictionRequest,
            predictionContext);

        try
        {
            await _userAnalysis.RecordQuestionAsync(
                user.Id,
                isAiMode: false,
                kind: "catalog",
                question: null,
                promptTokens: 0,
                completionTokens: 0,
                totalTokens: 0,
                cancellationToken);
        }
        catch (Exception exception)
        {
            Console.Error.WriteLine($"Dhruva question log failed: {exception.Message}");
        }

        return Ok(PredictionResponseMapper.Map(
            result,
            predictionContext.DashaTimeline));
    }

    private static string? ValidateRequest(PredictionQuestionRequestDto request)
    {
        if (request.KundaliData is null)
        {
            return "KundaliData is required.";
        }

        if (string.IsNullOrWhiteSpace(request.FocusKey))
        {
            return "FocusKey is required.";
        }

        if (request.TimeScope == PredictionTimeScope.DateRange)
        {
            return "TimeScope 'DateRange' is not supported for prediction questions in v1.";
        }

        var normalizedFocusKey = request.FocusKey.Trim().ToLowerInvariant();

        return LifeQuestionCatalog.IsSupported(request.LifeArea, normalizedFocusKey)
            ? null
            : "That life question is not supported.";
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
