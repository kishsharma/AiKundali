using System.Security.Claims;
using KundaliEngine.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Caching.Memory;

namespace KundaliEngine.API.Security;

public sealed class LastSeenMiddleware
{
    private static readonly TimeSpan UpdateInterval = TimeSpan.FromMinutes(5);
    private readonly RequestDelegate _next;

    public LastSeenMiddleware(RequestDelegate next)
    {
        _next = next;
    }

    public async Task InvokeAsync(HttpContext context, IMemoryCache cache, KundaliDbContext dbContext)
    {
        var userIdValue = context.User.FindFirstValue(ClaimTypes.NameIdentifier);
        if (Guid.TryParse(userIdValue, out var userId) && !cache.TryGetValue(CacheKey(userId), out _))
        {
            try
            {
                var now = DateTime.UtcNow;
                var updated = await dbContext.Users
                    .Where(user => user.Id == userId)
                    .ExecuteUpdateAsync(setters => setters.SetProperty(user => user.LastSeenAt, now));

                if (updated > 0)
                {
                    cache.Set(CacheKey(userId), true, UpdateInterval);
                }
            }
            catch (Exception exception)
            {
                var logger = context.RequestServices.GetService<ILogger<LastSeenMiddleware>>();
                logger?.LogWarning(exception, "Could not record last seen for user {UserId}.", userId);
            }
        }

        await _next(context);
    }

    private static string CacheKey(Guid userId) => $"last-seen:{userId:N}";
}
