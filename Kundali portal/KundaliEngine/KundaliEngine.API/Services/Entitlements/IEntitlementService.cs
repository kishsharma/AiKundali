using KundaliEngine.Domain.Entities;

namespace KundaliEngine.API.Services.Entitlements;

public interface IEntitlementService
{
    string GetEffectivePlanCode(User user);

    bool IsPaid(User user);

    bool IsPremium(User user);

    bool IsPlus(User user);

    int GetDefaultKundaliSaveLimit(string planCode);

    int GetMatchSaveLimit(User user);

    EntitlementSnapshot GetSnapshot(User user);

    void ApplyPlan(User user, string planCode, DateTimeOffset? expiresAt);
}
