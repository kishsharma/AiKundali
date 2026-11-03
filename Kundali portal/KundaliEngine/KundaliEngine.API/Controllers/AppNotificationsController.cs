using KundaliEngine.API.DTOs.Practitioner;
using KundaliEngine.API.Services.Entitlements;
using KundaliEngine.Infrastructure.Data;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System.Security.Claims;

namespace KundaliEngine.API.Controllers;

[ApiController]
[Authorize]
[Route("api/notifications")]
public sealed class AppNotificationsController : PractitionerControllerBase
{
    private readonly KundaliDbContext _db;

    public AppNotificationsController(KundaliDbContext db, IEntitlementService entitlements) : base(db, entitlements) => _db = db;

    [HttpGet]
    public async Task<ActionResult<IReadOnlyList<AppNotificationDto>>> GetAll([FromQuery] bool unreadOnly = false, CancellationToken cancellationToken = default)
    {
        var access = await GetAccessAsync(cancellationToken);
        if (access.UserId is null) return Unauthorized();
        var query = _db.AppNotifications.AsNoTracking().Where(x => x.UserId == access.UserId.Value);
        if (unreadOnly) query = query.Where(x => x.ReadAt == null);
        var notifications = await query.OrderByDescending(x => x.CreatedAt).Take(50).ToListAsync(cancellationToken);
        return Ok(notifications.Select(x => new AppNotificationDto
        {
            Id = x.Id, Type = x.Type, Title = x.Title, Message = x.Message, Link = x.Link, CreatedAt = x.CreatedAt, IsRead = x.ReadAt.HasValue
        }).ToArray());
    }

    [HttpPost("{id:guid}/read")]
    public async Task<IActionResult> MarkRead(Guid id, CancellationToken cancellationToken)
    {
        var access = await GetAccessAsync(cancellationToken);
        if (access.UserId is null) return Unauthorized();
        var notification = await _db.AppNotifications.FirstOrDefaultAsync(x => x.Id == id && x.UserId == access.UserId.Value, cancellationToken);
        if (notification is null) return NotFound();
        notification.ReadAt ??= DateTime.UtcNow;
        await _db.SaveChangesAsync(cancellationToken);
        return NoContent();
    }

    [HttpPost("read-all")]
    public async Task<IActionResult> MarkAllRead(CancellationToken cancellationToken)
    {
        var access = await GetAccessAsync(cancellationToken);
        if (access.UserId is null) return Unauthorized();
        var unread = await _db.AppNotifications.Where(x => x.UserId == access.UserId.Value && x.ReadAt == null).ToListAsync(cancellationToken);
        var now = DateTime.UtcNow;
        unread.ForEach(x => x.ReadAt = now);
        await _db.SaveChangesAsync(cancellationToken);
        return NoContent();
    }
}
