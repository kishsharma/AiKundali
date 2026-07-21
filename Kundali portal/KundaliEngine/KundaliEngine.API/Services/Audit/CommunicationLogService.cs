using System.Security.Claims;
using KundaliEngine.Domain.Entities;
using KundaliEngine.Infrastructure.Data;

namespace KundaliEngine.API.Services.Audit;

public class CommunicationLogService : ICommunicationLogService
{
    private readonly KundaliDbContext _dbContext;
    private readonly IHttpContextAccessor _httpContextAccessor;
    private readonly ILogger<CommunicationLogService> _logger;

    public CommunicationLogService(
        KundaliDbContext dbContext,
        IHttpContextAccessor httpContextAccessor,
        ILogger<CommunicationLogService> logger)
    {
        _dbContext = dbContext;
        _httpContextAccessor = httpContextAccessor;
        _logger = logger;
    }

    public async Task LogAsync(CommunicationLogEntry entry, CancellationToken cancellationToken = default)
    {
        try
        {
            var user = _httpContextAccessor.HttpContext?.User;
            var log = new CommunicationLog
            {
                Id = Guid.NewGuid(),
                CreatedAt = DateTime.UtcNow,
                Channel = entry.Channel.Trim(),
                RecipientEmail = TrimOrNull(entry.RecipientEmail),
                RecipientMobile = TrimOrNull(entry.RecipientMobile),
                MessageType = entry.MessageType.Trim(),
                TemplateName = TrimOrNull(entry.TemplateName),
                Subject = TrimOrNull(entry.Subject),
                Status = string.IsNullOrWhiteSpace(entry.Status) ? "Sent" : entry.Status.Trim(),
                Provider = TrimOrNull(entry.Provider),
                ProviderMessageId = TrimOrNull(entry.ProviderMessageId),
                FailureReason = TrimOrNull(entry.FailureReason),
                RelatedEntityType = TrimOrNull(entry.RelatedEntityType),
                RelatedEntityId = TrimOrNull(entry.RelatedEntityId),
                TriggeredByUserId = entry.TriggeredByUserId ?? GetUserId(user),
                TriggeredByUserName = FirstNonEmpty(entry.TriggeredByUserName, user?.FindFirstValue(ClaimTypes.Name), user?.FindFirstValue(ClaimTypes.Email))
            };

            _dbContext.CommunicationLogs.Add(log);
            await _dbContext.SaveChangesAsync(cancellationToken);
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Communication log write failed for channel {Channel}.", entry.Channel);
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
