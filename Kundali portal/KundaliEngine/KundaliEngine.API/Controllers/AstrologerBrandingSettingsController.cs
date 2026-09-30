using System.Security.Claims;
using KundaliEngine.API.DTOs.Requests;
using KundaliEngine.API.DTOs.Responses;
using KundaliEngine.API.Services.Entitlements;
using KundaliEngine.Domain.Entities;
using KundaliEngine.Infrastructure.Data;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace KundaliEngine.API.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    [Authorize]
    public class AstrologerBrandingSettingsController : ControllerBase
    {
        private readonly KundaliDbContext _dbContext;
        private readonly IEntitlementService _entitlementService;

        public AstrologerBrandingSettingsController(KundaliDbContext dbContext, IEntitlementService entitlementService)
        {
            _dbContext = dbContext;
            _entitlementService = entitlementService;
        }

        [HttpGet("me")]
        public async Task<ActionResult<AstrologerBrandingSettingDto?>> GetMine()
        {
            var userId = GetCurrentUserId();
            if (userId == null)
            {
                return Unauthorized();
            }

            var setting = await _dbContext.AstrologerBrandingSettings
                .AsNoTracking()
                .FirstOrDefaultAsync(item => item.UserId == userId.Value);

            return Ok(setting == null ? null : MapToDto(setting));
        }

        [HttpPut("me")]
        public async Task<ActionResult<AstrologerBrandingSettingDto>> UpsertMine(AstrologerBrandingSettingRequest request)
        {
            var userId = GetCurrentUserId();
            if (userId == null)
            {
                return Unauthorized();
            }

            var user = await _dbContext.Users.AsNoTracking().FirstOrDefaultAsync(item => item.Id == userId.Value);
            if (user == null || !user.IsActive)
            {
                return Unauthorized();
            }

            if (!_entitlementService.IsPlus(user))
            {
                return StatusCode(StatusCodes.Status402PaymentRequired, new
                {
                    message = "White-label branding is available on Premium Plus."
                });
            }

            if (string.IsNullOrWhiteSpace(request.AstrologerName))
            {
                return BadRequest(new { message = "Astrologer name is required." });
            }

            var setting = await _dbContext.AstrologerBrandingSettings
                .FirstOrDefaultAsync(item => item.UserId == userId.Value);

            if (setting == null)
            {
                setting = new AstrologerBrandingSetting
                {
                    Id = Guid.NewGuid(),
                    UserId = userId.Value,
                    CreatedAt = DateTime.UtcNow
                };

                _dbContext.AstrologerBrandingSettings.Add(setting);
            }

            setting.AstrologerName = request.AstrologerName.Trim();
            setting.Designation = NormalizeOptionalText(request.Designation);
            setting.Address = NormalizeOptionalText(request.Address);
            setting.ContactNumber = NormalizeOptionalText(request.ContactNumber);
            setting.Email = NormalizeOptionalText(request.Email);
            setting.LogoUrl = NormalizeOptionalText(request.LogoUrl);
            setting.ShowOnChartPage = request.ShowOnChartPage;
            setting.ShowOnCompatibilityPage = request.ShowOnCompatibilityPage;
            setting.ShowOnPdfReport = request.ShowOnPdfReport;
            setting.IsActive = request.IsActive;
            setting.UpdatedAt = DateTime.UtcNow;

            await _dbContext.SaveChangesAsync();

            return Ok(MapToDto(setting));
        }

        private Guid? GetCurrentUserId()
        {
            var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
            return Guid.TryParse(userId, out var parsedUserId) ? parsedUserId : null;
        }

        private static string? NormalizeOptionalText(string? value)
        {
            return string.IsNullOrWhiteSpace(value) ? null : value.Trim();
        }

        private static AstrologerBrandingSettingDto MapToDto(AstrologerBrandingSetting setting)
        {
            return new AstrologerBrandingSettingDto
            {
                Id = setting.Id,
                UserId = setting.UserId,
                AstrologerName = setting.AstrologerName,
                Designation = setting.Designation,
                Address = setting.Address,
                ContactNumber = setting.ContactNumber,
                Email = setting.Email,
                LogoUrl = setting.LogoUrl,
                ShowOnChartPage = setting.ShowOnChartPage,
                ShowOnCompatibilityPage = setting.ShowOnCompatibilityPage,
                ShowOnPdfReport = setting.ShowOnPdfReport,
                IsActive = setting.IsActive,
                CreatedAt = setting.CreatedAt,
                UpdatedAt = setting.UpdatedAt
            };
        }
    }
}
