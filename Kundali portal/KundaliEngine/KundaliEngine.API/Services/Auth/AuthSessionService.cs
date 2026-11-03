using System.Security.Cryptography;
using System.Text;
using KundaliEngine.API.DTOs.Auth;
using KundaliEngine.API.Security;
using KundaliEngine.API.Services.Entitlements;
using KundaliEngine.Domain.Entities;
using KundaliEngine.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;

namespace KundaliEngine.API.Services.Auth;

public sealed class AuthSessionService : IAuthSessionService
{
    private readonly KundaliDbContext _db;
    private readonly IJwtTokenGenerator _jwtTokenGenerator;
    private readonly IEntitlementService _entitlements;
    private readonly IConfiguration _configuration;

    public AuthSessionService(
        KundaliDbContext db,
        IJwtTokenGenerator jwtTokenGenerator,
        IEntitlementService entitlements,
        IConfiguration configuration)
    {
        _db = db;
        _jwtTokenGenerator = jwtTokenGenerator;
        _entitlements = entitlements;
        _configuration = configuration;
    }

    public async Task<RefreshSessionGrant> StartSessionAsync(
        Guid userId,
        bool rememberMe,
        string? previousRefreshToken,
        CancellationToken cancellationToken)
    {
        var now = DateTimeOffset.UtcNow;
        var userIsActive = await _db.Users.AnyAsync(
            x => x.Id == userId && x.IsActive,
            cancellationToken);
        if (!userIsActive)
        {
            throw new UnauthorizedAccessException("User account is no longer active.");
        }

        await using var transaction = await _db.Database.BeginTransactionAsync(cancellationToken);
        await RevokeTokenAsync(previousRefreshToken, now, cancellationToken);

        var session = NewSession(userId, Guid.NewGuid(), rememberMe, now);
        _db.AuthRefreshSessions.Add(session.Row);
        await _db.SaveChangesAsync(cancellationToken);
        await transaction.CommitAsync(cancellationToken);

        return new RefreshSessionGrant(session.Token, rememberMe, session.Row.ExpiresAtUtc);
    }

    public async Task<RefreshSessionGrant?> RotateAsync(string? refreshToken, CancellationToken cancellationToken)
    {
        if (string.IsNullOrWhiteSpace(refreshToken) || refreshToken.Length > 200)
        {
            return null;
        }

        var now = DateTimeOffset.UtcNow;
        var tokenHash = HashToken(refreshToken);
        var row = await _db.AuthRefreshSessions
            .FirstOrDefaultAsync(x => x.TokenHash == tokenHash, cancellationToken);
        if (row == null)
        {
            return null;
        }

        if (row.RevokedAtUtc != null)
        {
            await RevokeFamilyAsync(row.FamilyId, now, cancellationToken);
            return null;
        }

        if (row.ExpiresAtUtc <= now)
        {
            await RevokeFamilyAsync(row.FamilyId, now, cancellationToken);
            return null;
        }

        var user = await _db.Users.FirstOrDefaultAsync(x => x.Id == row.UserId, cancellationToken);
        if (user == null || !user.IsActive)
        {
            await RevokeFamilyAsync(row.FamilyId, now, cancellationToken);
            return null;
        }

        var next = NewSession(row.UserId, row.FamilyId, row.RememberMe, now, row.ExpiresAtUtc);
        await using var transaction = await _db.Database.BeginTransactionAsync(cancellationToken);
        var rotated = await _db.AuthRefreshSessions
            .Where(x => x.Id == row.Id && x.RevokedAtUtc == null && x.ExpiresAtUtc > now)
            .ExecuteUpdateAsync(update => update
                .SetProperty(x => x.RevokedAtUtc, now)
                .SetProperty(x => x.ReplacedByTokenHash, next.Row.TokenHash), cancellationToken);

        if (rotated == 0)
        {
            await RevokeFamilyAsync(row.FamilyId, now, cancellationToken);
            await transaction.CommitAsync(cancellationToken);
            return null;
        }

        _db.AuthRefreshSessions.Add(next.Row);
        await _db.SaveChangesAsync(cancellationToken);
        await transaction.CommitAsync(cancellationToken);

        var response = AuthResponseFactory.Create(user, _jwtTokenGenerator.GenerateToken(user), _entitlements);
        return new RefreshSessionGrant(next.Token, next.Row.RememberMe, next.Row.ExpiresAtUtc, response);
    }

    public async Task RevokeAsync(string? refreshToken, CancellationToken cancellationToken)
    {
        if (string.IsNullOrWhiteSpace(refreshToken) || refreshToken.Length > 200)
        {
            return;
        }

        var row = await _db.AuthRefreshSessions
            .FirstOrDefaultAsync(x => x.TokenHash == HashToken(refreshToken), cancellationToken);
        if (row != null)
        {
            await RevokeFamilyAsync(row.FamilyId, DateTimeOffset.UtcNow, cancellationToken);
        }
    }

    public async Task RevokeAllAsync(Guid userId, CancellationToken cancellationToken)
    {
        var now = DateTimeOffset.UtcNow;
        await _db.AuthRefreshSessions
            .Where(x => x.UserId == userId && x.RevokedAtUtc == null)
            .ExecuteUpdateAsync(update => update.SetProperty(x => x.RevokedAtUtc, now), cancellationToken);
    }

    private async Task RevokeTokenAsync(string? refreshToken, DateTimeOffset now, CancellationToken cancellationToken)
    {
        if (string.IsNullOrWhiteSpace(refreshToken) || refreshToken.Length > 200)
        {
            return;
        }

        await _db.AuthRefreshSessions
            .Where(x => x.TokenHash == HashToken(refreshToken) && x.RevokedAtUtc == null)
            .ExecuteUpdateAsync(update => update.SetProperty(x => x.RevokedAtUtc, now), cancellationToken);
    }

    private async Task RevokeFamilyAsync(Guid familyId, DateTimeOffset now, CancellationToken cancellationToken)
    {
        await _db.AuthRefreshSessions
            .Where(x => x.FamilyId == familyId && x.RevokedAtUtc == null)
            .ExecuteUpdateAsync(update => update.SetProperty(x => x.RevokedAtUtc, now), cancellationToken);
    }

    private (AuthRefreshSession Row, string Token) NewSession(
        Guid userId,
        Guid familyId,
        bool rememberMe,
        DateTimeOffset now,
        DateTimeOffset? fixedExpiry = null)
    {
        var token = Convert.ToBase64String(RandomNumberGenerator.GetBytes(64))
            .TrimEnd('=')
            .Replace('+', '-')
            .Replace('/', '_');
        var lifetime = rememberMe
            ? TimeSpan.FromDays(_configuration.GetValue("AuthSession:RememberMeDays", 30))
            : TimeSpan.FromHours(_configuration.GetValue("AuthSession:StandardHours", 2));

        return (new AuthRefreshSession
        {
            Id = Guid.NewGuid(),
            FamilyId = familyId,
            UserId = userId,
            TokenHash = HashToken(token),
            RememberMe = rememberMe,
            CreatedAtUtc = now,
            ExpiresAtUtc = fixedExpiry ?? now.Add(lifetime)
        }, token);
    }

    private static string HashToken(string token)
        => Convert.ToHexString(SHA256.HashData(Encoding.UTF8.GetBytes(token)));
}
