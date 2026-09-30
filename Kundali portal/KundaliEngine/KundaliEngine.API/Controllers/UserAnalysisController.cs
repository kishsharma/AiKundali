using KundaliEngine.API.Services.Admin;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace KundaliEngine.API.Controllers;

[ApiController]
[Authorize(Roles = "Admin")]
[Route("api/admin/user-analysis")]
public class UserAnalysisController : ControllerBase
{
    private readonly IUserAnalysisService _analysis;

    public UserAnalysisController(IUserAnalysisService analysis)
    {
        _analysis = analysis;
    }

    [HttpGet]
    public async Task<ActionResult<UserAnalysisResponse>> Get(CancellationToken cancellationToken)
    {
        return Ok(await _analysis.GetAsync(cancellationToken));
    }
}
