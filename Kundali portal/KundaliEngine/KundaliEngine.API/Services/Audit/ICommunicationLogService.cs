namespace KundaliEngine.API.Services.Audit;

public interface ICommunicationLogService
{
    Task LogAsync(CommunicationLogEntry entry, CancellationToken cancellationToken = default);
}
