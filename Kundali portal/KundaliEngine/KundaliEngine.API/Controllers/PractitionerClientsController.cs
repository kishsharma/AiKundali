using System.Security.Claims;
using KundaliEngine.API.DTOs.Practitioner;
using KundaliEngine.API.Services.Entitlements;
using KundaliEngine.API.Services;
using KundaliEngine.Domain.Entities;
using KundaliEngine.Infrastructure.Data;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace KundaliEngine.API.Controllers;

[ApiController]
[Authorize]
[Route("api/clients")]
public sealed class PractitionerClientsController : PractitionerControllerBase
{
    private readonly KundaliDbContext _db;

    public PractitionerClientsController(KundaliDbContext db, IEntitlementService entitlements) : base(db, entitlements) => _db = db;

    [HttpGet("dashboard-summary")]
    public async Task<ActionResult<PractitionerCrmDashboardSummaryDto>> GetDashboardSummary(
        [FromQuery] string? timeZoneId,
        CancellationToken cancellationToken)
    {
        var access = await GetAccessAsync(cancellationToken);
        if (access.UserId is null) return Unauthorized();
        if (!access.HasPremiumPlus) return UpgradeRequired();
        if (string.IsNullOrWhiteSpace(timeZoneId) || !TimeZoneInfo.TryFindSystemTimeZoneById(timeZoneId, out var timeZone))
            return BadRequest(new { message = "Choose a valid time zone." });

        var userId = access.UserId.Value;
        var now = DateTime.UtcNow;
        var today = DateOnly.FromDateTime(TimeZoneInfo.ConvertTimeFromUtc(now, timeZone));
        var startOfToday = TimeZoneInfo.ConvertTimeToUtc(today.ToDateTime(TimeOnly.MinValue), timeZone);
        var startOfTomorrow = TimeZoneInfo.ConvertTimeToUtc(today.AddDays(1).ToDateTime(TimeOnly.MinValue), timeZone);
        var activeClientIds = _db.PractitionerClients.AsNoTracking()
            .Where(client => client.UserId == userId && !client.IsArchived)
            .Select(client => client.Id);

        var summary = new PractitionerCrmDashboardSummaryDto
        {
            ActiveClients = await _db.PractitionerClients.CountAsync(client => client.UserId == userId && !client.IsArchived, cancellationToken),
            AllSavedCharts = await _db.Kundalis.CountAsync(chart => chart.UserId == userId, cancellationToken),
            LinkedClientCharts = await _db.Kundalis.CountAsync(chart => chart.UserId == userId && chart.ClientId.HasValue && activeClientIds.Contains(chart.ClientId.Value), cancellationToken),
            UpcomingConsultations = await _db.PractitionerAppointments.CountAsync(appointment => appointment.UserId == userId
                && (appointment.Status == "New" || appointment.Status == "Consultation Pending") && appointment.StartsAtUtc >= now, cancellationToken),
            CompletedConsultationsLast30Days = await _db.PractitionerAppointments.CountAsync(appointment => appointment.UserId == userId
                && appointment.Status == "Completed" && appointment.CompletedAtUtc >= now.AddDays(-30), cancellationToken),
            TodaysConsultations = await _db.PractitionerAppointments.CountAsync(appointment => appointment.UserId == userId
                && appointment.Status != "Cancelled" && appointment.StartsAtUtc >= startOfToday && appointment.StartsAtUtc < startOfTomorrow, cancellationToken)
        };
        return Ok(summary);
    }

    [HttpGet]
    public async Task<ActionResult<IReadOnlyList<PractitionerClientDto>>> GetAll([FromQuery] string? search, [FromQuery] bool includeArchived = false, CancellationToken cancellationToken = default)
    {
        var access = await GetAccessAsync(cancellationToken);
        if (access.UserId is null) return Unauthorized();
        if (!access.HasPremiumPlus) return UpgradeRequired();

        var query = _db.PractitionerClients.AsNoTracking().Where(x => x.UserId == access.UserId.Value && (includeArchived || !x.IsArchived));
        if (!string.IsNullOrWhiteSpace(search))
        {
            var term = search.Trim();
            query = query.Where(x => x.FullName.Contains(term) || (x.Surname != null && x.Surname.Contains(term)) || (x.PhoneNumber != null && x.PhoneNumber.Contains(term)) || (x.Email != null && x.Email.Contains(term)));
        }

        var clients = await query.OrderBy(x => x.FullName).ThenBy(x => x.Surname).ToListAsync(cancellationToken);
        var clientIds = clients.Select(client => client.Id).ToArray();
        var chartCounts = await _db.Kundalis.AsNoTracking().Where(chart => chart.UserId == access.UserId.Value && chart.ClientId.HasValue && clientIds.Contains(chart.ClientId.Value))
            .GroupBy(chart => chart.ClientId!.Value).Select(group => new { ClientId = group.Key, Count = group.Count() })
            .ToDictionaryAsync(item => item.ClientId, item => item.Count, cancellationToken);
        var nextAppointments = await _db.PractitionerAppointments.AsNoTracking()
            .Where(appointment => appointment.UserId == access.UserId.Value && clientIds.Contains(appointment.ClientId)
                && (appointment.Status == "New" || appointment.Status == "Consultation Pending") && appointment.StartsAtUtc >= DateTime.UtcNow)
            .Include(appointment => appointment.Kundali)
            .Include(appointment => appointment.Client)
            .OrderBy(appointment => appointment.StartsAtUtc)
            .ToListAsync(cancellationToken);
        var nextByClient = nextAppointments.GroupBy(appointment => appointment.ClientId)
            .ToDictionary(group => group.Key, group => MapAppointment(group.First()));
        var result = clients.Select(client =>
        {
            var dto = MapClient(client);
            dto.LinkedChartCount = chartCounts.GetValueOrDefault(client.Id);
            dto.NextAppointment = nextByClient.GetValueOrDefault(client.Id);
            return dto;
        }).ToArray();
        return Ok(result);
    }

    [HttpGet("{id:guid}")]
    public async Task<ActionResult<PractitionerClientDto>> GetById(Guid id, CancellationToken cancellationToken)
    {
        var access = await GetAccessAsync(cancellationToken);
        if (access.UserId is null) return Unauthorized();
        if (!access.HasPremiumPlus) return UpgradeRequired();
        var client = await _db.PractitionerClients.AsNoTracking().FirstOrDefaultAsync(x => x.Id == id && x.UserId == access.UserId.Value, cancellationToken);
        if (client is null) return NotFound();
        return Ok(await MapClientDetailsAsync(client, cancellationToken));
    }

    [HttpPost]
    public async Task<ActionResult<PractitionerClientDto>> Create([FromBody] PractitionerClientRequest request, CancellationToken cancellationToken)
    {
        var access = await GetAccessAsync(cancellationToken);
        if (access.UserId is null) return Unauthorized();
        if (!access.HasPremiumPlus) return UpgradeRequired();
        var error = PractitionerClientValidator.Validate(request);
        if (error is not null) return BadRequest(new { message = error });
        var client = new PractitionerClient { Id = Guid.NewGuid(), UserId = access.UserId.Value };
        Apply(client, request);
        _db.PractitionerClients.Add(client);
        await _db.SaveChangesAsync(cancellationToken);
        return CreatedAtAction(nameof(GetById), new { id = client.Id }, MapClient(client));
    }

    [HttpPut("{id:guid}")]
    public async Task<ActionResult<PractitionerClientDto>> Update(Guid id, [FromBody] PractitionerClientRequest request, CancellationToken cancellationToken)
    {
        var access = await GetAccessAsync(cancellationToken);
        if (access.UserId is null) return Unauthorized();
        if (!access.HasPremiumPlus) return UpgradeRequired();
        var error = PractitionerClientValidator.Validate(request);
        if (error is not null) return BadRequest(new { message = error });
        var client = await _db.PractitionerClients.FirstOrDefaultAsync(x => x.Id == id && x.UserId == access.UserId.Value && !x.IsArchived, cancellationToken);
        if (client is null) return NotFound();
        Apply(client, request);
        client.UpdatedAt = DateTime.UtcNow;
        await _db.SaveChangesAsync(cancellationToken);
        return Ok(MapClient(client));
    }

    [HttpPost("{id:guid}/archive")]
    public async Task<IActionResult> Archive(Guid id, CancellationToken cancellationToken)
    {
        var access = await GetAccessAsync(cancellationToken);
        if (access.UserId is null) return Unauthorized();
        if (!access.HasPremiumPlus) return UpgradeRequired();
        var client = await _db.PractitionerClients.FirstOrDefaultAsync(x => x.Id == id && x.UserId == access.UserId.Value, cancellationToken);
        if (client is null) return NotFound();
        client.IsArchived = true;
        client.UpdatedAt = DateTime.UtcNow;
        await _db.SaveChangesAsync(cancellationToken);
        return NoContent();
    }

    [HttpPost("{id:guid}/charts/{kundaliId:guid}")]
    public async Task<IActionResult> LinkChart(Guid id, Guid kundaliId, CancellationToken cancellationToken)
    {
        var access = await GetAccessAsync(cancellationToken);
        if (access.UserId is null) return Unauthorized();
        if (!access.HasPremiumPlus) return UpgradeRequired();
        var client = await _db.PractitionerClients.FirstOrDefaultAsync(x => x.Id == id && x.UserId == access.UserId.Value && !x.IsArchived, cancellationToken);
        if (client is null) return NotFound(new { message = "Client was not found." });
        var chart = await _db.Kundalis.FirstOrDefaultAsync(x => x.Id == kundaliId && x.UserId == access.UserId.Value, cancellationToken);
        if (chart is null) return NotFound(new { message = "Chart was not found." });
        chart.ClientId = client.Id;
        await _db.SaveChangesAsync(cancellationToken);
        return NoContent();
    }

    [HttpDelete("{id:guid}/charts/{kundaliId:guid}")]
    public async Task<IActionResult> UnlinkChart(Guid id, Guid kundaliId, CancellationToken cancellationToken)
    {
        var access = await GetAccessAsync(cancellationToken);
        if (access.UserId is null) return Unauthorized();
        if (!access.HasPremiumPlus) return UpgradeRequired();
        var chart = await _db.Kundalis.FirstOrDefaultAsync(x => x.Id == kundaliId && x.UserId == access.UserId.Value && x.ClientId == id, cancellationToken);
        if (chart is null) return NotFound();
        chart.ClientId = null;
        await _db.SaveChangesAsync(cancellationToken);
        return NoContent();
    }

    private async Task<PractitionerClientDto> MapClientDetailsAsync(PractitionerClient client, CancellationToken cancellationToken)
    {
        var charts = await _db.Kundalis.AsNoTracking().Where(x => x.UserId == client.UserId && x.ClientId == client.Id)
            .OrderBy(x => x.FullName).Select(x => new PractitionerClientChartDto { Id = x.Id, FullName = x.FullName, DateOfBirth = x.DateOfBirth, TimeOfBirth = x.TimeOfBirth, BirthPlace = x.BirthPlace }).ToListAsync(cancellationToken);
        var appointments = await _db.PractitionerAppointments.AsNoTracking().Where(x => x.UserId == client.UserId && x.ClientId == client.Id)
            .Include(x => x.Kundali).OrderBy(x => x.StartsAtUtc).ToListAsync(cancellationToken);
        var scheduledIntervals = await _db.PractitionerAppointments.AsNoTracking().Where(x => x.UserId == client.UserId && (x.Status == "New" || x.Status == "Consultation Pending"))
            .Select(x => new { x.Id, x.StartsAtUtc, x.EndsAtUtc }).ToListAsync(cancellationToken);
        var appointmentDtos = appointments.Select(x =>
        {
            var zone = TimeZoneInfo.FindSystemTimeZoneById(x.TimeZoneId);
            var startLocal = DateTime.SpecifyKind(TimeZoneInfo.ConvertTimeFromUtc(x.StartsAtUtc, zone), DateTimeKind.Unspecified);
            var endLocal = DateTime.SpecifyKind(TimeZoneInfo.ConvertTimeFromUtc(x.EndsAtUtc, zone), DateTimeKind.Unspecified);
            var dto = new PractitionerAppointmentDto
            {
                Id = x.Id, ClientId = x.ClientId, ClientName = client.FullName, KundaliId = x.KundaliId, KundaliName = x.Kundali?.FullName,
                StartsAtUtc = x.StartsAtUtc, EndsAtUtc = x.EndsAtUtc, TimeZoneId = x.TimeZoneId,
                StartsAtLocal = startLocal.ToString("yyyy-MM-dd'T'HH:mm", System.Globalization.CultureInfo.InvariantCulture),
                EndsAtLocal = endLocal.ToString("yyyy-MM-dd'T'HH:mm", System.Globalization.CultureInfo.InvariantCulture),
                Status = x.Status, Topic = x.Topic, ReminderLeadMinutes = x.ReminderLeadMinutes, Notes = x.Notes, CompletedAtUtc = x.CompletedAtUtc
            };
            dto.OverlapsExisting = (dto.Status == "New" || dto.Status == "Consultation Pending") && scheduledIntervals.Any(other => other.Id != dto.Id && other.StartsAtUtc < dto.EndsAtUtc && dto.StartsAtUtc < other.EndsAtUtc);
            return dto;
        }).ToArray();
        return MapClient(client, charts, appointmentDtos);
    }

    private static PractitionerClientDto MapClient(PractitionerClient client, IReadOnlyList<PractitionerClientChartDto>? charts = null, IReadOnlyList<PractitionerAppointmentDto>? appointments = null) => new()
    {
        Id = client.Id, FullName = client.FullName, Surname = client.Surname, FatherName = client.FatherName, MotherName = client.MotherName,
        Gotra = client.Gotra, Gender = client.Gender, DateOfBirth = client.DateOfBirth, TimeOfBirth = client.TimeOfBirth, BirthPlace = client.BirthPlace,
        Latitude = client.Latitude, Longitude = client.Longitude, PhoneNumber = client.PhoneNumber, Email = client.Email, IsArchived = client.IsArchived,
        Topic = client.Topic, Status = client.Status, Notes = client.Notes,
        LinkedChartCount = charts?.Count ?? 0,
        CreatedAt = client.CreatedAt, Charts = charts ?? Array.Empty<PractitionerClientChartDto>(), Appointments = appointments ?? Array.Empty<PractitionerAppointmentDto>()
    };

    private static PractitionerAppointmentDto MapAppointment(PractitionerAppointment appointment)
    {
        var zone = TimeZoneInfo.FindSystemTimeZoneById(appointment.TimeZoneId);
        var startLocal = DateTime.SpecifyKind(TimeZoneInfo.ConvertTimeFromUtc(appointment.StartsAtUtc, zone), DateTimeKind.Unspecified);
        var endLocal = DateTime.SpecifyKind(TimeZoneInfo.ConvertTimeFromUtc(appointment.EndsAtUtc, zone), DateTimeKind.Unspecified);
        return new PractitionerAppointmentDto
        {
            Id = appointment.Id,
            ClientId = appointment.ClientId,
            ClientName = appointment.Client?.FullName,
            KundaliId = appointment.KundaliId,
            KundaliName = appointment.Kundali?.FullName,
            Topic = appointment.Topic,
            StartsAtUtc = appointment.StartsAtUtc,
            EndsAtUtc = appointment.EndsAtUtc,
            StartsAtLocal = startLocal.ToString("yyyy-MM-dd'T'HH:mm", System.Globalization.CultureInfo.InvariantCulture),
            EndsAtLocal = endLocal.ToString("yyyy-MM-dd'T'HH:mm", System.Globalization.CultureInfo.InvariantCulture),
            TimeZoneId = appointment.TimeZoneId,
            Status = appointment.Status,
            ReminderLeadMinutes = appointment.ReminderLeadMinutes,
            Notes = appointment.Notes,
            CompletedAtUtc = appointment.CompletedAtUtc
        };
    }

    private static void Apply(PractitionerClient client, PractitionerClientRequest request)
    {
        client.FullName = request.FullName.Trim(); client.Surname = Clean(request.Surname); client.FatherName = Clean(request.FatherName);
        client.MotherName = Clean(request.MotherName); client.Gotra = Clean(request.Gotra); client.Gender = request.Gender.Trim();
        client.DateOfBirth = request.DateOfBirth; client.TimeOfBirth = request.TimeOfBirth; client.BirthPlace = request.BirthPlace.Trim();
        client.Latitude = request.Latitude; client.Longitude = request.Longitude; client.PhoneNumber = Clean(request.PhoneNumber); client.Email = Clean(request.Email)?.ToLowerInvariant();
        client.Topic = Clean(request.Topic); client.Status = request.Status.Trim(); client.Notes = Clean(request.Notes);
    }

    private static string? Clean(string? value) => string.IsNullOrWhiteSpace(value) ? null : value.Trim();
}
