using KundaliEngine.API.DTOs.Auth;
using KundaliEngine.API.Services.Audit;
using KundaliEngine.API.Services.Auth;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System.Security.Claims;

namespace KundaliEngine.API.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class AuthController : ControllerBase
    {
        private readonly IAuthService _authService;
        private readonly IAuditLogService _auditLogService;

        public AuthController(IAuthService authService, IAuditLogService auditLogService)
        {
            _authService = authService;
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
