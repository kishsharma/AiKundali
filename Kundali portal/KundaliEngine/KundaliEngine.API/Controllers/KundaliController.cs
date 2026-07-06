using KundaliEngine.API.DTOs.Requests;
using KundaliEngine.API.DTOs.Responses;
using KundaliEngine.Astrology.Astronomy;
using KundaliEngine.Astrology.Services.Astrology;
using KundaliEngine.Astrology.Services.Dasha;
using KundaliEngine.Astrology.Services.House;
using KundaliEngine.Astrology.Services.KundaliYoga;
using KundaliEngine.Astrology.Services.Panchang;
using KundaliEngine.Astrology.Services.Scoring;
using KundaliEngine.Domain.Enums;
using KundaliEngine.Domain.Models;
using KundaliEngine.Domain.Rules;
using KundaliEngine.KnowledgeBase.Services;
using Microsoft.AspNetCore.Mvc;
using System;
using System.Collections.Generic;
using System.Linq;
using KundaliEngine.API.Helper_functions;

namespace KundaliEngine.API.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class KundaliController : ControllerBase
    {
        private readonly ChartService _chartService;
        private readonly PanchangService _panchangService;
        private readonly PlanetScoreService _planetScoreService;
        private readonly HouseScoreService _houseScoreservice;
        private readonly DashaService _dashaService;
        private readonly OverallKundaliScoreService _overallScoreService;
        private readonly AspectService _aspectService;
        private readonly YogaDetectionService _yogaDetectionService;
        private readonly YogaMetadataService _yogaMetadataService;
        public KundaliController(ChartService chartService, PanchangService panchangService, PlanetScoreService planetScoreService, 
            HouseScoreService houseScoreService, DashaService dashaService,
            OverallKundaliScoreService overallScoreService,
            AspectService aspectService,
            YogaDetectionService yogaDetectionService,
            YogaMetadataService yogaMetadataService
            
            )
        {
            _chartService = chartService;
            _panchangService = panchangService;
            _planetScoreService = planetScoreService;
            _houseScoreservice = houseScoreService;
            _dashaService = dashaService;
            _overallScoreService = overallScoreService;
            _aspectService = aspectService;
            _yogaDetectionService = yogaDetectionService;
            _yogaMetadataService = yogaMetadataService;
        }

        [HttpPost("generate")]
        public ActionResult<GenerateKundaliResponse> Generate([FromBody] GenerateKundaliRequest request)
        {
            if (request == null)
                return BadRequest("Request is required");

            // Generate planet positions (Kundali chart)
            TimeZoneInfo indiaZone = TimeZoneInfo.FindSystemTimeZoneById("India Standard Time");

            // Assume the input is in IST (India Standard Time)
            var birthLocal = DateTime.SpecifyKind(request.BirthDateTime, DateTimeKind.Unspecified);
            request.BirthDateTime = birthLocal;
            DateTime birthUtc = TimeZoneInfo.ConvertTimeToUtc(request.BirthDateTime, indiaZone);

            request.BirthDateTime= birthUtc;
            var planets = _chartService.GenerateChart(request.BirthDateTime, request.Latitude, request.Longitude );
            // Panchang
            double sunLongitude = planets.First(p => p.Planet == Planet.Sun).Longitude;
            double moonLongitude = planets.First(p => p.Planet == Planet.Moon).Longitude;
            var panchang = _panchangService.Calculate(request.BirthDateTime, sunLongitude, moonLongitude);

            // Dasha
            var moonPos = planets.First(p => p.Planet == Planet.Moon);
            var birthDasha = _dashaService.GetBirthMahadasha(moonPos);

            // Ascendant
            var ascCalc = new AscendantCalculator();
            double ascendantLongitude = ascCalc.Calculate(request.BirthDateTime, request.Latitude, request.Longitude);
            int ascendantIndex = (int)(ascendantLongitude / 30.0);
            ZodiacSign ascendantSign = (ZodiacSign)((ascendantIndex % 12) + 1);

            // Houses and house scores
            var aspectResult = _aspectService.CalculateGrahaDrishti(planets);
            var houseScores = _houseScoreservice.CalculateAllHouseScores(ascendantSign, planets, aspectResult);

            // Compute house signs (house N sign = ascendantSign + (N-1))
            var houseDtos = new List<DTOs.Responses.HouseDto>();
            for (int i = 1; i <= 12; i++)
            {
                var houseNumber = (HouseNumber)i;
                var signIndex = (((int)ascendantSign - 1) + (i - 1)) % 12;
                var houseSign = (ZodiacSign)(signIndex + 1);
                var houseOwner = HelperData.GetSignOwner(houseSign);

                var houseAspects = aspectResult.HouseAspects
                    .Where(a => a.ToHouse == i)
                    .ToList();
                var occupants = planets.Where(p => (int)p.House == i).Select(p => p.Planet.ToString()).ToList();
                var scoreResult = houseScores.First(h => (int)h.HouseNumber == i);


                houseDtos.Add(new DTOs.Responses.HouseDto
                {
                    HouseNumber = i,
                    Sign = houseSign.ToString(),
                    HouseOwner = houseOwner,
                    HouseSwamy = houseOwner,
                    Occupants = occupants,
                    HasBeneficAspect = houseAspects.Any(a => HelperData.IsNaturalBenefic(a.FromPlanet)),
                    HasMaleficAspect = houseAspects.Any(a => HelperData.IsNaturalMaleficOrShadow(a.FromPlanet)),
                    Score = scoreResult.Score,
                    Factors = scoreResult.Factors,
                    DominantThemes = scoreResult.DominantThemes,
                    HouseName = HouseMetadataService.GetHouseName(i),
                    LifeAreas = HouseMetadataService.GetLifeAreas(i),
                });
            }

            // map the new ScoreBreakdown from domain result into DTO
            var overall = _overallScoreService.Calculate(ascendantSign, planets, aspectResult, null);

            var overallDto = new OverallScoreDto
            {
                Overall = overall.OverallScore,
                Stability = overall.StabilityScore,
                Growth = overall.GrowthScore,
                Challenge = overall.ChallengeScore,
                DominantStrengths = overall.DominantStrengths,
                DominantWeaknesses = overall.DominantWeaknesses,
                OverallFactors = overall.OverallFactors,
                StabilityFactors = overall.StabilityFactors,
                GrowthFactors = overall.GrowthFactors,
                ChallengeFactors = overall.ChallengeFactors,
                ScoreBreakdown = new ScoreBreakdownDto
                {
                    Overall = overall.ScoreBreakdown.Overall.Select(c => new ScoreComponentDto { Label = c.Label, Score = c.Score, Weight = c.Weight, Contribution = c.Contribution }).ToList(),
                    Stability = overall.ScoreBreakdown.Stability.Select(c => new ScoreComponentDto { Label = c.Label, Score = c.Score, Weight = c.Weight, Contribution = c.Contribution }).ToList(),
                    Growth = overall.ScoreBreakdown.Growth.Select(c => new ScoreComponentDto { Label = c.Label, Score = c.Score, Weight = c.Weight, Contribution = c.Contribution }).ToList(),
                    Challenge = overall.ScoreBreakdown.Challenge.Select(c => new ScoreComponentDto { Label = c.Label, Score = c.Score, Weight = c.Weight, Contribution = c.Contribution }).ToList()
                }
            };

            var dashaCalculation = _dashaService.CalculateCurrentDasha(
                birthDasha.Planet,
                birthDasha.RemainingYears,
                request.BirthDateTime,
                DateTime.UtcNow);

            var currentDashaDto = new CurrentDashaDto
            {
                Mahadasha = dashaCalculation.CurrentMahadasha.Planet.ToString(),
                Antardasha = dashaCalculation.CurrentAntardasha.SubPlanet.ToString(),
                StartDate = dashaCalculation.CurrentMahadasha.StartDate,
                EndDate = dashaCalculation.CurrentMahadasha.EndDate,
                AntardashaStartDate = dashaCalculation.CurrentAntardasha.StartDate,
                AntardashaEndDate = dashaCalculation.CurrentAntardasha.EndDate,
                Summary = string.Empty
            };
            // Detect yogas
            var detectedYogas = _yogaDetectionService.DetectYogas(planets, houseDtos.Select(h => new Domain.Models.HouseDto
            {
                HouseNumber = (HouseNumber)h.HouseNumber,
                Sign = Enum.Parse<ZodiacSign>(h.Sign),
                Occupants = h.Occupants.Select(o => Enum.Parse<Planet>(o)).ToList()
            }).ToList());

            // Map to YogaDto
            var yogas = detectedYogas
                .Select(match => new { Match = match, Meta = _yogaMetadataService.GetYoga(match.Key) })
                .Where(item => item.Meta != null)
                .Select(item => new YogaDto
                {
                    Key = item.Meta!.Key,
                    Name = item.Meta.Name,
                    SanskritName = item.Meta.SanskritName,
                    Category = item.Meta.Category,
                    Nature = item.Meta.Nature,
                    Description = item.Meta.Description,
                    Impact = item.Meta.Impact,
                    References = item.Meta.References,
                    Strength = item.Match.Strength,
                    InvolvedPlanets = item.Match.InvolvedPlanets,
                    InvolvedHouses = item.Match.InvolvedHouses,
                    Reason = item.Match.Reason
                })
                .ToList();
            // Build response (mapped)
            var response = new GenerateKundaliResponse
            {
                Summary = new KundaliSummaryDto
                {
                    Name = request.Name,
                    Ascendant = $"{(int)ascendantSign} - {ascendantSign}",
                    MoonSign = moonPos.Sign.ToString(),
                    SunSign = planets.First(p => p.Planet == Planet.Sun).Sign.ToString(),
                    Nakshatra = panchang.Nakshatra,
                    Tithi = panchang.Tithi,
                    CurrentMahadasha = currentDashaDto.Mahadasha,
                    CurrentAntardasha = currentDashaDto.Antardasha,
                    Summary = "Kundali generated successfully"
                },
                Panchang = new PanchangDto
                {
                    Tithi = panchang.Tithi,
                    Paksha = panchang.Paksha,
                    Nakshatra = panchang.Nakshatra,
                    Pada = panchang.Pada,
                    Yoga = panchang.Yoga,
                    Karana = panchang.Karana,
                    Vaar = panchang.Vaar,
                    Maas = panchang.Maas,
                    Shake=panchang.Shake,
                    Samvat=panchang.Samvat

                },
                Planets = planets.Select(p => new PlanetDto
                {
                    Planet = p.Planet.ToString(),
                    Sign = p.Sign.ToString(),
                    Degree = p.Degree,
                    Minute = p.Minute,
                    House = (int)p.House,
                    Longitude = p.Longitude,
                    Latitude = p.Latitude,
                    IsCombust = p.IsCombust,
                    IsRetrograde = p.IsRetrograde,
                    DetailedStrength = p.DetailedStrength,
                    Nakshatra = p.Nakshatra.ToString(),
                    Pada = p.Pada
                }).ToList(),
                Houses = houseDtos,
                Scores = overallDto,
                CurrentDasha = currentDashaDto,
                Yogas = yogas
            };

            return Ok(response);
        }

        [HttpPost("planet-positions")]
        public ActionResult<List<PlanetDto>> PlanetPositions([FromBody] GenerateKundaliRequest request)
        {
            if (request == null)
                return BadRequest("Request is required");

            var planets = _chartService.GenerateChart(request.BirthDateTime, request.Latitude, request.Longitude);

            var result = planets.Select(p => new PlanetDto
            {
                Planet = p.Planet.ToString(),
                Sign = p.Sign.ToString(),
                Degree = p.Degree,
                Minute = p.Minute,
                House = (int)p.House,
                Longitude = p.Longitude,
                Latitude = p.Latitude,
                IsCombust = p.IsCombust,
                IsRetrograde = p.IsRetrograde,
                DetailedStrength = p.DetailedStrength,
                Nakshatra = p.Nakshatra.ToString(),
                Pada = p.Pada
            }).ToList();

            return Ok(result);
        }

        [HttpPost("houses")]
        public ActionResult<List<DTOs.Responses.HouseDto>> Houses([FromBody] GenerateKundaliRequest request)
        {
            if (request == null)
                return BadRequest("Request is required");

            var planets = _chartService.GenerateChart(request.BirthDateTime, request.Latitude, request.Longitude);

            // Placeholder: return empty house DTOs for Phase 1
            var result = Enumerable.Range(1, 12).Select(i => new DTOs.Responses.HouseDto()).ToList();

            return Ok(result);
        }

        [HttpPost("summary")]
        public ActionResult<KundaliSummaryDto> Summary([FromBody] GenerateKundaliRequest request)
        {
            if (request == null)
                return BadRequest("Request is required");

            var planets = _chartService.GenerateChart(request.BirthDateTime, request.Latitude, request.Longitude);

            var dto = new KundaliSummaryDto();

            return Ok(dto);
        }

        [HttpPost("score")]
        public ActionResult<OverallScoreDto> Score([FromBody] GenerateKundaliRequest request)
        {
            if (request == null)
                return BadRequest("Request is required");

            var planets = _chartService.GenerateChart(request.BirthDateTime, request.Latitude, request.Longitude);

            // Phase 1: lightweight aggregate using planet base scores
            var planetScores = _planetScoreService.CalculateMany(planets);
            int avgPlanetScore = (int)Math.Round(planetScores.Average(p => p.Score));

            var overall = new OverallScoreDto();

            return Ok(overall);
        }


    }
}
