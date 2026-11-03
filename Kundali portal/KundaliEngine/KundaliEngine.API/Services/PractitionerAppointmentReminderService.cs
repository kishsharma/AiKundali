using KundaliEngine.Domain.Entities;
using KundaliEngine.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;

namespace KundaliEngine.API.Services;

public sealed class PractitionerAppointmentReminderService : BackgroundService
{
    private readonly IServiceScopeFactory _scopeFactory;
    private readonly ILogger<PractitionerAppointmentReminderService> _logger;

    public PractitionerAppointmentReminderService(IServiceScopeFactory scopeFactory, ILogger<PractitionerAppointmentReminderService> logger)
    {
        _scopeFactory = scopeFactory;
        _logger = logger;
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        using var timer = new PeriodicTimer(TimeSpan.FromMinutes(1));
        while (!stoppingToken.IsCancellationRequested)
        {
            try { await DispatchDueRemindersAsync(stoppingToken); }
            catch (OperationCanceledException) when (stoppingToken.IsCancellationRequested) { break; }
            catch (Exception exception) { _logger.LogError(exception, "Appointment reminder dispatch failed."); }
            try { await timer.WaitForNextTickAsync(stoppingToken); }
            catch (OperationCanceledException) when (stoppingToken.IsCancellationRequested) { break; }
        }
    }

    private async Task DispatchDueRemindersAsync(CancellationToken cancellationToken)
    {
        await using var scope = _scopeFactory.CreateAsyncScope();
        var db = scope.ServiceProvider.GetRequiredService<KundaliDbContext>();
        var now = DateTime.UtcNow;
        var candidates = await db.PractitionerAppointments
            .Where(x => (x.Status == "New" || x.Status == "Consultation Pending") && x.ReminderLeadMinutes != null && (x.ReminderSentVersion == null || x.ReminderSentVersion != x.ReminderVersion) && x.StartsAtUtc > now && x.StartsAtUtc <= now.AddDays(7))
            .OrderBy(x => x.StartsAtUtc).Take(100).ToListAsync(cancellationToken);

        foreach (var appointment in candidates)
        {
            if (appointment.ReminderLeadMinutes is not { } leadMinutes || appointment.StartsAtUtc.AddMinutes(-leadMinutes) > now) continue;
            var client = await db.PractitionerClients.AsNoTracking().FirstOrDefaultAsync(x => x.Id == appointment.ClientId && x.UserId == appointment.UserId, cancellationToken);
            var chart = appointment.KundaliId.HasValue
                ? await db.Kundalis.AsNoTracking().FirstOrDefaultAsync(x => x.Id == appointment.KundaliId.Value && x.UserId == appointment.UserId, cancellationToken)
                : null;
            if (client is null)
            {
                appointment.ReminderSentVersion = appointment.ReminderVersion;
                continue;
            }

            var zone = TimeZoneInfo.FindSystemTimeZoneById(appointment.TimeZoneId);
            var localStart = TimeZoneInfo.ConvertTimeFromUtc(appointment.StartsAtUtc, zone);
            var notification = new AppNotification
            {
                Id = Guid.NewGuid(), UserId = appointment.UserId, Type = "AppointmentReminder",
                Title = $"Consultation with {client.FullName}",
                Message = chart is null
                    ? $"Your promised consultation starts at {localStart:ddd, dd MMM yyyy HH:mm} ({appointment.TimeZoneId})."
                    : $"Your consultation about {chart.FullName} starts at {localStart:ddd, dd MMM yyyy HH:mm} ({appointment.TimeZoneId}).",
                Link = $"/app/clients/{client.Id}?appointment={appointment.Id}",
                DedupeKey = $"appointment:{appointment.Id}:{appointment.ReminderVersion}"
            };
            db.AppNotifications.Add(notification);
            appointment.ReminderSentVersion = appointment.ReminderVersion;
            try { await db.SaveChangesAsync(cancellationToken); }
            catch (DbUpdateException exception)
            {
                db.Entry(notification).State = EntityState.Detached;
                db.Entry(appointment).State = EntityState.Detached;
                _logger.LogDebug(exception, "Duplicate reminder delivery was suppressed for appointment {AppointmentId}.", appointment.Id);
            }
        }

        if (candidates.Any(x => x.ReminderSentVersion == x.ReminderVersion)) await db.SaveChangesAsync(cancellationToken);
    }
}
