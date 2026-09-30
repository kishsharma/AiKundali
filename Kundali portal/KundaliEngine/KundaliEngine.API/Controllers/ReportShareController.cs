using System.Net.Mail;
using System.Security.Claims;
using KundaliEngine.API.DTOs.Requests;
using KundaliEngine.API.DTOs.Responses;
using KundaliEngine.API.Services.Audit;
using KundaliEngine.API.Services.Entitlements;
using KundaliEngine.API.Services.ReportPdf;
using KundaliEngine.API.Services.ReportShare;
using KundaliEngine.Domain.Entities;
using KundaliEngine.Infrastructure.Data;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.RateLimiting;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace KundaliEngine.API.Controllers;

[ApiController]
[Authorize]
[EnableRateLimiting("ReportShare")]
[Route("api/[controller]")]
public class ReportShareController : ControllerBase
{
    private readonly IReportShareEmailService _reportShareEmailService;
    private readonly IReportPdfService _reportPdfService;
    private readonly IAuditLogService _auditLogService;
    private readonly ICommunicationLogService _communicationLogService;
    private readonly ILogger<ReportShareController> _logger;
    private readonly KundaliDbContext _dbContext;
    private readonly IEntitlementService _entitlementService;

    public ReportShareController(
        IReportShareEmailService reportShareEmailService,
        IReportPdfService reportPdfService,
        IAuditLogService auditLogService,
        ICommunicationLogService communicationLogService,
        ILogger<ReportShareController> logger,
        KundaliDbContext dbContext,
        IEntitlementService entitlementService)
    {
        _reportShareEmailService = reportShareEmailService;
        _reportPdfService = reportPdfService;
        _auditLogService = auditLogService;
        _communicationLogService = communicationLogService;
        _logger = logger;
        _dbContext = dbContext;
        _entitlementService = entitlementService;
    }

    [HttpPost("pdf")]
    [DisableRateLimiting]
    public async Task<ActionResult> GeneratePdf([FromBody] ReportPdfRequest request)
    {
        if (!ModelState.IsValid)
        {
            return ValidationProblem(ModelState);
        }

        var user = await GetActiveUserAsync();
        if (user == null)
        {
            return Unauthorized();
        }

        var isPaid = _entitlementService.IsPaid(user);
        if (!isPaid)
        {
            var usedToday = await CountFreePdfDownloadsTodayAsync(user.Id);
            if (usedToday >= 1)
            {
                return StatusCode(StatusCodes.Status429TooManyRequests, new SendReportEmailResponse
                {
                    Success = false,
                    Message = "Free plan can download 1 Kundali PDF per day. Premium includes unlimited downloads, email, and WhatsApp."
                });
            }
        }

        request.Audience = isPaid ? "paid" : "free";

        try
        {
            var pdfBytes = _reportPdfService.GenerateReportPdf(request);
            var fileName = _reportPdfService.BuildFileName(request);
            await _auditLogService.LogAsync(new AuditLogEntry
            {
                Action = "PdfExported",
                Category = "Export",
                EntityType = "Report",
                EntityId = fileName,
                Description = $"Report PDF exported as {fileName}.",
                Status = "Success"
            });

            return File(pdfBytes, "application/pdf", fileName);
        }
        catch (InvalidOperationException ex)
        {
            _logger.LogWarning(ex, "Report PDF request could not be processed.");
            await LogPdfExportFailure(ex.Message);
            return BadRequest(new SendReportEmailResponse
            {
                Success = false,
                Message = ex.Message
            });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Report PDF generation failed.");
            await LogPdfExportFailure("Unable to generate report PDF right now.");
            return StatusCode(StatusCodes.Status500InternalServerError, new SendReportEmailResponse
            {
                Success = false,
                Message = "Unable to generate report PDF right now."
            });
        }
    }

    [HttpPost("sendEmail")]
    public async Task<ActionResult<SendReportEmailResponse>> SendEmail(
        [FromBody] SendReportEmailRequest request,
        CancellationToken cancellationToken)
    {
        if (!ModelState.IsValid)
        {
            return ValidationProblem(ModelState);
        }

        var paidCheck = await EnsurePaidAsync();
        if (paidCheck != null)
        {
            return paidCheck;
        }

        try
        {
            await _reportShareEmailService.SendReportEmailAsync(request, cancellationToken);
            await LogEmailAsync(request, "Sent", null, cancellationToken);
            return Ok(new SendReportEmailResponse
            {
                Success = true,
                Message = "Report email sent."
            });
        }
        catch (ReportShareConfigurationException ex)
        {
            _logger.LogError(ex, "Report email SMTP settings are not configured for {Recipient}.", request.ToEmail);
            await LogEmailAsync(request, "Failed", ex.Message, cancellationToken);
            return StatusCode(StatusCodes.Status503ServiceUnavailable, new SendReportEmailResponse
            {
                Success = false,
                Message = "Report email service is not configured."
            });
        }
        catch (InvalidOperationException ex) when (IsAttachmentTooLarge(ex))
        {
            _logger.LogWarning(ex, "Report email attachment was too large for {Recipient}.", request.ToEmail);
            await LogEmailAsync(request, "Failed", ex.Message, cancellationToken);
            return StatusCode(StatusCodes.Status413PayloadTooLarge, new SendReportEmailResponse
            {
                Success = false,
                Message = ex.Message
            });
        }
        catch (InvalidOperationException ex)
        {
            _logger.LogWarning(ex, "Report email request could not be processed for {Recipient}.", request.ToEmail);
            await LogEmailAsync(request, "Failed", ex.Message, cancellationToken);
            return BadRequest(new SendReportEmailResponse
            {
                Success = false,
                Message = ex.Message
            });
        }
        catch (OperationCanceledException ex) when (!cancellationToken.IsCancellationRequested)
        {
            _logger.LogWarning(ex, "Report email send timed out for {Recipient}.", request.ToEmail);
            await LogEmailAsync(request, "Failed", "Report email timed out while contacting SMTP server.", CancellationToken.None);
            return StatusCode(StatusCodes.Status504GatewayTimeout, new SendReportEmailResponse
            {
                Success = false,
                Message = "Report email timed out while contacting SMTP server."
            });
        }
        catch (SmtpException ex) when (IsSenderRejectedBecauseNotAuthenticated(ex))
        {
            _logger.LogError(ex, "Report email SMTP sender was rejected for {Recipient}.", request.ToEmail);
            await LogEmailAsync(request, "Failed", "Report email SMTP authentication failed.", cancellationToken);
            return StatusCode(StatusCodes.Status502BadGateway, new SendReportEmailResponse
            {
                Success = false,
                Message = "Report email SMTP authentication failed. Verify the SMTP username/password and ensure FromEmail is the same mailbox or an allowed sender alias."
            });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Report email send failed for {Recipient}.", request.ToEmail);
            await LogEmailAsync(request, "Failed", "Unable to send report email right now.", cancellationToken);
            return StatusCode(StatusCodes.Status502BadGateway, new SendReportEmailResponse
            {
                Success = false,
                Message = "Unable to send report email right now."
            });
        }
    }

    private async Task<ActionResult?> EnsurePaidAsync()
    {
        var user = await GetActiveUserAsync();
        if (user == null)
        {
            return Unauthorized();
        }

        if (_entitlementService.IsPaid(user))
        {
            return null;
        }

        return StatusCode(StatusCodes.Status402PaymentRequired, new SendReportEmailResponse
        {
            Success = false,
            Message = "Email and WhatsApp reports are available on Premium."
        });
    }

    private async Task<User?> GetActiveUserAsync()
    {
        var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
        if (!Guid.TryParse(userId, out var parsedUserId))
        {
            return null;
        }

        var user = await _dbContext.Users.AsNoTracking().FirstOrDefaultAsync(item => item.Id == parsedUserId);
        return user is { IsActive: true } ? user : null;
    }

    private Task<int> CountFreePdfDownloadsTodayAsync(Guid userId)
    {
        var indiaNow = DateTime.UtcNow.AddHours(5.5);
        var startUtc = DateTime.SpecifyKind(indiaNow.Date.AddHours(-5.5), DateTimeKind.Utc);
        return _dbContext.AuditLogs.CountAsync(log =>
            log.UserId == userId
            && log.Action == "PdfExported"
            && log.Status == "Success"
            && log.CreatedAt >= startUtc);
    }

    private Task LogPdfExportFailure(string reason)
    {
        return _auditLogService.LogAsync(new AuditLogEntry
        {
            Action = "PdfExportFailed",
            Category = "Export",
            EntityType = "Report",
            Description = "Report PDF export failed.",
            Status = "Failed",
            FailureReason = reason
        });
    }

    private Task LogEmailAsync(SendReportEmailRequest request, string status, string? failureReason, CancellationToken cancellationToken)
    {
        return _communicationLogService.LogAsync(new CommunicationLogEntry
        {
            Channel = "Email",
            RecipientEmail = request.ToEmail,
            MessageType = "Report",
            TemplateName = "KundaliReportEmail",
            Subject = string.IsNullOrWhiteSpace(request.Subject) ? "Kundali report" : request.Subject.Trim(),
            Status = status,
            Provider = "SMTP",
            FailureReason = failureReason,
            RelatedEntityType = "Report"
        }, cancellationToken);
    }

    private static bool IsSenderRejectedBecauseNotAuthenticated(SmtpException exception)
    {
        var message = exception.Message;
        return message.Contains("Sender address rejected", StringComparison.OrdinalIgnoreCase)
            || message.Contains("not logged in", StringComparison.OrdinalIgnoreCase);
    }

    private static bool IsAttachmentTooLarge(InvalidOperationException exception)
    {
        return exception.Message.Contains("too large", StringComparison.OrdinalIgnoreCase);
    }
}
