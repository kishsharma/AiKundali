using KundaliEngine.API.DTOs.Requests;
using KundaliEngine.API.DTOs.Responses;
using KundaliEngine.API.Helper_functions;
using KundaliEngine.Astrology.Astronomy;
using KundaliEngine.Astrology.Services.Astrology;
using KundaliEngine.Astrology.Services.DivisionalCharts;
using KundaliEngine.Astrology.Services.House;
using KundaliEngine.Domain.Enums;
using KundaliEngine.Domain.Models;
using Microsoft.AspNetCore.Mvc;

namespace KundaliEngine.API.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class DivisionalChartController : ControllerBase
    {
        private readonly ChartService _chartService;
        private readonly IEphemerisProvider _ephemerisProvider;
        private readonly DivisionalChartService _divisionalChartService;

        public DivisionalChartController(
            ChartService chartService,
            IEphemerisProvider ephemerisProvider,
            DivisionalChartService divisionalChartService)
        {
            _chartService = chartService;
            _ephemerisProvider = ephemerisProvider;
            _divisionalChartService = divisionalChartService;
        }

        [HttpPost("generate")]
        public ActionResult<List<DivisionalChartDto>> Generate([FromBody] GenerateDivisionalChartsRequest request)
        {
            if (request == null)
            {
                return BadRequest("Request is required.");
            }

            if (request.ChartTypes == null || request.ChartTypes.Count == 0)
            {
                return BadRequest("At least one chart type is required.");
            }

            var requestedChartTypes = request.ChartTypes.Distinct().ToList();
            var unsupportedChartTypes = requestedChartTypes
                .Where(chartType => !_divisionalChartService.SupportedChartTypes.Contains(chartType))
                .ToList();

            if (unsupportedChartTypes.Count > 0)
            {
                return BadRequest(new
                {
                    message = "One or more chart types are not supported yet.",
                    unsupportedChartTypes,
                    supportedChartTypes = _divisionalChartService.SupportedChartTypes
                });
            }

            var planets = _chartService.GenerateChart(request.BirthDateTime, request.Latitude, request.Longitude);
            var location = new GeoLocation(request.Latitude, request.Longitude);
            var ascendantLongitude = _ephemerisProvider.GetAscendant(request.BirthDateTime, location).Longitude;
            var ascendantSign = (ZodiacSign)((int)(NormalizeLongitude(ascendantLongitude) / 30.0) + 1);
            var ascendantDegreeInSign = NormalizeDegreeInSign(ascendantLongitude);

            var baseChart = new KundaliChart
            {
                BirthDetails = new BirthDetails
                {
                    BirthDateTimeLocal = request.BirthDateTime,
                    PlaceName = request.BirthPlace,
                    Latitude = request.Latitude,
                    Longitude = request.Longitude
                },
                GeneratedAtUtc = DateTime.UtcNow,
                AscendantSign = ascendantSign,
                AscendantSignAndNumber = $"{(int)ascendantSign} - {ascendantSign}",
                AscendantDegree = (int)Math.Floor(ascendantDegreeInSign),
                AscendantMinute = (int)Math.Floor((ascendantDegreeInSign - Math.Floor(ascendantDegreeInSign)) * 60.0),
                PlanetPositions = planets
            };

            var response = requestedChartTypes
                .Select(chartType => _divisionalChartService.GenerateDivisionalChart(
                    baseChart,
                    chartType,
                    ascendantDegreeInSign))
                .Select(MapDivisionalChartDto)
                .ToList();

            return Ok(response);
        }

        private static DivisionalChartDto MapDivisionalChartDto(KundaliEngine.Domain.Models.DivisionalCharts.DivisionalChart chart)
        {
            return new DivisionalChartDto
            {
                ChartType = chart.ChartType.ToString(),
                Ascendant = $"{(int)chart.AscendantSign} - {chart.AscendantSign}",
                Planets = chart.PlanetPositions.Select(MapPlanetDto).ToList(),
                Houses = BuildDivisionalHouseDtos(chart.AscendantSign, chart.PlanetPositions),
                Description = chart.Description
            };
        }

        private static PlanetDto MapPlanetDto(PlanetPosition planet)
        {
            return new PlanetDto
            {
                Planet = planet.Planet.ToString(),
                Sign = planet.Sign.ToString(),
                Degree = planet.Degree,
                Minute = planet.Minute,
                House = (int)planet.House,
                Longitude = planet.Longitude,
                Latitude = planet.Latitude,
                IsCombust = planet.IsCombust,
                IsRetrograde = planet.IsRetrograde,
                DetailedStrength = planet.DetailedStrength,
                AspectedBy = new List<string>(),
                HasBeneficAspect = false,
                HasMaleficAspect = false,
                ConditionTone = "Neutral",
                Nakshatra = planet.Nakshatra.ToString(),
                Pada = planet.Pada
            };
        }

        private static List<DTOs.Responses.HouseDto> BuildDivisionalHouseDtos(
            ZodiacSign ascendantSign,
            List<PlanetPosition> planets)
        {
            var houses = new List<DTOs.Responses.HouseDto>();

            for (var i = 1; i <= 12; i++)
            {
                var signIndex = (((int)ascendantSign - 1) + (i - 1)) % 12;
                var houseSign = (ZodiacSign)(signIndex + 1);
                var houseOwner = HelperData.GetSignOwner(houseSign);
                var occupants = planets
                    .Where(planet => (int)planet.House == i)
                    .Select(planet => planet.Planet.ToString())
                    .ToList();

                houses.Add(new DTOs.Responses.HouseDto
                {
                    HouseNumber = i,
                    Sign = houseSign.ToString(),
                    HouseOwner = houseOwner,
                    HouseSwamy = houseOwner,
                    Occupants = occupants,
                    HasBeneficAspect = false,
                    HasMaleficAspect = false,
                    AspectedBy = new List<string>(),
                    Score = 0,
                    Factors = new List<string>(),
                    DominantThemes = new List<string>(),
                    HouseName = HouseMetadataService.GetHouseName(i),
                    LifeAreas = HouseMetadataService.GetLifeAreas(i)
                });
            }

            return houses;
        }

        private static double NormalizeLongitude(double longitude)
        {
            longitude %= 360.0;
            if (longitude < 0)
            {
                longitude += 360.0;
            }

            return longitude;
        }

        private static double NormalizeDegreeInSign(double longitude)
        {
            return NormalizeLongitude(longitude) % 30.0;
        }
    }
}
