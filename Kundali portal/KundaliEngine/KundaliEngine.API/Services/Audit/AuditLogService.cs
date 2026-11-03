using System.Security.Claims;
using KundaliEngine.Domain.Entities;
using KundaliEngine.Infrastructure.Data;

namespace KundaliEngine.API.Services.Audit;

public class AuditLogService : IAuditLogService
{
    private readonly KundaliDbContext _dbContext;
    private readonly IHttpContextAccessor _httpContextAccessor;
    private readonly ILogger<AuditLogService> _logger;

    public AuditLogService(
        KundaliDbContext dbContext,
        IHttpContextAccessor httpContextAccessor,
        ILogger<AuditLogService> logger)
    {
        _dbContext = dbContext;
        _httpContextAccessor = httpContextAccessor;
        _logger = logger;
    }

    public async Task LogAsync(AuditLogEntry entry, CancellationToken cancellationToken = default)
    {
        try
        {
            var httpContext = _httpContextAccessor.HttpContext;
            var user = httpContext?.User;
            var userId = entry.UserId ?? GetUserId(user);

            var log = new AuditLog
            {
                Id = Guid.NewGuid(),
                CreatedAt = DateTime.UtcNow,
                Action = entry.Action.Trim(),
                Category = entry.Category.Trim(),
                UserId = userId,
                UserName = entry.CaptureUserName
                    ? FirstNonEmpty(entry.UserName, user?.FindFirstValue(ClaimTypes.Name), user?.FindFirstValue(ClaimTypes.Email))
                    : null,
                EntityType = TrimOrNull(entry.EntityType),
                EntityId = TrimOrNull(entry.EntityId),
                Description = TrimOrNull(entry.Description),
                IpAddress = entry.CaptureRequestContext ? httpContext?.Connection.RemoteIpAddress?.ToString() : null,
                UserAgent = entry.CaptureRequestContext ? httpContext?.Request.Headers.UserAgent.ToString() : null,
                RequestPath = entry.CaptureRequestContext ? httpContext?.Request.Path.Value : null,
                Status = string.IsNullOrWhiteSpace(entry.Status) ? "Success" : entry.Status.Trim(),
                FailureReason = TrimOrNull(entry.FailureReason)
            };

            _dbContext.AuditLogs.Add(log);
            await _dbContext.SaveChangesAsync(cancellationToken);
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Audit log write failed for action {Action}.", entry.Action);
        }
    }

    private static Guid? GetUserId(ClaimsPrincipal? user)
    {
        var value = user?.FindFirstValue(ClaimTypes.NameIdentifier);
        return Guid.TryParse(value, out var userId) ? userId : null;
    }

    private static string? FirstNonEmpty(params string?[] values)
    {
        return values.FirstOrDefault(value => !string.IsNullOrWhiteSpace(value))?.Trim();
    }

    private static string? TrimOrNull(string? value)
    {
        return string.IsNullOrWhiteSpace(value) ? null : value.Trim();
    }
}
