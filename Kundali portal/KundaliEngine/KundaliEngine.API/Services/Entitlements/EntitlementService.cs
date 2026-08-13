using KundaliEngine.Domain.Entities;

namespace KundaliEngine.API.Services.Entitlements;

public class EntitlementService : IEntitlementService
{
    public bool IsPremium(User user)
    {
        return user.PlanCode == "Premium" &&
            (user.PlanExpiresAt == null || user.PlanExpiresAt > DateTimeOffset.UtcNow);
    }
}