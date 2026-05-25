using KundaliEngine.API.DTOs.Requests;
using KundaliEngine.API.DTOs.Responses;
using KundaliEngine.Astrology.Astronomy;
using KundaliEngine.Astrology.Services.Astrology;
using KundaliEngine.Astrology.Services.Dasha;
using KundaliEngine.Domain.Models;
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

        public DashaController(DashaService dashaService)
        {
            _dashaService = dashaService;
        }

        [HttpPost("mahadasha-periods")]
        public ActionResult<List<MahadashaPeriodDto>> GetMahadashaPeriodsTimeline([FromBody] GenerateKundaliRequest request)
        {
            if (request == null)
                return BadRequest("Request is required");

            try
            {
                var planets = new ChartService().GenerateChart(request.BirthDateTime, request.Latitude, request.Longitude);
                var moonPos = planets.First(p => p.Planet == Domain.Enums.Planet.Moon);
                var birthDasha = _dashaService.GetBirthMahadasha(moonPos);

                // Dasha timeline for 120 years (full Vimshottari cycle)
                var mahadashaTimeline = _dashaService.GenerateMahadashaTimeline(birthDasha.Planet, birthDasha.RemainingYears, request.BirthDateTime);
                
                // Ensure timeline covers at least 120 years by extending cycles if needed
                while ((mahadashaTimeline.Last().EndDate - mahadashaTimeline.First().StartDate).TotalDays < 120 * 365.25)
                {
                    var lastPlanet = mahadashaTimeline.Last().Planet;
                    var extend = _dashaService.GenerateMahadashaTimeline(lastPlanet, _dashaService.GetBirthMahadasha(moonPos).RemainingYears, mahadashaTimeline.Last().EndDate);
                    mahadashaTimeline.AddRange(extend);
                }

                // Build mahadasha + antardasha DTOs
                var mahadashaDtos = mahadashaTimeline.Select(m => new MahadashaPeriodDto
                {
                    Planet = m.Planet.ToString(),
                    Start = m.StartDate,
                    End = m.EndDate,
                    Antardashas = _dashaService.GenerateAntardasha(m, _dashaService.GetBirthMahadasha(moonPos).RemainingYears)
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
