using System.Security.Claims;
using KundaliEngine.API.DTOs.User;
using KundaliEngine.API.Services.Audit;
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

        public UserController(KundaliDbContext dbContext, IAuditLogService auditLogService)
        {
            _dbContext = dbContext;
            _auditLogService = auditLogService;
        }

        [HttpGet]
        public async Task<ActionResult<IEnumerable<UserLoginControlDto>>> GetUsers()
        {
            var users = await _dbContext.Users
                .AsNoTracking()
                .OrderBy(x => x.FullName)
                .Select(x => new UserLoginControlDto
                {
                    Id = x.Id,
                    FullName = x.FullName,
                    Email = x.Email,
                    PhoneNumber = x.PhoneNumber,
                    IsActive = x.IsActive,
                    KundaliSaveLimit = x.KundaliSaveLimit
                })
                .ToListAsync();

            return Ok(users);
        }

        [HttpGet("me")]
        public IActionResult Me()
        {
            var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
            var fullName = User.FindFirstValue(ClaimTypes.Name);
            var email = User.FindFirstValue(ClaimTypes.Email);
            var phoneNumber = User.FindFirstValue("phone_number");
            var role = User.FindFirstValue(ClaimTypes.Role);

            return Ok(new
            {
                userId,
                fullName,
                email,
                phoneNumber,
                role
            });
        }

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

            return Ok(new UserLoginControlDto
            {
                Id = user.Id,
                FullName = user.FullName,
                Email = user.Email,
                PhoneNumber = user.PhoneNumber,
                IsActive = user.IsActive,
                KundaliSaveLimit = user.KundaliSaveLimit
            });
        }
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

            return Ok(new UserLoginControlDto
            {
                Id = user.Id,
                FullName = user.FullName,
                Email = user.Email,
                PhoneNumber = user.PhoneNumber,
                IsActive = user.IsActive,
                KundaliSaveLimit = user.KundaliSaveLimit
            });
        }
    }
}
