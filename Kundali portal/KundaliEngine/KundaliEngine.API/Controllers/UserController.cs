using System.Security.Claims;
using KundaliEngine.API.DTOs.User;
using KundaliEngine.API.Services.Audit;
using KundaliEngine.API.Services.Auth;
using KundaliEngine.API.Services.Entitlements;
using KundaliEngine.Infrastructure.Data;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace KundaliEngine.API.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    [Authorize]
    public class UserController : ControllerBase
    {
        private readonly KundaliDbContext _dbContext;
        private readonly IAuditLogService _auditLogService;
        private readonly IEntitlementService _entitlementService;

        public UserController(KundaliDbContext dbContext, IAuditLogService auditLogService, IEntitlementService entitlementService)
        {
            _dbContext = dbContext;
            _auditLogService = auditLogService;
            _entitlementService = entitlementService;
        }

        [Authorize(Roles = "Admin")]
        [HttpGet]
        public async Task<ActionResult<IEnumerable<UserLoginControlDto>>> GetUsers()
        {
            var users = await _dbContext.Users
                .AsNoTracking()
                .OrderBy(x => x.FullName)
                .ToListAsync();

            return Ok(users.Select(MapUserLoginControlDto));
        }

        [HttpGet("me")]
        public async Task<IActionResult> Me()
        {
            var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
            if (!Guid.TryParse(userId, out var parsedUserId))
            {
                return Unauthorized();
            }

            var user = await _dbContext.Users
                .AsNoTracking()
                .FirstOrDefaultAsync(x => x.Id == parsedUserId);

            if (user == null || !user.IsActive)
            {
                return Unauthorized();
            }

            return Ok(new
            {
                userId = user.Id,
                fullName = user.FullName,
                email = user.Email,
                phoneNumber = user.PhoneNumber,
                preferredLanguage = user.PreferredLanguage,
                role = user.Role,
                planCode = user.PlanCode,
                planExpiresAt = user.PlanExpiresAt,
                isPremium = _entitlementService.IsPremium(user)
            });
        }

        [HttpPatch("me/preferred-language")]
        public async Task<IActionResult> UpdatePreferredLanguage(UpdatePreferredLanguageRequest request)
        {
            var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
            if (!Guid.TryParse(userId, out var parsedUserId))
            {
                return Unauthorized();
            }

            string preferredLanguage;
            try
            {
                preferredLanguage = PreferredLanguageHelper.NormalizeRequired(request.PreferredLanguage);
            }
            catch (InvalidOperationException ex)
            {
                return BadRequest(new { message = ex.Message });
            }

            var user = await _dbContext.Users.FirstOrDefaultAsync(x => x.Id == parsedUserId);
            if (user == null || !user.IsActive)
            {
                return Unauthorized();
            }

            user.PreferredLanguage = preferredLanguage;
            user.UpdatedAt = DateTime.UtcNow;
            await _dbContext.SaveChangesAsync();

            await _auditLogService.LogAsync(new AuditLogEntry
            {
                Action = "UserPreferredLanguageChanged",
                Category = "UserPreference",
                UserId = user.Id,
                UserName = user.FullName,
                EntityType = "User",
                EntityId = user.Id.ToString(),
                Description = $"User preferred language changed to {preferredLanguage}.",
                Status = "Success"
            });

            return Ok(new
            {
                preferredLanguage = user.PreferredLanguage
            });
        }

        [Authorize(Roles = "Admin")]
        [HttpPatch("{id:guid}/kundali-save-limit")]
        public async Task<IActionResult> UpdateKundaliSaveLimit(Guid id, UpdateUserKundaliSaveLimitRequest request)
        {
            if (request.KundaliSaveLimit < 0 || request.KundaliSaveLimit > 1000)
            {
                return BadRequest(new
                {
                    message = "Kundali save limit must be between 0 and 1000."
                });
            }

            var user = await _dbContext.Users.FirstOrDefaultAsync(x => x.Id == id);

            if (user == null)
            {
                return NotFound(new
                {
                    message = "User was not found."
                });
            }

            var oldLimit = user.KundaliSaveLimit;
            user.KundaliSaveLimit = request.KundaliSaveLimit;
            user.UpdatedAt = DateTime.UtcNow;

            await _dbContext.SaveChangesAsync();
            await _auditLogService.LogAsync(new AuditLogEntry
            {
                Action = "UserKundaliSaveLimitChanged",
                Category = "AdminAction",
                EntityType = "User",
                EntityId = user.Id.ToString(),
                Description = $"User Kundali save limit changed from {oldLimit} to {user.KundaliSaveLimit} for {user.Email}.",
                Status = "Success"
            });

            return Ok(MapUserLoginControlDto(user));
        }

        [Authorize(Roles = "Admin")]
        [HttpPatch("{id:guid}/premium-plan")]
        public async Task<IActionResult> UpdatePremiumPlan(Guid id, UpdateUserPremiumPlanRequest request)
        {
            var user = await _dbContext.Users.FirstOrDefaultAsync(x => x.Id == id);

            if (user == null)
            {
                return NotFound(new
                {
                    message = "User was not found."
                });
            }

            var oldPlanCode = user.PlanCode;
            var oldPlanExpiresAt = user.PlanExpiresAt;
            user.PlanCode = request.IsPremium ? "Premium" : "Free";
            user.PlanExpiresAt = request.IsPremium ? request.PlanExpiresAt : null;
            user.UpdatedAt = DateTime.UtcNow;

            await _dbContext.SaveChangesAsync();
            await _auditLogService.LogAsync(new AuditLogEntry
            {
                Action = "UserPremiumPlanChanged",
                Category = "AdminAction",
                EntityType = "User",
                EntityId = user.Id.ToString(),
                Description = $"User plan changed from {oldPlanCode} ({oldPlanExpiresAt?.ToString("O") ?? "no expiry"}) to {user.PlanCode} ({user.PlanExpiresAt?.ToString("O") ?? "no expiry"}) for {user.Email}.",
                Status = "Success"
            });

            return Ok(MapUserLoginControlDto(user));
        }
        [Authorize(Roles = "Admin")]
        [HttpPatch("{id:guid}/active")]
        public async Task<IActionResult> UpdateActiveStatus(Guid id, UpdateUserActiveStatusRequest request)
        {
            var user = await _dbContext.Users.FirstOrDefaultAsync(x => x.Id == id);

            if (user == null)
            {
                return NotFound(new
                {
                    message = "User was not found."
                });
            }

            var oldStatus = user.IsActive;
            user.IsActive = request.IsActive;
            user.UpdatedAt = DateTime.UtcNow;

            await _dbContext.SaveChangesAsync();
            await _auditLogService.LogAsync(new AuditLogEntry
            {
                Action = "UserActiveStatusChanged",
                Category = "AdminAction",
                EntityType = "User",
                EntityId = user.Id.ToString(),
                Description = $"User active status changed from {oldStatus} to {user.IsActive} for {user.Email}.",
                Status = "Success"
            });

            return Ok(MapUserLoginControlDto(user));
        }

        private UserLoginControlDto MapUserLoginControlDto(KundaliEngine.Domain.Entities.User user)
        {
            return new UserLoginControlDto
            {
                Id = user.Id,
                FullName = user.FullName,
                Email = user.Email,
                PhoneNumber = user.PhoneNumber,
                IsActive = user.IsActive,
                KundaliSaveLimit = user.KundaliSaveLimit,
                PlanCode = user.PlanCode,
                PlanExpiresAt = user.PlanExpiresAt,
                IsPremium = _entitlementService.IsPremium(user)
            };
        }
    }
}
