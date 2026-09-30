using KundaliEngine.API.DTOs.Muhurtha.GrahaPravesha;
using KundaliEngine.API.Services.Muhurtha.GrahaPravesha;
using Microsoft.AspNetCore.Mvc;

namespace KundaliEngine.API.Controllers;

[Route("api/muhurtha/graha-pravesha")]
[ApiController]
public class GrahaPraveshaMuhurthaController : ControllerBase
{
    private readonly IGrahaPraveshaMuhurthaService _service;

    public GrahaPraveshaMuhurthaController(IGrahaPraveshaMuhurthaService service)
    {
        _service = service;
    }

    [HttpPost("search")]
    public ActionResult<GrahaPraveshaSearchResponseDto> Search([FromBody] GrahaPraveshaSearchRequestDto request)
    {
        if (request is null)
        {
            return BadRequest("Request is required.");
        }

        try
        {
            return Ok(_service.Search(request));
        }
        catch (ArgumentException ex)
        {
            return BadRequest(ex.Message);
        }
    }

    [HttpGet("{date}")]
    public ActionResult<GrahaPraveshaDayResponseDto> AnalyzeDate(
        DateOnly date,
        [FromQuery] double latitude,
        [FromQuery] double longitude,
        [FromQuery] string timezone,
        [FromQuery] string? city,
        [FromQuery] string calendarSystem = "AMANTA")
    {
        try
        {
            return Ok(_service.AnalyzeDate(
                date,
                new GrahaPraveshaLocationDto
                {
                    City = city ?? string.Empty,
                    Latitude = latitude,
                    Longitude = longitude,
                    Timezone = timezone
                },
                calendarSystem,
                null));
        }
        catch (ArgumentException ex)
        {
            return BadRequest(ex.Message);
        }
    }
}
