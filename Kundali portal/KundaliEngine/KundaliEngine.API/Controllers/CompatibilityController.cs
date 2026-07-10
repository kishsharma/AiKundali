using KundaliEngine.API.DTOs.Requests;
using KundaliEngine.API.DTOs.Responses;
using KundaliEngine.Astrology.Astronomy;
using KundaliEngine.Astrology.Services;
using KundaliEngine.Astrology.Services.Astrology;
using KundaliEngine.Astrology.Services.Compatibility;
using KundaliEngine.Astrology.Services.House;
using KundaliEngine.Astrology.Services.KundaliDosha;
using KundaliEngine.Astrology.Services.Panchang;
using KundaliEngine.Astrology.Services.Scoring;
using KundaliEngine.Domain.Enums;
using KundaliEngine.Domain.Models;
using KundaliEngine.API.Helper_functions;
using KundaliEngine.KnowledgeBase.Services;
using Microsoft.AspNetCore.Mvc;
using System.Numerics;

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
        private readonly CompatibilityDoshaAnalysisService _compatibilityDoshaAnalysisService;
        private readonly DoshaDetectionService _doshaDetectionService;
        private readonly DoshaMetadataService _doshaMetadataService;
        private readonly HouseScoreService _houseScoreservice;
        private readonly NakshatraMetadataService _nakshatraMetadataService;
        private readonly IEphemerisProvider _ephemerisProvider;
        public CompatibilityController(
            ChartService chartService,
            PanchangService panchangService,
            AspectService aspectService,
            CompatibilityScoreService compatibilityService,
            CompatibilityDoshaAnalysisService compatibilityDoshaAnalysisService,
            DoshaDetectionService doshaDetectionService,
            DoshaMetadataService doshaMetadataService,
            HouseScoreService houseScoreservice,
            NakshatraMetadataService nakshatraMetadataService,
            IEphemerisProvider ephemerisProvider)
        {
            _chartService = chartService;
            _panchangService = panchangService;
            _aspectService = aspectService;
            _compatibilityService = compatibilityService;
            _compatibilityDoshaAnalysisService = compatibilityDoshaAnalysisService;
            _doshaDetectionService = doshaDetectionService;
            _doshaMetadataService = doshaMetadataService;
            _houseScoreservice = houseScoreservice;
            _nakshatraMetadataService = nakshatraMetadataService;
            _ephemerisProvider = ephemerisProvider;
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
                var personADoshas = _doshaDetectionService.DetectDoshas(
                    profileA.Chart.PlanetPositions,
                    profileA.Chart.Houses);
                var personBDoshas = _doshaDetectionService.DetectDoshas(
                    profileB.Chart.PlanetPositions,
                    profileB.Chart.Houses);

                result.DoshaAnalysis = _compatibilityDoshaAnalysisService.Analyze(
                    profileA.Name,
                    profileB.Name,
                    personADoshas,
                    personBDoshas,
                    _doshaMetadataService);

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
            // Generate planet positions (Kundali chart)
            TimeZoneInfo indiaZone = TimeZoneInfo.FindSystemTimeZoneById("India Standard Time");

            // Assume the input is in IST (India Standard Time)
            var birthLocal = DateTime.SpecifyKind(req.BirthDateTime, DateTimeKind.Unspecified);
            req.BirthDateTime = birthLocal;
            DateTime birthUtc = TimeZoneInfo.ConvertTimeToUtc(req.BirthDateTime, indiaZone);

            req.BirthDateTime = birthUtc;
            // Chart (planet positions)
            var planetPositions = _chartService.GenerateChart(req.BirthDateTime, req.Latitude, req.Longitude);

            // Ascendant
            var location = new GeoLocation(req.Latitude, req.Longitude);
            double ascendantLongitude = _ephemerisProvider.GetAscendant(req.BirthDateTime, location).Longitude;
            int ascendantIndex = (int)(ascendantLongitude / 30.0);

            ZodiacSign ascendantSign = (ZodiacSign)((ascendantIndex % 12) + 1);


            // Houses and house scores
            var aspectResult = _aspectService.CalculateGrahaDrishti(planetPositions);
            var houseScores = _houseScoreservice.CalculateAllHouseScores(ascendantSign, planetPositions, aspectResult);
            // Compute house signs (house N sign = ascendantSign + (N-1))
            var houseDtos = new List<DTOs.Responses.HouseDto>();
            for (int i = 1; i <= 12; i++)
            {
                var houseNumber = (HouseNumber)i;
                var signIndex = (((int)ascendantSign - 1) + (i - 1)) % 12;
                var houseSign = (ZodiacSign)(signIndex + 1);
                var houseOwner =HelperData.GetSignOwner(houseSign);

                var houseAspects = aspectResult.HouseAspects
                    .Where(a => a.ToHouse == i)
                    .ToList();
                var occupants = planetPositions.Where(p => (int)p.House == i).Select(p => p.Planet.ToString()).ToList();
                var scoreResult = houseScores.First(h => (int)h.HouseNumber == i);


                houseDtos.Add(new DTOs.Responses.HouseDto
                {
                    HouseNumber = i,
                    Sign = houseSign.ToString(),
                    HouseOwner = houseOwner,
                    HouseSwamy = houseOwner,
                    Occupants = occupants,
                    HasBeneficAspect = houseAspects.Any(a =>HelperData.IsNaturalBenefic(a.FromPlanet)),
                    HasMaleficAspect = houseAspects.Any(a =>HelperData.IsNaturalMaleficOrShadow(a.FromPlanet)),
                    Score = scoreResult.Score,
                    Factors = scoreResult.Factors,
                    DominantThemes = scoreResult.DominantThemes,
                    HouseName = HouseMetadataService.GetHouseName(i),
                    LifeAreas = HouseMetadataService.GetLifeAreas(i),
                });
            }

            // Panchang
            double sunLongitude = planetPositions.First(p => p.Planet == Planet.Sun).Longitude;
            double moonLongitude = planetPositions.First(p => p.Planet == Planet.Moon).Longitude;
            var panchang = _panchangService.Calculate(req.BirthDateTime, sunLongitude, moonLongitude);
            panchang.Akshara = _nakshatraMetadataService.GetPadaAkshara(panchang.Nakshatra, panchang.Pada);

            // Aspects
            //var aspectResult = _aspectService.CalculateGrahaDrishti(planetPositions);

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
                AscendantSignAndNumber = $"{(int)ascendantSign} - {ascendantSign}",
                PlanetPositions = planetPositions.Select(x => new PlanetPosition
                {
                    Degree = x.Degree,
                    DegreeInSign = x.DegreeInSign,
                    DetailedStrength = x.DetailedStrength,
                    House = x.House,
                    IsCombust = x.IsCombust,
                    IsRetrograde = x.IsRetrograde,
                    Latitude = x.Latitude,
                    Longitude = x.Longitude,
                    Minute = x.Minute,
                    Nakshatra = x.Nakshatra,
                    Pada = x.Pada,
                    Planet = x.Planet,
                    PlanetName = x.Planet.ToString(),
                    Sign = x.Sign



                }
                ).ToList(),
                Aspects = aspectResult.PlanetAspects,
                Panchang = panchang,
                Houses = houseDtos.Select(h => new Domain.Models.HouseDto
                {
                    HouseNumber = (HouseNumber)h.HouseNumber,
                    Sign = Enum.Parse<ZodiacSign>(h.Sign),
                    HouseOwner = h.HouseOwner,
                    HouseSwamy = h.HouseSwamy,
                    Occupants = h.Occupants.Select(o => Enum.Parse<Planet>(o)).ToList(),
                    HasBeneficAspect = h.HasBeneficAspect,
                    HasMaleficAspect = h.HasMaleficAspect,
                    Score = h.Score,
                    Factors = h.Factors,
                    DominantThemes = h.DominantThemes,
                    HouseName = h.HouseName,
                    LifeAreas = h.LifeAreas
                }).ToList()
            };

            return new CompatibilityProfile(name, kundaliChart);
        }
    }
}
