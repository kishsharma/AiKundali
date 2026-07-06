using KundaliEngine.API.DTOs.Requests;
using KundaliEngine.API.DTOs.Responses;
using KundaliEngine.Astrology.Services.Astrology;
using KundaliEngine.Astrology.Services.Dasha;
using Microsoft.AspNetCore.Mvc;
using System;
using System.Collections.Generic;
using System.Linq;

namespace KundaliEngine.API.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class DashaController : ControllerBase
    {
        private readonly DashaService _dashaService;
        private readonly ChartService _chartService;

        public DashaController(DashaService dashaService, ChartService chartService)
        {
            _dashaService = dashaService;
            _chartService = chartService;
        }

        [HttpPost("mahadasha-periods")]
        public ActionResult<List<MahadashaPeriodDto>> GetMahadashaPeriodsTimeline([FromBody] GenerateKundaliRequest request)
        {
            if (request == null)
                return BadRequest("Request is required");

            try
            {
                var indiaZone = TimeZoneInfo.FindSystemTimeZoneById("India Standard Time");
                DateTime birthUtc = TimeZoneInfo.ConvertTimeToUtc(request.BirthDateTime, indiaZone);

                var planets = _chartService.GenerateChart(birthUtc, request.Latitude, request.Longitude);
                var moonPos = planets.First(p => p.Planet == Domain.Enums.Planet.Moon);
                var birthDasha = _dashaService.GetBirthMahadasha(moonPos);

                var mahadashaTimeline = _dashaService.GenerateMahadashaTimelineForYears(
                    birthDasha.Planet,
                    birthDasha.RemainingYears,
                    birthUtc,
                    120);

                // Build mahadasha + antardasha DTOs
                var mahadashaDtos = mahadashaTimeline.Select(m => new MahadashaPeriodDto
                {
                    Planet = m.Planet.ToString(),
                    Start = m.StartDate,
                    End = m.EndDate,
                    Antardashas = _dashaService.GenerateAntardashaForTimelinePeriod(
                            m,
                            birthDasha.Planet,
                            birthDasha.RemainingYears)
                        .Select(a => new AntardashaPeriodDto { SubPlanet = a.SubPlanet.ToString(), StartDate = a.StartDate, EndDate = a.EndDate })
                        .ToList()
                }).ToList();

                return Ok(mahadashaDtos);
            }
            catch (Exception ex)
            {
                return BadRequest($"Error generating mahadasha periods: {ex.Message}");
            }
        }
    }
}
