using KundaliEngine.API.DTOs.Auth;

namespace KundaliEngine.API.Services.Auth;

public interface IAuthSessionService
{
    Task<RefreshSessionGrant> StartSessionAsync(
        Guid userId,
        bool rememberMe,
        string? previousRefreshToken,
        CancellationToken cancellationToken);

    Task<RefreshSessionGrant?> RotateAsync(string? refreshToken, CancellationToken cancellationToken);

    Task RevokeAsync(string? refreshToken, CancellationToken cancellationToken);

    Task RevokeAllAsync(Guid userId, CancellationToken cancellationToken);
}

public sealed record RefreshSessionGrant(
    string RefreshToken,
    bool Persistent,
    DateTimeOffset ExpiresAtUtc,
    AuthResponse? AuthResponse = null);
