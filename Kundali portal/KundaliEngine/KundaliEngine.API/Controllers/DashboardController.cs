using System.Security.Claims;
using KundaliEngine.API.Services.Dashboard;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace KundaliEngine.API.Controllers;

[ApiController]
[Authorize]
[Route("api/[controller]")]
public sealed class DashboardController : ControllerBase
{
    private readonly IDashboardSummaryService _dashboardSummaryService;

    public DashboardController(IDashboardSummaryService dashboardSummaryService)
    {
        _dashboardSummaryService = dashboardSummaryService;
    }

    [HttpGet("summary")]
    public async Task<IActionResult> GetSummary(CancellationToken cancellationToken)
    {
        var userId = GetCurrentUserId();
        if (userId is null)
        {
            return Unauthorized();
        }

        var summary = await _dashboardSummaryService.GetSummaryAsync(userId.Value, cancellationToken);
        return summary is null ? NotFound() : Ok(summary);
    }

    private Guid? GetCurrentUserId()
    {
        var claimValue = User.FindFirstValue(ClaimTypes.NameIdentifier);
        return Guid.TryParse(claimValue, out var userId) ? userId : null;
    }
}
