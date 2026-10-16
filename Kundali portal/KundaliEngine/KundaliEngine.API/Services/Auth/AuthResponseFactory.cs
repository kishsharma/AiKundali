using KundaliEngine.API.DTOs.Auth;
using KundaliEngine.API.Services.Entitlements;
using KundaliEngine.Domain.Entities;

namespace KundaliEngine.API.Services.Auth;

public static class AuthResponseFactory
{
    public static AuthResponse Create(User user, string token, IEntitlementService entitlements)
    {
        var snapshot = entitlements.GetSnapshot(user);
        return new AuthResponse
        {
            UserId = user.Id,
            FullName = user.FullName,
            Email = user.Email,
            ProfileImageUrl = user.ProfileImageUrl,
            PhoneNumber = user.PhoneNumber,
            PreferredLanguage = user.PreferredLanguage,
            Role = user.Role,
            PlanCode = snapshot.PlanCode,
            PlanExpiresAt = snapshot.PlanExpiresAt,
            IsPremium = snapshot.IsPremium,
            IsPaid = snapshot.IsPaid,
            IsPlus = snapshot.IsPlus,
            KundaliSaveLimit = snapshot.KundaliSaveLimit,
            MatchSaveLimit = snapshot.MatchSaveLimit,
            Token = token
        };
    }
}
