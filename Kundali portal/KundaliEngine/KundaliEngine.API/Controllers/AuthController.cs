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

        public AuthController(IAuthService authService, IAuthOtpService authOtpService, IAuditLogService auditLogService)
        {
            _authService = authService;
            _authOtpService = authOtpService;
            _auditLogService = auditLogService;
        }

        [HttpPost("register")]
        public async Task<IActionResult> Register(RegisterRequest request)
        {
            try
            {
                var response = await _authService.RegisterAsync(request);
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


        [EnableRateLimiting("AuthOtp")]
        [HttpPost("otp/start-login")]
        public async Task<IActionResult> StartOtpLogin(StartEmailOtpRequest request, CancellationToken cancellationToken)
        {
            try
            {
                var response = await _authOtpService.StartEmailLoginAsync(request, cancellationToken);
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
                var response = await _authOtpService.VerifyEmailLoginAsync(request, cancellationToken);
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
                var response = await _authOtpService.StartEmailRegistrationAsync(request, cancellationToken);
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
                var response = await _authOtpService.VerifyEmailRegistrationAsync(request, cancellationToken);
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
    }
}

