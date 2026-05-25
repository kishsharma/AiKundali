using KundaliEngine.API.DTOs.Requests;
using KundaliEngine.Domain.Models;
using KundaliEngine.Astrology.Astronomy;
using KundaliEngine.Astrology.Services.Astrology;
using KundaliEngine.Astrology.Services.Panchang;
using KundaliEngine.Astrology.Services.Compatibility;
using KundaliEngine.Astrology.Services;
using KundaliEngine.Domain.Enums;
using Microsoft.AspNetCore.Mvc;

namespace KundaliEngine.API.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class CompatibilityController : ControllerBase
    {
        private readonly ChartService _chartService;
        private readonly PanchangService _panchangService;
        private readonly AspectService _aspectService;
        private readonly CompatibilityScoreService _compatibilityService;

        public CompatibilityController(
            ChartService chartService,
            PanchangService panchangService,
            AspectService aspectService,
            CompatibilityScoreService compatibilityService)
        {
            _chartService = chartService;
            _panchangService = panchangService;
            _aspectService = aspectService;
            _compatibilityService = compatibilityService;
        }

        [HttpPost("match")]
        public ActionResult<CompatibilityResult> Match([FromBody] CompatibilityRequestDto request)
        {
            if (request == null)
                return BadRequest("Request is required");

            if (request.PersonA == null || request.PersonB == null)
                return BadRequest("Both PersonA and PersonB are required");

            try
            {
                var profileA = BuildProfile(request.PersonA, request.PersonA.Name ?? "Person A");
                var profileB = BuildProfile(request.PersonB, request.PersonB.Name ?? "Person B");

                var result = _compatibilityService.CalculateCompatibility(profileA, profileB);

                return Ok(result);
            }
            catch (ArgumentException aex)
            {
                return BadRequest(aex.Message);
            }
            catch (Exception ex)
            {
                return StatusCode(500, $"Compatibility calculation failed: {ex.Message}");
            }
        }

        private CompatibilityProfile BuildProfile(GenerateKundaliRequest req, string name)
        {
            if (req == null)
                throw new ArgumentNullException(nameof(req));

            // Chart (planet positions)
            var planetPositions = _chartService.GenerateChart(req.BirthDateTime, req.Latitude, req.Longitude);

            // Ascendant
            var ascCalc = new AscendantCalculator();
            double ascendantLongitude = ascCalc.Calculate(req.BirthDateTime, req.Latitude, req.Longitude);
            int ascendantIndex = (int)(ascendantLongitude / 30.0);
            ZodiacSign ascendantSign = (ZodiacSign)(ascendantIndex + 1);

            // Panchang
            double sunLongitude = planetPositions.First(p => p.Planet == Planet.Sun).Longitude;
            double moonLongitude = planetPositions.First(p => p.Planet == Planet.Moon).Longitude;
            var panchang = _panchangService.Calculate(req.BirthDateTime, sunLongitude, moonLongitude);

            // Aspects
            var aspectResult = _aspectService.CalculateGrahaDrishti(planetPositions);

            // Build KundaliChart
            var kundaliChart = new KundaliChart
            {
                GeneratedAtUtc = DateTime.UtcNow,
                BirthDetails = new BirthDetails
                {
                    BirthDateTimeLocal = req.BirthDateTime,
                    Latitude = req.Latitude,
                    Longitude = req.Longitude,
                    TimeZoneOffsetHours = req.TimeZoneOffset
                },
                AscendantDegree = (int)(ascendantLongitude % 30),
                AscendantMinute = (int)((ascendantLongitude % 1) * 60),
                AscendantSign = ascendantSign,
                PlanetPositions = planetPositions,
                Aspects = aspectResult.PlanetAspects,
                Panchang = panchang
            };

            return new CompatibilityProfile(name, kundaliChart);
        }
    }
}
