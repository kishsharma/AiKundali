using KundaliEngine.API.DTOs.Responses;

namespace KundaliEngine.API.Services.Dashboard;

public interface IDashboardSummaryService
{
    Task<DashboardSummaryDto?> GetSummaryAsync(Guid userId, CancellationToken cancellationToken);
}
