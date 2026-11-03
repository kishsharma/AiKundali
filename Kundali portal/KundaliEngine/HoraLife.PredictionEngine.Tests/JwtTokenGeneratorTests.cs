using System.IdentityModel.Tokens.Jwt;
using KundaliEngine.API.Security;
using KundaliEngine.Domain.Entities;
using Microsoft.Extensions.Configuration;

namespace HoraLife.PredictionEngine.Tests;

public sealed class JwtTokenGeneratorTests
{
    [Fact]
    public void Default_access_token_lifetime_is_fifteen_minutes()
    {
        var before = DateTime.UtcNow;
        var token = CreateGenerator().GenerateToken(TestUser());
        var expiresAt = new JwtSecurityTokenHandler().ReadJwtToken(token).ValidTo;

        Assert.InRange(expiresAt, before.AddMinutes(14).AddSeconds(59), DateTime.UtcNow.AddMinutes(15).AddSeconds(1));
    }

    [Fact]
    public void Explicit_store_handoff_lifetime_remains_two_minutes()
    {
        var before = DateTime.UtcNow;
        var token = CreateGenerator().GenerateToken(TestUser(), TimeSpan.FromMinutes(2));
        var expiresAt = new JwtSecurityTokenHandler().ReadJwtToken(token).ValidTo;

        Assert.InRange(expiresAt, before.AddMinutes(1).AddSeconds(59), DateTime.UtcNow.AddMinutes(2).AddSeconds(1));
    }

    private static JwtTokenGenerator CreateGenerator()
    {
        var values = new Dictionary<string, string?>
        {
            ["Jwt:Key"] = "this-is-a-test-signing-key-with-more-than-32-bytes",
            ["Jwt:Issuer"] = "KundaliEngine.Tests",
            ["Jwt:Audience"] = "KundaliEngine.Tests",
            ["Jwt:ExpiryMinutes"] = "15"
        };
        return new JwtTokenGenerator(new ConfigurationBuilder().AddInMemoryCollection(values).Build());
    }

    private static User TestUser() => new()
    {
        Id = Guid.NewGuid(),
        FullName = "Test User",
        Email = "test@example.com",
        Role = "User"
    };
}
