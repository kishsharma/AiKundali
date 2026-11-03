using KundaliEngine.API.DTOs.Auth;
using KundaliEngine.API.Services.Audit;
using KundaliEngine.API.Services.Auth;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.RateLimiting;
using System.Security.Claims;

namespace KundaliEngine.API.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class AuthController : ControllerBase
    {
        private readonly IAuthService _authService;
        private readonly IAuthOtpService _authOtpService;
        private readonly IAuditLogService _auditLogService;
        private readonly IStoreHandoffService _storeHandoffService;
        private readonly IAuthSessionService _authSessionService;
        private readonly IConfiguration _configuration;
        private readonly IWebHostEnvironment _environment;

        public AuthController(
            IAuthService authService,
            IAuthOtpService authOtpService,
            IAuditLogService auditLogService,
            IStoreHandoffService storeHandoffService,
            IAuthSessionService authSessionService,
            IConfiguration configuration,
            IWebHostEnvironment environment)
        {
            _authService = authService;
            _authOtpService = authOtpService;
            _auditLogService = auditLogService;
            _storeHandoffService = storeHandoffService;
            _authSessionService = authSessionService;
            _configuration = configuration;
            _environment = environment;
        }

        [HttpPost("register")]
        public async Task<IActionResult> Register(RegisterRequest request)
        {
            try
            {
                var response = await _authService.RegisterAsync(request);
                await IssueRefreshCookieAsync(response, request.RememberMe, cancellationToken: HttpContext.RequestAborted);
                await _auditLogService.LogAsync(new AuditLogEntry
                {
                    Action = "UserRegistered",
                    Category = "Authentication",
                    UserId = response.UserId,
                    UserName = response.FullName,
                    EntityType = "User",
                    EntityId = response.UserId.ToString(),
                    Description = $"User registered with email {response.Email}.",
                    Status = "Success"
                });

                return Ok(response);
            }
            catch (InvalidOperationException ex)
            {
                await _auditLogService.LogAsync(new AuditLogEntry
                {
                    Action = "UserRegistrationFailed",
                    Category = "Authentication",
                    Description = $"Registration failed for email {request.Email}.",
                    Status = "Failed",
                    FailureReason = ex.Message
                });

                return BadRequest(new
                {
                    message = ex.Message
                });
            }
        }

        [HttpPost("login")]
        public async Task<IActionResult> Login(LoginRequest request)
        {
            try
            {
                var response = await _authService.LoginAsync(request);
                await IssueRefreshCookieAsync(response, request.RememberMe, cancellationToken: HttpContext.RequestAborted);
                await _auditLogService.LogAsync(new AuditLogEntry
                {
                    Action = "LoginSuccess",
                    Category = "Authentication",
                    UserId = response.UserId,
                    UserName = response.FullName,
                    EntityType = "User",
                    EntityId = response.UserId.ToString(),
                    Description = $"User logged in with email {response.Email}.",
                    Status = "Success"
                });

                return Ok(response);
            }
            catch (UnauthorizedAccessException ex)
            {
                await _auditLogService.LogAsync(new AuditLogEntry
                {
                    Action = "LoginFailed",
                    Category = "Authentication",
                    Description = $"Login failed for email {request.Email}.",
                    Status = "Failed",
                    FailureReason = ex.Message
                });

                return Unauthorized(new
                {
                    message = ex.Message
                });
            }
        }

        [EnableRateLimiting("GoogleAuth")]
        [HttpPost("google")]
        public async Task<IActionResult> GoogleLogin(GoogleLoginRequest request, CancellationToken cancellationToken)
        {
            try
            {
                var response = await _authService.LoginWithGoogleAsync(request.IdToken, cancellationToken);
                await IssueRefreshCookieAsync(response, request.RememberMe, cancellationToken);
                await _auditLogService.LogAsync(new AuditLogEntry
                {
                    Action = "GoogleLoginSuccess",
                    Category = "Authentication",
                    UserId = response.UserId,
                    UserName = response.FullName,
                    EntityType = "User",
                    EntityId = response.UserId.ToString(),
                    Description = $"User signed in with Google for email {response.Email}.",
                    Status = "Success"
                });

                return Ok(response);
            }
            catch (UnauthorizedAccessException ex)
            {
                await _auditLogService.LogAsync(new AuditLogEntry
                {
                    Action = "GoogleLoginFailed",
                    Category = "Authentication",
                    Description = "Google sign-in failed.",
                    Status = "Failed",
                    FailureReason = ex.Message
                });

                return Unauthorized(new
                {
                    message = ex.Message
                });
            }
            catch (InvalidOperationException ex)
            {
                await _auditLogService.LogAsync(new AuditLogEntry
                {
                    Action = "GoogleLoginFailed",
                    Category = "Authentication",
                    Description = "Google sign-in failed.",
                    Status = "Failed",
                    FailureReason = ex.Message
                });

                var statusCode = ex.Message == "Google sign-in is not configured."
                    ? StatusCodes.Status500InternalServerError
                    : StatusCodes.Status400BadRequest;

                return StatusCode(statusCode, new
                {
                    message = ex.Message
                });
            }
        }


        [EnableRateLimiting("AuthOtp")]
        [HttpPost("otp/start-login")]
        public async Task<IActionResult> StartOtpLogin(StartEmailOtpRequest request, CancellationToken cancellationToken)
        {
            try
            {
                var response = await _authOtpService.StartOtpLoginAsync(request, cancellationToken);
                await _auditLogService.LogAsync(new AuditLogEntry
                {
                    Action = "OtpLoginRequested",
                    Category = "Authentication",
                    Description = $"OTP login requested for email {request.Email}.",
                    Status = "Success"
                });

                return Ok(response);
            }
            catch (InvalidOperationException ex)
            {
                await _auditLogService.LogAsync(new AuditLogEntry
                {
                    Action = "OtpLoginRequestFailed",
                    Category = "Authentication",
                    Description = $"OTP login request failed for email {request.Email}.",
                    Status = "Failed",
                    FailureReason = ex.Message
                });

                return BadRequest(new { message = ex.Message });
            }
        }

        [EnableRateLimiting("AuthOtp")]
        [HttpPost("otp/verify-login")]
        public async Task<IActionResult> VerifyOtpLogin(VerifyEmailOtpRequest request, CancellationToken cancellationToken)
        {
            try
            {
                var response = await _authOtpService.VerifyOtpLoginAsync(request, cancellationToken);
                await IssueRefreshCookieAsync(response, request.RememberMe, cancellationToken);
                await _auditLogService.LogAsync(new AuditLogEntry
                {
                    Action = "OtpLoginSuccess",
                    Category = "Authentication",
                    UserId = response.UserId,
                    UserName = response.FullName,
                    EntityType = "User",
                    EntityId = response.UserId.ToString(),
                    Description = $"User logged in with email OTP for {response.Email}.",
                    Status = "Success"
                });

                return Ok(response);
            }
            catch (UnauthorizedAccessException ex)
            {
                await _auditLogService.LogAsync(new AuditLogEntry
                {
                    Action = "OtpLoginFailed",
                    Category = "Authentication",
                    Description = $"OTP login failed for email {request.Email}.",
                    Status = "Failed",
                    FailureReason = ex.Message
                });

                return Unauthorized(new { message = ex.Message });
            }
        }

        [EnableRateLimiting("AuthOtp")]
        [HttpPost("otp/start-registration")]
        public async Task<IActionResult> StartOtpRegistration(StartEmailOtpRequest request, CancellationToken cancellationToken)
        {
            try
            {
                var response = await _authOtpService.StartOtpRegistrationAsync(request, cancellationToken);
                await _auditLogService.LogAsync(new AuditLogEntry
                {
                    Action = "OtpRegistrationRequested",
                    Category = "Authentication",
                    Description = $"OTP registration requested for email {request.Email}.",
                    Status = "Success"
                });

                return Ok(response);
            }
            catch (InvalidOperationException ex)
            {
                await _auditLogService.LogAsync(new AuditLogEntry
                {
                    Action = "OtpRegistrationRequestFailed",
                    Category = "Authentication",
                    Description = $"OTP registration request failed for email {request.Email}.",
                    Status = "Failed",
                    FailureReason = ex.Message
                });

                return BadRequest(new { message = ex.Message });
            }
        }

        [EnableRateLimiting("AuthOtp")]
        [HttpPost("otp/verify-registration")]
        public async Task<IActionResult> VerifyOtpRegistration(VerifyEmailOtpRequest request, CancellationToken cancellationToken)
        {
            try
            {
                var response = await _authOtpService.VerifyOtpRegistrationAsync(request, cancellationToken);
                await _auditLogService.LogAsync(new AuditLogEntry
                {
                    Action = "OtpRegistrationVerified",
                    Category = "Authentication",
                    Description = $"OTP registration verified for email {request.Email}.",
                    Status = "Success"
                });

                return Ok(response);
            }
            catch (InvalidOperationException ex)
            {
                return BadRequest(new { message = ex.Message });
            }
            catch (UnauthorizedAccessException ex)
            {
                await _auditLogService.LogAsync(new AuditLogEntry
                {
                    Action = "OtpRegistrationVerificationFailed",
                    Category = "Authentication",
                    Description = $"OTP registration verification failed for email {request.Email}.",
                    Status = "Failed",
                    FailureReason = ex.Message
                });

                return Unauthorized(new { message = ex.Message });
            }
        }

        [EnableRateLimiting("AuthOtp")]
        [HttpPost("otp/complete-registration")]
        public async Task<IActionResult> CompleteOtpRegistration(CompleteOtpRegistrationRequest request, CancellationToken cancellationToken)
        {
            try
            {
                var response = await _authOtpService.CompleteRegistrationAsync(request, cancellationToken);
                await IssueRefreshCookieAsync(response, request.RememberMe, cancellationToken);
                await _auditLogService.LogAsync(new AuditLogEntry
                {
                    Action = "OtpUserRegistered",
                    Category = "Authentication",
                    UserId = response.UserId,
                    UserName = response.FullName,
                    EntityType = "User",
                    EntityId = response.UserId.ToString(),
                    Description = $"User registered with verified email {response.Email}.",
                    Status = "Success"
                });

                return Ok(response);
            }
            catch (InvalidOperationException ex)
            {
                return BadRequest(new { message = ex.Message });
            }
            catch (UnauthorizedAccessException ex)
            {
                return Unauthorized(new { message = ex.Message });
            }
        }

        [Authorize]
        [HttpPost("store-handoff")]
        public async Task<IActionResult> IssueStoreHandoff()
        {
            var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
            if (!Guid.TryParse(userId, out var parsedUserId))
            {
                return Unauthorized();
            }

            var response = _storeHandoffService.Issue(parsedUserId);
            await _auditLogService.LogAsync(new AuditLogEntry
            {
                Action = "StoreHandoffIssued",
                Category = "Authentication",
                UserId = parsedUserId,
                UserName = User.Identity?.Name,
                EntityType = "User",
                EntityId = parsedUserId.ToString(),
                Description = "Issued a one-time HoraLife store sign-in from the active portal session.",
                Status = "Success"
            });

            return Ok(response);
        }

        [AllowAnonymous]
        [EnableRateLimiting("AuthOtp")]
        [HttpPost("refresh")]
        public async Task<IActionResult> Refresh(CancellationToken cancellationToken)
        {
            if (!IsTrustedBrowserOrigin())
            {
                return Forbid();
            }

            var grant = await _authSessionService.RotateAsync(
                Request.Cookies[RefreshCookieName],
                cancellationToken);
            if (grant?.AuthResponse == null)
            {
                ClearRefreshCookie();
                return Unauthorized(new { message = "Your session has expired. Please sign in again." });
            }

            WriteRefreshCookie(grant);
            return Ok(grant.AuthResponse);
        }

        [AllowAnonymous]
        [HttpPost("logout")]
        public async Task<IActionResult> Logout(CancellationToken cancellationToken)
        {
            if (!IsTrustedBrowserOrigin())
            {
                return Forbid();
            }

            await _authSessionService.RevokeAsync(Request.Cookies[RefreshCookieName], cancellationToken);
            ClearRefreshCookie();
            return NoContent();
        }

        [AllowAnonymous]
        [HttpPost("store-handoff/redeem")]
        public async Task<IActionResult> RedeemStoreHandoff(StoreHandoffRedeemRequest request, CancellationToken cancellationToken)
        {
            try
            {
                var response = await _storeHandoffService.RedeemAsync(request.Code, cancellationToken);
                await _auditLogService.LogAsync(new AuditLogEntry
                {
                    Action = "StoreAutoLogin",
                    Category = "Authentication",
                    UserId = response.UserId,
                    UserName = response.FullName,
                    EntityType = "User",
                    EntityId = response.UserId.ToString(),
                    Description = $"User {response.Email} entered the HoraLife store from an active HoraLife session.",
                    Status = "Success"
                });

                return Ok(response);
            }
            catch (UnauthorizedAccessException ex)
            {
                await _auditLogService.LogAsync(new AuditLogEntry
                {
                    Action = "StoreAutoLoginFailed",
                    Category = "Authentication",
                    Description = "HoraLife store auto sign-in failed.",
                    Status = "Failed",
                    FailureReason = ex.Message
                });

                return Unauthorized(new { message = ex.Message });
            }
        }

        [Authorize]
        [HttpPost("change-password")]
        public async Task<IActionResult> ChangePassword(ChangePasswordRequest request)
        {
            var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
            if (!Guid.TryParse(userId, out var parsedUserId))
            {
                return Unauthorized();
            }

            try
            {
                await _authService.ChangePasswordAsync(parsedUserId, request);
                await _authSessionService.RevokeAllAsync(parsedUserId, HttpContext.RequestAborted);
                ClearRefreshCookie();
                await _auditLogService.LogAsync(new AuditLogEntry
                {
                    Action = "PasswordChanged",
                    Category = "Security",
                    EntityType = "User",
                    EntityId = parsedUserId.ToString(),
                    Description = "User changed password.",
                    Status = "Success"
                });

                return Ok(new
                {
                    message = "Password changed successfully."
                });
            }
            catch (InvalidOperationException ex)
            {
                await LogPasswordChangeFailure(parsedUserId, ex.Message);
                return BadRequest(new
                {
                    message = ex.Message
                });
            }
            catch (UnauthorizedAccessException ex)
            {
                await LogPasswordChangeFailure(parsedUserId, ex.Message);
                return Unauthorized(new
                {
                    message = ex.Message
                });
            }
        }

        private Task LogPasswordChangeFailure(Guid userId, string reason)
        {
            return _auditLogService.LogAsync(new AuditLogEntry
            {
                Action = "PasswordChangeFailed",
                Category = "Security",
                EntityType = "User",
                EntityId = userId.ToString(),
                Description = "User password change failed.",
                Status = "Failed",
                FailureReason = reason
            });
        }

        private const string RefreshCookieName = "horalife_refresh";

        private async Task IssueRefreshCookieAsync(
            AuthResponse response,
            bool rememberMe,
            CancellationToken cancellationToken)
        {
            var grant = await _authSessionService.StartSessionAsync(
                response.UserId,
                rememberMe,
                Request.Cookies[RefreshCookieName],
                cancellationToken);
            WriteRefreshCookie(grant);
        }

        private void WriteRefreshCookie(RefreshSessionGrant grant)
        {
            var cookie = new CookieOptions
            {
                HttpOnly = true,
                Secure = true,
                SameSite = RefreshCookieSameSite(),
                Path = "/api/auth",
                IsEssential = true
            };

            if (grant.Persistent)
            {
                cookie.Expires = grant.ExpiresAtUtc;
            }

            Response.Cookies.Append(RefreshCookieName, grant.RefreshToken, cookie);
        }

        private void ClearRefreshCookie()
        {
            Response.Cookies.Delete(RefreshCookieName, new CookieOptions
            {
                HttpOnly = true,
                Secure = true,
                SameSite = RefreshCookieSameSite(),
                Path = "/api/auth",
                IsEssential = true
            });
        }

        private bool IsTrustedBrowserOrigin()
        {
            var origin = Request.Headers.Origin.ToString();
            if (string.IsNullOrWhiteSpace(origin))
            {
                return false;
            }

            var allowedOrigins = _configuration.GetSection("Cors:AllowedOrigins").Get<string[]>()
                ?? Array.Empty<string>();
            if (_environment.IsDevelopment())
            {
                allowedOrigins = allowedOrigins.Concat(new[]
                {
                    "http://localhost:4200",
                    "http://127.0.0.1:4200"
                }).ToArray();
            }

            return allowedOrigins.Contains(origin, StringComparer.OrdinalIgnoreCase);
        }

        private SameSiteMode RefreshCookieSameSite()
        {
            var origin = Request.Headers.Origin.ToString();
            var localDevelopmentOrigin = origin.Equals("http://localhost:4200", StringComparison.OrdinalIgnoreCase)
                || origin.Equals("http://127.0.0.1:4200", StringComparison.OrdinalIgnoreCase);
            return _environment.IsDevelopment() && localDevelopmentOrigin
                ? SameSiteMode.None
                : SameSiteMode.Lax;
        }
    }
}

