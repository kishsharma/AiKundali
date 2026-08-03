using KundaliEngine.API.DTOs.Auth;
using KundaliEngine.API.Security;
using KundaliEngine.Domain.Entities;
using KundaliEngine.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;

namespace KundaliEngine.API.Services.Auth
{
    public class AuthService : IAuthService
    {
        private readonly KundaliDbContext _dbContext;
        private readonly IJwtTokenGenerator _jwtTokenGenerator;

        public AuthService(
            KundaliDbContext dbContext,
            IJwtTokenGenerator jwtTokenGenerator)
        {
            _dbContext = dbContext;
            _jwtTokenGenerator = jwtTokenGenerator;
        }

        public async Task<AuthResponse> RegisterAsync(RegisterRequest request)
        {
            var email = request.Email.Trim().ToLower();

            var existingUser = await _dbContext.Users
                .AnyAsync(x => x.Email != null && x.Email.ToLower() == email);

            if (existingUser)
            {
                throw new InvalidOperationException("Email is already registered.");
            }

            var user = new User
            {
                Id = Guid.NewGuid(),
                FullName = request.FullName.Trim(),
                Email = email,
                PhoneNumber = request.PhoneNumber,
                PasswordHash = BCrypt.Net.BCrypt.HashPassword(request.Password),
                AuthProvider = "Local",
                Role = "User",
                IsActive = true,
                CreatedAt = DateTime.UtcNow
            };

            _dbContext.Users.Add(user);
            await _dbContext.SaveChangesAsync();

            var token = _jwtTokenGenerator.GenerateToken(user);

            return new AuthResponse
            {
                UserId = user.Id,
                FullName = user.FullName,
                Email = user.Email,
                PhoneNumber = user.PhoneNumber,
                Role = user.Role,
                Token = token
            };
        }

        public async Task<AuthResponse> LoginAsync(LoginRequest request)
        {
            var email = request.Email.Trim().ToLower();

            var user = await _dbContext.Users
                .FirstOrDefaultAsync(x => x.Email != null && x.Email.ToLower() == email);

            if (user == null)
            {
                throw new UnauthorizedAccessException("Invalid email or password.");
            }

            if (!user.IsActive)
            {
                throw new UnauthorizedAccessException("Your login has been disabled. Please contact administrator.");
            }

            if (string.IsNullOrWhiteSpace(user.PasswordHash))
            {
                throw new UnauthorizedAccessException("Invalid login provider.");
            }

            var isPasswordValid = BCrypt.Net.BCrypt.Verify(
                request.Password,
                user.PasswordHash
            );

            if (!isPasswordValid)
            {
                throw new UnauthorizedAccessException("Invalid email or password.");
            }

            user.LastLoginAt = DateTime.UtcNow;
            await _dbContext.SaveChangesAsync();

            var token = _jwtTokenGenerator.GenerateToken(user);

            return new AuthResponse
            {
                UserId = user.Id,
                FullName = user.FullName,
                Email = user.Email,
                PhoneNumber = user.PhoneNumber,
                Role = user.Role,
                Token = token
            };
        }

        public async Task ChangePasswordAsync(Guid userId, ChangePasswordRequest request)
        {
            var user = await _dbContext.Users
                .FirstOrDefaultAsync(x => x.Id == userId);

            if (user == null || !user.IsActive)
            {
                throw new UnauthorizedAccessException("User account was not found.");
            }

            if (string.IsNullOrWhiteSpace(user.PasswordHash))
            {
                throw new InvalidOperationException("Password cannot be changed for this login provider.");
            }

            var isCurrentPasswordValid = BCrypt.Net.BCrypt.Verify(
                request.CurrentPassword,
                user.PasswordHash
            );

            if (!isCurrentPasswordValid)
            {
                throw new UnauthorizedAccessException("Current password is incorrect.");
            }

            if (string.IsNullOrWhiteSpace(request.NewPassword) || request.NewPassword.Length < 6)
            {
                throw new InvalidOperationException("New password must be at least 6 characters.");
            }

            user.PasswordHash = BCrypt.Net.BCrypt.HashPassword(request.NewPassword);
            user.UpdatedAt = DateTime.UtcNow;

            await _dbContext.SaveChangesAsync();
        }
    }
}

