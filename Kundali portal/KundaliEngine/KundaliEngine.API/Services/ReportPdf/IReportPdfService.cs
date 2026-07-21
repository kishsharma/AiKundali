using KundaliEngine.API.DTOs.Requests;

namespace KundaliEngine.API.Services.ReportPdf;

public interface IReportPdfService
{
    byte[] GenerateReportPdf(ReportPdfRequest request);
    string BuildFileName(ReportPdfRequest request);
}