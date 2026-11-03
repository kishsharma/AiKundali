using System.Security.Cryptography;
using KundaliEngine.API.DTOs.Auth;
using KundaliEngine.API.Security;
using KundaliEngine.API.Services.Entitlements;
using KundaliEngine.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Caching.Memory;

namespace KundaliEngine.API.Services.Auth;

public sealed class StoreHandoffService : IStoreHandoffService
{
    private static readonly TimeSpan Lifetime = TimeSpan.FromMinutes(2);
    private readonly IMemoryCache _cache;
    private readonly KundaliDbContext _dbContext;
    private readonly IJwtTokenGenerator _jwtTokenGenerator;
    private readonly IEntitlementService _entitlementService;

    public StoreHandoffService(
        IMemoryCache cache,
        KundaliDbContext dbContext,
        IJwtTokenGenerator jwtTokenGenerator,
        IEntitlementService entitlementService)
    {
        _cache = cache;
        _dbContext = dbContext;
        _jwtTokenGenerator = jwtTokenGenerator;
        _entitlementService = entitlementService;
    }

    public StoreHandoffResponse Issue(Guid userId)
    {
        var code = Convert.ToHexString(RandomNumberGenerator.GetBytes(32));
        _cache.Set(CacheKey(code), userId, Lifetime);
        return new StoreHandoffResponse
        {
            Code = code,
            ExpiresAt = DateTimeOffset.UtcNow.Add(Lifetime)
        };
    }

    public async Task<AuthResponse> RedeemAsync(string code, CancellationToken cancellationToken)
    {
        var normalized = (code ?? string.Empty).Trim();
        if (normalized.Length == 0 || !_cache.TryGetValue(CacheKey(normalized), out Guid userId))
        {
            throw new UnauthorizedAccessException("This store sign-in link has expired. Open the store from HoraLife again.");
        }

        _cache.Remove(CacheKey(normalized));

        var user = await _dbContext.Users.FirstOrDefaultAsync(x => x.Id == userId, cancellationToken);
        if (user == null || !user.IsActive)
        {
            throw new UnauthorizedAccessException("This store sign-in link has expired. Open the store from HoraLife again.");
        }

        return AuthResponseFactory.Create(
            user,
            _jwtTokenGenerator.GenerateToken(user, TimeSpan.FromMinutes(2)),
            _entitlementService);
    }

    private static string CacheKey(string code) => $"store-handoff:{code}";
}
