using KundaliEngine.Domain.Entities;

namespace KundaliEngine.API.Services.Entitlements;

public interface IEntitlementService
{
    bool IsPremium(User user);
}