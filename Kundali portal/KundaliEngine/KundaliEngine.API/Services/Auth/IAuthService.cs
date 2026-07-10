using KundaliEngine.API.DTOs.Auth;

namespace KundaliEngine.API.Services.Auth
{
    public interface IAuthService
    {
        Task<AuthResponse> RegisterAsync(RegisterRequest request);

        Task<AuthResponse> LoginAsync(LoginRequest request);

        Task ChangePasswordAsync(Guid userId, ChangePasswordRequest request);
    }
}
