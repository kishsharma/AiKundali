using System.ComponentModel.DataAnnotations;
using System.Text.Json;
using System.Text.RegularExpressions;
using KundaliEngine.API.Services.Audit;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.RateLimiting;

namespace KundaliEngine.API.Controllers;

[ApiController]
[AllowAnonymous]
[EnableRateLimiting("UsageAnalytics")]
[Route("api/usage/activity-events")]
[RequestSizeLimit(4096)]
public sealed class UserActivityController : ControllerBase
{
    private static readonly HashSet<string> AllowedActions = new(StringComparer.Ordinal)
    {
        "PageViewed",
        "PageVisibleTime",
        "FormStarted",
        "FormAbandoned",
        "OperationStarted",
        "OperationSucceeded",
        "OperationFailed",
        "OperationAbandoned",
        "ReportGeneratedNotSaved"
    };

    private static readonly HashSet<string> AllowedFlows = new(StringComparer.Ordinal)
    {
        "KundaliGeneration",
        "CompatibilityMatch",
        "KundaliForm",
        "CompatibilityForm"
    };

    private readonly IAuditLogService _auditLogService;

    public UserActivityController(IAuditLogService auditLogService)
    {
        _auditLogService = auditLogService;
    }

    [HttpPost]
    public async Task<IActionResult> Record([FromBody] UserActivityEventRequest request, CancellationToken cancellationToken)
    {
        if (!AllowedActions.Contains(request.Action))
        {
            return BadRequest(new { message = "Unsupported activity action." });
        }

        if (!Guid.TryParse(request.SessionId, out var sessionId))
        {
            return BadRequest(new { message = "A valid session id is required." });
        }

        if (!string.IsNullOrWhiteSpace(request.Flow) && !AllowedFlows.Contains(request.Flow))
        {
            return BadRequest(new { message = "Unsupported activity flow." });
        }

        var route = NormalizeRoute(request.Route);
        if (route is null)
        {
            return BadRequest(new { message = "A valid page route is required." });
        }

        var previousRoute = string.IsNullOrWhiteSpace(request.PreviousRoute)
            ? null
            : NormalizeRoute(request.PreviousRoute);
        if (!string.IsNullOrWhiteSpace(request.PreviousRoute) && previousRoute is null)
        {
            return BadRequest(new { message = "The previous page route is invalid." });
        }

        var description = JsonSerializer.Serialize(new
        {
            sessionId,
            route,
            previousRoute,
            flow = request.Flow,
            operationId = request.OperationId,
            durationMs = request.DurationMs
        });

        await _auditLogService.LogAsync(new AuditLogEntry
        {
            Action = request.Action,
            Category = "UsageAnalytics",
            EntityType = request.Action.StartsWith("Page", StringComparison.Ordinal) ? "Page" : "UserFlow",
            EntityId = request.Action.StartsWith("Page", StringComparison.Ordinal) ? route : request.Flow,
            Description = description,
            CaptureUserName = false,
            CaptureRequestContext = false,
            Status = request.Action switch
            {
                "OperationStarted" or "FormStarted" => "Started",
                "OperationFailed" => "Failed",
                "FormAbandoned" or "OperationAbandoned" or "ReportGeneratedNotSaved" => "Abandoned",
                _ => "Success"
            }
        }, cancellationToken);

        return Accepted();
    }

    private static string? NormalizeRoute(string? value)
    {
        if (string.IsNullOrWhiteSpace(value) || value.Length > 100 || !value.StartsWith('/')
            || value.Contains('?') || value.Contains('#') || value.Any(char.IsControl))
        {
            return null;
        }

        var route = Regex.Replace(value, @"(?i)(?<=/)[0-9a-f]{8}-[0-9a-f]{4}-[0-9a-f]{4}-[0-9a-f]{4}-[0-9a-f]{12}(?=/|$)", ":id");
        route = Regex.Replace(route, @"(?<=/)\d+(?=/|$)", ":id");
        return route.Length <= 100 ? route : null;
    }
}

public sealed class UserActivityEventRequest
{
    [Required]
    [MaxLength(40)]
    public string Action { get; set; } = string.Empty;

    [Required]
    [MaxLength(100)]
    public string Route { get; set; } = string.Empty;

    [Required]
    [MaxLength(36)]
    public string SessionId { get; set; } = string.Empty;

    [MaxLength(40)]
    public string? Flow { get; set; }

    [MaxLength(36)]
    public string? OperationId { get; set; }

    [MaxLength(100)]
    public string? PreviousRoute { get; set; }

    [Range(0, 3_600_000)]
    public int? DurationMs { get; set; }
}
