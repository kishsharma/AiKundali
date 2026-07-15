using KundaliEngine.Domain.Entities;

namespace KundaliEngine.API.Security
{
    public interface IJwtTokenGenerator
    {
        string GenerateToken(User user);
    }
}
