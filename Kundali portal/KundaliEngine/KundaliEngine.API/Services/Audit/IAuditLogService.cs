namespace KundaliEngine.API.Services.Audit;

public interface IAuditLogService
{
    Task LogAsync(AuditLogEntry entry, CancellationToken cancellationToken = default);
}
