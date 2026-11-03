using KundaliEngine.API.DTOs.Practitioner;
using KundaliEngine.API.Services;
using KundaliEngine.API.Services.Entitlements;
using KundaliEngine.Domain.Entities;
using KundaliEngine.Infrastructure.Data;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace KundaliEngine.API.Controllers;

[ApiController]
[Authorize]
[Route("api/appointments")]
public sealed class PractitionerAppointmentsController : PractitionerControllerBase
{
    private readonly KundaliDbContext _db;

    public PractitionerAppointmentsController(KundaliDbContext db, IEntitlementService entitlements) : base(db, entitlements) => _db = db;

    [HttpGet]
    public async Task<ActionResult<IReadOnlyList<PractitionerAppointmentDto>>> GetAll([FromQuery] DateTime? fromUtc, [FromQuery] DateTime? toUtc, CancellationToken cancellationToken)
    {
        var access = await GetAccessAsync(cancellationToken);
        if (access.UserId is null) return Unauthorized();
        if (!access.HasPremiumPlus) return UpgradeRequired();
        var query = _db.PractitionerAppointments.AsNoTracking().Include(x => x.Kundali).Include(x => x.Client).Where(x => x.UserId == access.UserId.Value);
        if (fromUtc.HasValue) query = query.Where(x => x.EndsAtUtc >= DateTime.SpecifyKind(fromUtc.Value, DateTimeKind.Utc));
        if (toUtc.HasValue) query = query.Where(x => x.StartsAtUtc <= DateTime.SpecifyKind(toUtc.Value, DateTimeKind.Utc));
        var records = await query.OrderBy(x => x.StartsAtUtc).ToListAsync(cancellationToken);
        var dtos = records.Select(Map).ToArray();
        foreach (var dto in dtos.Where(x => x.Status is "New" or "Consultation Pending"))
        {
            dto.OverlapsExisting = records.Any(other => other.Id != dto.Id && (other.Status is "New" or "Consultation Pending") && other.StartsAtUtc < dto.EndsAtUtc && dto.StartsAtUtc < other.EndsAtUtc);
        }
        return Ok(dtos);
    }

    [HttpPost]
    public async Task<ActionResult<PractitionerAppointmentDto>> Create([FromBody] PractitionerAppointmentRequest request, CancellationToken cancellationToken)
    {
        var access = await GetAccessAsync(cancellationToken);
        if (access.UserId is null) return Unauthorized();
        if (!access.HasPremiumPlus) return UpgradeRequired();
        var error = ValidateAndResolve(request, out var zone, out var startUtc, out var endUtc);
        if (error is not null) return BadRequest(new { message = error });
        var relatedError = await ValidateLinksAsync(access.UserId.Value, request, cancellationToken);
        if (relatedError is not null) return BadRequest(new { message = relatedError });
        var entity = new PractitionerAppointment
        {
            Id = Guid.NewGuid(),
            UserId = access.UserId.Value,
            ClientId = request.ClientId,
            KundaliId = request.KundaliId,
            Status = request.Status,
            CreatedAt = DateTime.UtcNow
        };
        Apply(entity, request, zone!, startUtc, endUtc);
        entity.CompletedAtUtc = entity.Status == "Completed" ? DateTime.UtcNow : null;
        _db.PractitionerAppointments.Add(entity);
        await _db.SaveChangesAsync(cancellationToken);
        var dto = await LoadDtoAsync(entity.Id, access.UserId.Value, cancellationToken);
        dto!.OverlapsExisting = await HasOverlapAsync(entity, cancellationToken);
        return CreatedAtAction(nameof(GetAll), new { }, dto);
    }

    [HttpPut("{id:guid}")]
    public async Task<ActionResult<PractitionerAppointmentDto>> Update(Guid id, [FromBody] PractitionerAppointmentRequest request, CancellationToken cancellationToken)
    {
        var access = await GetAccessAsync(cancellationToken);
        if (access.UserId is null) return Unauthorized();
        if (!access.HasPremiumPlus) return UpgradeRequired();
        var error = ValidateAndResolve(request, out var zone, out var startUtc, out var endUtc);
        if (error is not null) return BadRequest(new { message = error });
        var entity = await _db.PractitionerAppointments.FirstOrDefaultAsync(x => x.Id == id && x.UserId == access.UserId.Value, cancellationToken);
        if (entity is null) return NotFound();
        if (entity.Status is not ("New" or "Consultation Pending")) return Conflict(new { message = "Only active consultations can be edited." });
        var relatedError = await ValidateLinksAsync(access.UserId.Value, request, cancellationToken);
        if (relatedError is not null) return BadRequest(new { message = relatedError });
        entity.ClientId = request.ClientId;
        entity.KundaliId = request.KundaliId;
        Apply(entity, request, zone!, startUtc, endUtc);
        entity.Status = request.Status;
        entity.CompletedAtUtc = entity.Status == "Completed" ? DateTime.UtcNow : null;
        entity.ReminderVersion++;
        entity.ReminderSentVersion = null;
        entity.UpdatedAt = DateTime.UtcNow;
        await RemoveReminderNotificationsAsync(entity.Id, cancellationToken);
        await _db.SaveChangesAsync(cancellationToken);
        var dto = await LoadDtoAsync(entity.Id, access.UserId.Value, cancellationToken);
        dto!.OverlapsExisting = await HasOverlapAsync(entity, cancellationToken);
        return Ok(dto);
    }

    [HttpPost("{id:guid}/cancel")]
    public async Task<IActionResult> Cancel(Guid id, CancellationToken cancellationToken)
    {
        var access = await GetAccessAsync(cancellationToken);
        if (access.UserId is null) return Unauthorized();
        if (!access.HasPremiumPlus) return UpgradeRequired();
        var entity = await _db.PractitionerAppointments.FirstOrDefaultAsync(x => x.Id == id && x.UserId == access.UserId.Value, cancellationToken);
        if (entity is null) return NotFound();
        var cancellation = PractitionerAppointmentCancellation.Evaluate(entity.Status);
        if (cancellation == PractitionerAppointmentCancellationResult.Conflict)
            return Conflict(new { message = "Only scheduled consultations can be cancelled." });
        if (cancellation == PractitionerAppointmentCancellationResult.AlreadyCancelled)
            return NoContent();

        entity.Status = "Cancelled";
        entity.CompletedAtUtc = null;
        entity.ReminderVersion++;
        entity.ReminderSentVersion = entity.ReminderVersion;
        entity.UpdatedAt = DateTime.UtcNow;
        await RemoveReminderNotificationsAsync(entity.Id, cancellationToken);
        await _db.SaveChangesAsync(cancellationToken);
        return NoContent();
    }

    [HttpPost("{id:guid}/complete")]
    public async Task<IActionResult> Complete(Guid id, CancellationToken cancellationToken)
    {
        var access = await GetAccessAsync(cancellationToken);
        if (access.UserId is null) return Unauthorized();
        if (!access.HasPremiumPlus) return UpgradeRequired();
        var entity = await _db.PractitionerAppointments.FirstOrDefaultAsync(x => x.Id == id && x.UserId == access.UserId.Value, cancellationToken);
        if (entity is null) return NotFound();
        if (entity.Status == "Cancelled") return Conflict(new { message = "Cancelled appointments cannot be completed." });
        if (entity.Status == "Completed") return NoContent();
        entity.Status = "Completed";
        entity.CompletedAtUtc = DateTime.UtcNow;
        entity.ReminderVersion++;
        entity.ReminderSentVersion = entity.ReminderVersion;
        entity.UpdatedAt = DateTime.UtcNow;
        await RemoveReminderNotificationsAsync(entity.Id, cancellationToken);
        await _db.SaveChangesAsync(cancellationToken);
        return NoContent();
    }

    private async Task<string?> ValidateLinksAsync(Guid userId, PractitionerAppointmentRequest request, CancellationToken cancellationToken)
    {
        var client = await _db.PractitionerClients.AsNoTracking().FirstOrDefaultAsync(x => x.Id == request.ClientId && x.UserId == userId && !x.IsArchived, cancellationToken);
        if (client is null) return "Select an active client owned by this account.";
        if (request.KundaliId is { } chartId)
        {
            var chart = await _db.Kundalis.AsNoTracking().FirstOrDefaultAsync(x => x.Id == chartId && x.UserId == userId, cancellationToken);
            if (chart is null) return "Select a chart owned by this account.";
            if (chart.ClientId.HasValue && chart.ClientId != request.ClientId) return "The selected chart is linked to another client.";
        }
        return null;
    }

    private static string? ValidateAndResolve(PractitionerAppointmentRequest request, out TimeZoneInfo? zone, out DateTime startUtc, out DateTime endUtc)
    {
        zone = null; startUtc = default; endUtc = default;
        if (request.ClientId == Guid.Empty) return "Client is required.";
        if (request.Status is not ("New" or "Consultation Pending" or "Completed")) return "Choose a valid consultation status.";
        if (request.Topic?.Trim().Length > 200) return "Consultation topic is too long.";
        if (request.ReminderLeadMinutes is < 1 or > 10080) return "Reminder lead time must be between 1 minute and 7 days.";
        if (request.Notes?.Trim().Length > 1000) return "Appointment notes are too long.";
        if (PractitionerAppointmentTimeResolver.TryResolve(request.StartsAtLocal, request.EndsAtLocal, request.TimeZoneId, out startUtc, out endUtc, out var error))
        {
            TimeZoneInfo.TryFindSystemTimeZoneById(request.TimeZoneId, out zone);
            return null;
        }
        return error;
    }

    private static void Apply(PractitionerAppointment entity, PractitionerAppointmentRequest request, TimeZoneInfo zone, DateTime startUtc, DateTime endUtc)
    {
        entity.StartsAtUtc = startUtc; entity.EndsAtUtc = endUtc; entity.TimeZoneId = zone.Id;
        entity.ReminderLeadMinutes = request.ReminderLeadMinutes; entity.Notes = string.IsNullOrWhiteSpace(request.Notes) ? null : request.Notes.Trim();
        entity.Topic = string.IsNullOrWhiteSpace(request.Topic) ? null : request.Topic.Trim();
    }

    private async Task<PractitionerAppointmentDto?> LoadDtoAsync(Guid id, Guid userId, CancellationToken cancellationToken)
    {
        var entity = await _db.PractitionerAppointments.AsNoTracking().Include(x => x.Kundali).Include(x => x.Client).FirstOrDefaultAsync(x => x.Id == id && x.UserId == userId, cancellationToken);
        return entity is null ? null : Map(entity);
    }

    private async Task<bool> HasOverlapAsync(PractitionerAppointment entity, CancellationToken cancellationToken) => await _db.PractitionerAppointments.AsNoTracking().AnyAsync(x => x.UserId == entity.UserId && x.Id != entity.Id && (x.Status == "New" || x.Status == "Consultation Pending") && x.StartsAtUtc < entity.EndsAtUtc && entity.StartsAtUtc < x.EndsAtUtc, cancellationToken);

    private async Task RemoveReminderNotificationsAsync(Guid appointmentId, CancellationToken cancellationToken)
    {
        var prefix = $"appointment:{appointmentId}:";
        var notifications = await _db.AppNotifications.Where(x => x.DedupeKey.StartsWith(prefix)).ToListAsync(cancellationToken);
        _db.AppNotifications.RemoveRange(notifications);
    }

    private static PractitionerAppointmentDto Map(PractitionerAppointment entity)
    {
        var zone = TimeZoneInfo.FindSystemTimeZoneById(entity.TimeZoneId);
        var startsLocal = DateTime.SpecifyKind(TimeZoneInfo.ConvertTimeFromUtc(entity.StartsAtUtc, zone), DateTimeKind.Unspecified);
        var endsLocal = DateTime.SpecifyKind(TimeZoneInfo.ConvertTimeFromUtc(entity.EndsAtUtc, zone), DateTimeKind.Unspecified);
        return new PractitionerAppointmentDto
        {
            Id = entity.Id, ClientId = entity.ClientId, KundaliId = entity.KundaliId, KundaliName = entity.Kundali?.FullName, ClientName = entity.Client?.FullName,
            StartsAtUtc = entity.StartsAtUtc, EndsAtUtc = entity.EndsAtUtc, StartsAtLocal = startsLocal.ToString("yyyy-MM-dd'T'HH:mm"),
            EndsAtLocal = endsLocal.ToString("yyyy-MM-dd'T'HH:mm"), TimeZoneId = entity.TimeZoneId, Status = entity.Status,
            ReminderLeadMinutes = entity.ReminderLeadMinutes, Notes = entity.Notes, Topic = entity.Topic, CompletedAtUtc = entity.CompletedAtUtc
        };
    }
}
