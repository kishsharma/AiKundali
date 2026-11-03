using KundaliEngine.Infrastructure.Data;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace KundaliEngine.API.Controllers;

[ApiController]
[Route("api/admin/logs")]
[Authorize(Roles = "Admin")]
public class AdminLogsController : ControllerBase
{
    private readonly KundaliDbContext _dbContext;

    public AdminLogsController(KundaliDbContext dbContext)
    {
        _dbContext = dbContext;
    }

    [HttpGet("audit")]
    public async Task<IActionResult> GetAuditLogs([FromQuery] AuditLogQuery query)
    {
        var page = Math.Max(query.Page, 1);
        var pageSize = Math.Clamp(query.PageSize, 1, 100);

        var logsQuery = _dbContext.AuditLogs.AsNoTracking();

        if (query.DateFrom.HasValue)
        {
            logsQuery = logsQuery.Where(x => x.CreatedAt >= query.DateFrom.Value);
        }

        if (query.DateTo.HasValue)
        {
            logsQuery = logsQuery.Where(x => x.CreatedAt <= query.DateTo.Value);
        }

        if (query.UserId.HasValue)
        {
            logsQuery = logsQuery.Where(x => x.UserId == query.UserId.Value);
        }

        if (!string.IsNullOrWhiteSpace(query.SessionId))
        {
            if (!Guid.TryParse(query.SessionId, out var sessionId))
            {
                return BadRequest(new { message = "Session id must be a valid identifier." });
            }

            var sessionToken = sessionId.ToString();
            logsQuery = logsQuery.Where(x => x.Category == "UsageAnalytics"
                && x.Description != null
                && EF.Functions.Like(x.Description, $"%{sessionToken}%"));
        }

        if (!string.IsNullOrWhiteSpace(query.Action))
        {
            logsQuery = logsQuery.Where(x => x.Action == query.Action.Trim());
        }

        if (!string.IsNullOrWhiteSpace(query.Category))
        {
            logsQuery = logsQuery.Where(x => x.Category == query.Category.Trim());
        }

        if (!string.IsNullOrWhiteSpace(query.EntityType))
        {
            logsQuery = logsQuery.Where(x => x.EntityType == query.EntityType.Trim());
        }

        if (!string.IsNullOrWhiteSpace(query.Status))
        {
            logsQuery = logsQuery.Where(x => x.Status == query.Status.Trim());
        }

        var total = await logsQuery.CountAsync();
        var items = await logsQuery
            .OrderByDescending(x => x.CreatedAt)
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .ToListAsync();

        return Ok(new { total, page, pageSize, items });
    }

    [HttpGet("communication")]
    public async Task<IActionResult> GetCommunicationLogs([FromQuery] CommunicationLogQuery query)
    {
        var page = Math.Max(query.Page, 1);
        var pageSize = Math.Clamp(query.PageSize, 1, 100);

        var logsQuery = _dbContext.CommunicationLogs.AsNoTracking();

        if (query.DateFrom.HasValue)
        {
            logsQuery = logsQuery.Where(x => x.CreatedAt >= query.DateFrom.Value);
        }

        if (query.DateTo.HasValue)
        {
            logsQuery = logsQuery.Where(x => x.CreatedAt <= query.DateTo.Value);
        }

        if (!string.IsNullOrWhiteSpace(query.Channel))
        {
            logsQuery = logsQuery.Where(x => x.Channel == query.Channel.Trim());
        }

        if (!string.IsNullOrWhiteSpace(query.Status))
        {
            logsQuery = logsQuery.Where(x => x.Status == query.Status.Trim());
        }

        if (!string.IsNullOrWhiteSpace(query.MessageType))
        {
            logsQuery = logsQuery.Where(x => x.MessageType == query.MessageType.Trim());
        }

        if (!string.IsNullOrWhiteSpace(query.Recipient))
        {
            var recipient = query.Recipient.Trim();
            logsQuery = logsQuery.Where(x => x.RecipientEmail == recipient || x.RecipientMobile == recipient);
        }

        var total = await logsQuery.CountAsync();
        var items = await logsQuery
            .OrderByDescending(x => x.CreatedAt)
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .ToListAsync();

        return Ok(new { total, page, pageSize, items });
    }
}

public sealed class AuditLogQuery
{
    public DateTime? DateFrom { get; set; }
    public DateTime? DateTo { get; set; }
    public Guid? UserId { get; set; }
    public string? SessionId { get; set; }
    public string? Action { get; set; }
    public string? Category { get; set; }
    public string? EntityType { get; set; }
    public string? Status { get; set; }
    public int Page { get; set; } = 1;
    public int PageSize { get; set; } = 50;
}

public sealed class CommunicationLogQuery
{
    public DateTime? DateFrom { get; set; }
    public DateTime? DateTo { get; set; }
    public string? Channel { get; set; }
    public string? Status { get; set; }
    public string? MessageType { get; set; }
    public string? Recipient { get; set; }
    public int Page { get; set; } = 1;
    public int PageSize { get; set; } = 50;
}

