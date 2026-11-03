using System.Security.Claims;
using KundaliEngine.API.Services.Entitlements;
using KundaliEngine.Infrastructure.Data;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace KundaliEngine.API.Controllers;

public abstract class PractitionerControllerBase : ControllerBase
{
    private readonly KundaliDbContext _db;
    private readonly IEntitlementService _entitlements;

    protected PractitionerControllerBase(KundaliDbContext db, IEntitlementService entitlements)
    {
        _db = db;
        _entitlements = entitlements;
    }

    protected async Task<(Guid? UserId, bool HasPremiumPlus)> GetAccessAsync(CancellationToken cancellationToken)
    {
        var claim = User.FindFirstValue(ClaimTypes.NameIdentifier);
        if (!Guid.TryParse(claim, out var userId)) return (null, false);
        var user = await _db.Users.AsNoTracking().FirstOrDefaultAsync(x => x.Id == userId && x.IsActive, cancellationToken);
        if (user is null) return (null, false);
        return (userId, _entitlements.IsPlus(user));
    }

    protected ObjectResult UpgradeRequired() => StatusCode(StatusCodes.Status402PaymentRequired, new
    {
        code = "PremiumPlusRequired",
        message = "The practitioner CRM is available on Premium Plus."
    });
}
