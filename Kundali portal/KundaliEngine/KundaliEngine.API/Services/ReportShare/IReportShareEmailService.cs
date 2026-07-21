using KundaliEngine.API.DTOs.Requests;

namespace KundaliEngine.API.Services.ReportShare;

public interface IReportShareEmailService
{
    Task SendReportEmailAsync(SendReportEmailRequest request, CancellationToken cancellationToken);
}
