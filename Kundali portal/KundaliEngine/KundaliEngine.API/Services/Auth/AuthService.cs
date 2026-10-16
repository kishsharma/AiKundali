using Google.Apis.Auth;
using KundaliEngine.API.DTOs.Auth;
using KundaliEngine.API.Security;
using KundaliEngine.API.Services.Entitlements;
using KundaliEngine.Domain.Entities;
using KundaliEngine.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;

namespace KundaliEngine.API.Services.Auth
{
    public class AuthService : IAuthService
    {
        private readonly KundaliDbContext _dbContext;
        private readonly IJwtTokenGenerator _jwtTokenGenerator;
        private readonly IEntitlementService _entitlementService;
        private readonly IConfiguration _configuration;

        public AuthService(
            KundaliDbContext dbContext,
            IJwtTokenGenerator jwtTokenGenerator,
            IEntitlementService entitlementService,
            IConfiguration configuration)
        {
            _dbContext = dbContext;
            _jwtTokenGenerator = jwtTokenGenerator;
            _entitlementService = entitlementService;
            _configuration = configuration;
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
                PreferredLanguage = PreferredLanguageHelper.Normalize(request.PreferredLanguage),
                PasswordHash = BCrypt.Net.BCrypt.HashPassword(request.Password),
                AuthProvider = "Local",
                Role = "User",
                IsActive = true,
                CreatedAt = DateTime.UtcNow
            };

            _dbContext.Users.Add(user);
            await _dbContext.SaveChangesAsync();

            return AuthResponseFactory.Create(user, _jwtTokenGenerator.GenerateToken(user), _entitlementService);
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

            return AuthResponseFactory.Create(user, _jwtTokenGenerator.GenerateToken(user), _entitlementService);
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

        public async Task<AuthResponse> LoginWithGoogleAsync(string idToken, CancellationToken cancellationToken)
        {
            if (string.IsNullOrWhiteSpace(idToken))
            {
                throw new UnauthorizedAccessException("Google sign-in could not be verified.");
            }

            var clientId = _configuration["Google:ClientId"];
            if (string.IsNullOrWhiteSpace(clientId))
            {
                throw new InvalidOperationException("Google sign-in is not configured.");
            }

            GoogleJsonWebSignature.Payload payload;
            try
            {
                payload = await GoogleJsonWebSignature.ValidateAsync(idToken, new GoogleJsonWebSignature.ValidationSettings
                {
                    Audience = new[] { clientId }
                });
            }
            catch (InvalidJwtException)
            {
                throw new UnauthorizedAccessException("Google sign-in could not be verified.");
            }

            if (!payload.EmailVerified || string.IsNullOrWhiteSpace(payload.Email))
            {
                throw new UnauthorizedAccessException(string.IsNullOrWhiteSpace(payload.Email)
                    ? "Google account did not include an email address."
                    : "Google did not verify this email address.");
            }

            if (string.IsNullOrWhiteSpace(payload.Subject))
            {
                throw new UnauthorizedAccessException("Google sign-in could not be verified.");
            }

            var email = payload.Email.Trim().ToLowerInvariant();
            if (email.Length > 150)
            {
                throw new UnauthorizedAccessException("Google account email cannot be used.");
            }

            var subject = payload.Subject.Trim();
            var user = await _dbContext.Users
                .FirstOrDefaultAsync(x => x.GoogleSubject == subject, cancellationToken);

            if (user == null)
            {
                user = await _dbContext.Users
                    .FirstOrDefaultAsync(x => x.Email != null && x.Email.ToLower() == email, cancellationToken);
            }

            var now = DateTime.UtcNow;
            if (user == null)
            {
                user = new User
                {
                    Id = Guid.NewGuid(),
                    FullName = ResolveGoogleName(payload, email),
                    Email = email,
                    PasswordHash = null,
                    AuthProvider = "Google",
                    GoogleSubject = subject,
                    Role = "User",
                    IsActive = true,
                    EmailVerifiedAt = now,
                    CreatedAt = now,
                    LastLoginAt = now
                };

                _dbContext.Users.Add(user);
            }
            else
            {
                if (!user.IsActive)
                {
                    throw new UnauthorizedAccessException("Your login has been disabled. Please contact administrator.");
                }

                if (!string.IsNullOrWhiteSpace(user.GoogleSubject)
                    && !string.Equals(user.GoogleSubject, subject, StringComparison.Ordinal))
                {
                    throw new UnauthorizedAccessException("This email is already linked to a different Google account.");
                }

                if (string.IsNullOrWhiteSpace(user.GoogleSubject))
                {
                    user.GoogleSubject = subject;
                }

                if (string.IsNullOrWhiteSpace(user.Email))
                {
                    user.Email = email;
                }

                user.EmailVerifiedAt ??= now;
                user.LastLoginAt = now;
                user.UpdatedAt = now;
            }

            ApplyGooglePicture(user, payload);

            try
            {
                await _dbContext.SaveChangesAsync(cancellationToken);
            }
            catch (DbUpdateException)
            {
                throw new InvalidOperationException("This Google account could not be linked. Please try again.");
            }

            return AuthResponseFactory.Create(user, _jwtTokenGenerator.GenerateToken(user), _entitlementService);
        }

        private static void ApplyGooglePicture(User user, GoogleJsonWebSignature.Payload payload)
        {
            var picture = NormalizeGooglePicture(payload.Picture);
            if (picture != null)
            {
                user.ProfileImageUrl = picture;
            }
        }

        private static string? NormalizeGooglePicture(string? picture)
        {
            if (string.IsNullOrWhiteSpace(picture))
            {
                return null;
            }

            var value = picture.Trim();
            if (value.Length > 1000
                || !Uri.TryCreate(value, UriKind.Absolute, out var uri)
                || !string.Equals(uri.Scheme, Uri.UriSchemeHttps, StringComparison.OrdinalIgnoreCase))
            {
                return null;
            }

            var host = uri.IdnHost;
            var allowedHost = host.Equals("googleusercontent.com", StringComparison.OrdinalIgnoreCase)
                || host.EndsWith(".googleusercontent.com", StringComparison.OrdinalIgnoreCase);
            return allowedHost ? value : null;
        }

        private static string ResolveGoogleName(GoogleJsonWebSignature.Payload payload, string email)
        {
            if (!string.IsNullOrWhiteSpace(payload.Name))
            {
                return Limit(payload.Name.Trim(), 150);
            }

            var combined = string.Join(' ', new[] { payload.GivenName, payload.FamilyName }
                .Where(part => !string.IsNullOrWhiteSpace(part))
                .Select(part => part!.Trim()));

            if (!string.IsNullOrWhiteSpace(combined))
            {
                return Limit(combined, 150);
            }

            var localPart = email.Split('@')[0];
            return Limit(string.IsNullOrWhiteSpace(localPart) ? "HoraLife User" : localPart, 150);
        }

        private static string Limit(string value, int maxLength)
        {
            return value.Length <= maxLength ? value : value[..maxLength];
        }
    }
}



