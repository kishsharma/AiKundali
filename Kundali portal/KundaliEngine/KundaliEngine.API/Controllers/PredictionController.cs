using HoraLife.PredictionEngine.Enums;
using HoraLife.PredictionEngine.Models;
using HoraLife.PredictionEngine.Services;
using KundaliEngine.API.Prediction.DTOs;
using KundaliEngine.API.Prediction.Mappers;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace KundaliEngine.API.Controllers;

[Route("api/[controller]")]
[ApiController]
public class PredictionController : ControllerBase
{
    private readonly PredictionContextMapper _predictionContextMapper;
    private readonly PredictionEngineService _predictionEngineService;

    public PredictionController(
        PredictionContextMapper predictionContextMapper,
        PredictionEngineService predictionEngineService)
    {
        _predictionContextMapper = predictionContextMapper;
        _predictionEngineService = predictionEngineService;
    }

    [Authorize]
    [HttpPost("question")]
    [ProducesResponseType(typeof(PredictionResponseDto), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    public ActionResult<PredictionResponseDto> AskQuestion(
        [FromBody] PredictionQuestionRequestDto request)
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

        var predictionContext = _predictionContextMapper.Map(request.KundaliData!);
        var normalizedFocusKey = request.FocusKey.Trim().ToLowerInvariant();
        var referenceDate = request.ReferenceDate ?? DateTimeOffset.UtcNow;

        var predictionRequest = new PredictionRequest
        {
            Mode = PredictionMode.CustomQuestion,
            LifeArea = request.LifeArea,
            FocusKey = normalizedFocusKey,
            TimeScope = request.TimeScope,
            ReferenceDate = referenceDate
        };

        var result = _predictionEngineService.Generate(
            predictionRequest,
            predictionContext);

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

        return (request.LifeArea, normalizedFocusKey) switch
        {
            (PredictionLifeArea.Relationships, "marriage_possibility") => null,
            (PredictionLifeArea.Relationships, "marriage_timing") => null,
            (PredictionLifeArea.Relationships, _) =>
                "Unsupported FocusKey for Relationships. Supported values: marriage_possibility, marriage_timing.",
            _ =>
                "CustomQuestion support is currently available only for Relationships with FocusKey marriage_possibility or marriage_timing."
        };
    }
}
