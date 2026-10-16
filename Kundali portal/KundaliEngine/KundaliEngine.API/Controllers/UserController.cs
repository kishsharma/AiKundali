using System.Security.Claims;
using KundaliEngine.API.DTOs.User;
using KundaliEngine.API.Services.Practice;
using KundaliEngine.API.Services.Audit;
using KundaliEngine.API.Services.Auth;
using KundaliEngine.API.Services.Billing;
using KundaliEngine.API.Services.Entitlements;
using KundaliEngine.Domain.Subscriptions;
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
        private readonly IBillingService _billingService;

        public UserController(
            KundaliDbContext dbContext,
            IAuditLogService auditLogService,
            IEntitlementService entitlementService,
            IBillingService billingService)
        {
            _dbContext = dbContext;
            _auditLogService = auditLogService;
            _entitlementService = entitlementService;
            _billingService = billingService;
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

            var snapshot = _entitlementService.GetSnapshot(user);
            return Ok(new
            {
                userId = user.Id,
                fullName = user.FullName,
                email = user.Email,
                profileImageUrl = user.ProfileImageUrl,
                phoneNumber = user.PhoneNumber,
                preferredLanguage = user.PreferredLanguage,
                role = user.Role,
                planCode = snapshot.PlanCode,
                planExpiresAt = snapshot.PlanExpiresAt,
                isPremium = snapshot.IsPremium,
                isPaid = snapshot.IsPaid,
                isPlus = snapshot.IsPlus,
                kundaliSaveLimit = snapshot.KundaliSaveLimit,
                matchSaveLimit = snapshot.MatchSaveLimit
            });
        }

        [HttpGet("me/practice")]
        public async Task<IActionResult> GetPractice()
        {
            var user = await GetCurrentUserAsync();
            if (user is null)
            {
                return Unauthorized();
            }

            return Ok(ToPracticeResponse(user.PracticeProfile));
        }

        [HttpPut("me/practice")]
        public async Task<IActionResult> UpdatePractice(UpdatePracticeProfileRequest request)
        {
            var note = request.Note?.Trim();
            if (note is { Length: > PracticeProfileCatalog.NoteMaxLength })
            {
                return BadRequest(new { message = "Keep the other-services note under 400 characters." });
            }

            var user = await GetCurrentUserAsync();
            if (user is null)
            {
                return Unauthorized();
            }

            var services = PracticeProfileCatalog.Normalize(request.Services);
            user.PracticeProfile = PracticeProfileCatalog.Serialize(services, note);
            user.UpdatedAt = DateTime.UtcNow;
            await _dbContext.SaveChangesAsync();

            return Ok(ToPracticeResponse(user.PracticeProfile));
        }

        [HttpPatch("me/phone")]
        public async Task<IActionResult> UpdatePhone(UpdatePhoneNumberRequest request)
        {
            var digits = new string((request.PhoneNumber ?? string.Empty).Where(char.IsDigit).ToArray());
            if (digits.Length is < 10 or > 15)
            {
                return BadRequest(new { message = "Enter a mobile number with 10 to 15 digits." });
            }

            var user = await GetCurrentUserAsync();
            if (user is null)
            {
                return Unauthorized();
            }

            var taken = await _dbContext.Users.AnyAsync(item => item.Id != user.Id && item.PhoneNumber == digits);
            if (taken)
            {
                return Conflict(new { message = "This mobile number is already used by another account." });
            }

            if (!string.Equals(user.PhoneNumber, digits, StringComparison.Ordinal))
            {
                user.PhoneNumber = digits;
                user.PhoneVerifiedAt = null;
                user.UpdatedAt = DateTime.UtcNow;
                await _dbContext.SaveChangesAsync();
            }

            return Ok(new { phoneNumber = user.PhoneNumber });
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

            var requestedPlan = ResolveAdminPlanCode(request);
            if (requestedPlan == null)
            {
                return BadRequest(new
                {
                    message = "Plan must be Free, Premium, or Premium Plus."
                });
            }

            var oldPlanCode = user.PlanCode;
            var oldPlanExpiresAt = user.PlanExpiresAt;
            var expiresAt = requestedPlan == PlanCodes.Free ? null : request.PlanExpiresAt;

            await _billingService.OverrideUserPlanAsync(user, requestedPlan, expiresAt, HttpContext.RequestAborted);
            await _dbContext.Entry(user).ReloadAsync();
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
            var snapshot = _entitlementService.GetSnapshot(user);
            return new UserLoginControlDto
            {
                Id = user.Id,
                FullName = user.FullName,
                Email = user.Email,
                PhoneNumber = user.PhoneNumber,
                IsActive = user.IsActive,
                KundaliSaveLimit = snapshot.KundaliSaveLimit,
                PlanCode = snapshot.PlanCode,
                PlanExpiresAt = snapshot.PlanExpiresAt,
                IsPremium = snapshot.IsPremium,
                IsPaid = snapshot.IsPaid,
                IsPlus = snapshot.IsPlus,
                MatchSaveLimit = snapshot.MatchSaveLimit,
                LastSeenAt = user.LastSeenAt,
                IsOnline = user.LastSeenAt.HasValue && user.LastSeenAt.Value >= DateTime.UtcNow.AddMinutes(-10)
            };
        }

        private static string? ResolveAdminPlanCode(UpdateUserPremiumPlanRequest request)
        {
            if (!string.IsNullOrWhiteSpace(request.PlanCode))
            {
                return PlanCodes.TryParse(request.PlanCode, out var fromCode) ? fromCode : null;
            }

            if (request.IsPremium == true)
            {
                return PlanCodes.Premium;
            }

            if (request.IsPremium == false)
            {
                return PlanCodes.Free;
            }

            return null;
        }

        private async Task<KundaliEngine.Domain.Entities.User?> GetCurrentUserAsync()
        {
            var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
            if (!Guid.TryParse(userId, out var parsedUserId))
            {
                return null;
            }

            var user = await _dbContext.Users.FirstOrDefaultAsync(item => item.Id == parsedUserId);
            return user == null || !user.IsActive ? null : user;
        }

        private static object ToPracticeResponse(string? json)
        {
            var stored = PracticeProfileCatalog.Read(json);
            return new
            {
                groups = PracticeProfileCatalog.List().Select(group => new
                {
                    key = group.Key,
                    title = group.Title,
                    items = group.Items.Select(item => new { key = item.Key, label = item.Label })
                }),
                selected = stored.Services,
                note = stored.Note
            };
        }
    }
}
