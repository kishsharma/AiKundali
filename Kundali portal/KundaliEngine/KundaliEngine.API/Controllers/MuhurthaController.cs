using KundaliEngine.API.DTOs.Muhurtha.Requests;
using KundaliEngine.API.DTOs.Muhurtha.Responses;
using KundaliEngine.API.Services.Muhurtha;
using KundaliEngine.Domain.Models.Muhurtha;
using Microsoft.AspNetCore.Mvc;

namespace KundaliEngine.API.Controllers;

[Route("api/[controller]")]
[ApiController]
public class MuhurthaController : ControllerBase
{
    private readonly IMuhurthaService _muhurthaService;

    public MuhurthaController(IMuhurthaService muhurthaService)
    {
        _muhurthaService = muhurthaService;
    }

    [HttpPost("find")]
    public async Task<ActionResult<MuhurthaResponseDto>> Find(
        [FromBody] FindMuhurthaRequestDto request,
        CancellationToken cancellationToken)
    {
        if (request == null)
        {
            return BadRequest("Request is required.");
        }

        try
        {
            var response = await _muhurthaService.FindMuhurthaAsync(
                MapRequest(request),
                cancellationToken);

            return Ok(MapResponse(response));
        }
        catch (ArgumentException ex)
        {
            return BadRequest(ex.Message);
        }
    }

    private static MuhurthaRequest MapRequest(FindMuhurthaRequestDto dto)
    {
        return new MuhurthaRequest
        {
            EventType = dto.EventType,
            RegionalProfile = dto.RegionalProfile,
            Location = new MuhurthaLocation
            {
                Name = dto.Location.Name,
                Latitude = dto.Location.Latitude,
                Longitude = dto.Location.Longitude,
                Timezone = dto.Location.Timezone
            },
            RequestedStart = dto.RequestedStart,
            RequestedEnd = dto.RequestedEnd,
            DailyStartTime = dto.DailyStartTime,
            DailyEndTime = dto.DailyEndTime,
            Horoscope = dto.Horoscope == null
                ? null
                : new MuhurthaHoroscopeReference
                {
                    Name = dto.Horoscope.Name,
                    BirthDateTime = dto.Horoscope.BirthDateTime,
                    BirthPlace = dto.Horoscope.BirthPlace,
                    Latitude = dto.Horoscope.Latitude,
                    Longitude = dto.Horoscope.Longitude,
                    Timezone = dto.Horoscope.Timezone
                },
            Filters = new MuhurthaUserFilters
            {
                AvoidRahuKalam = dto.Filters.AvoidRahuKalam,
                AvoidBhadra = dto.Filters.AvoidBhadra,
                PreferGoodChoghadiya = dto.Filters.PreferGoodChoghadiya,
                CheckPersonalHoroscope = dto.Filters.CheckPersonalHoroscope,
                CheckMuhurthaLagna = dto.Filters.CheckMuhurthaLagna
            }
        };
    }

    private static MuhurthaResponseDto MapResponse(MuhurthaResponse response)
    {
        return new MuhurthaResponseDto
        {
            EventType = response.EventType,
            RegionalProfile = response.RegionalProfile,
            Location = new MuhurthaLocationDto
            {
                Name = response.Location.Name,
                Latitude = response.Location.Latitude,
                Longitude = response.Location.Longitude,
                Timezone = response.Location.Timezone
            },
            RequestedStart = response.RequestedStart,
            RequestedEnd = response.RequestedEnd,
            UsedPersonalHoroscope = response.UsedPersonalHoroscope,
            UnsupportedCalculations = response.UnsupportedCalculations,
            RecommendedSlots = response.RecommendedSlots.Select(MapRecommendation).ToList()
        };
    }

    private static MuhurthaRecommendationDto MapRecommendation(MuhurthaRecommendation recommendation)
    {
        return new MuhurthaRecommendationDto
        {
            SlotId = recommendation.SlotId,
            StartTime = recommendation.StartTime,
            EndTime = recommendation.EndTime,
            Score = recommendation.Score,
            Verdict = recommendation.Verdict,
            HardRejected = recommendation.HardRejected,
            Strengths = recommendation.Strengths,
            Warnings = recommendation.Warnings,
            RuleResults = recommendation.RuleResults.Select(MapRuleResult).ToList(),
            PanchangTimings = MapPanchangTimings(recommendation.PanchangTimings),
            ScoreBreakdown = new MuhurthaScoreBreakdownDto
            {
                GenericTimeRulesScore = recommendation.ScoreBreakdown.GenericTimeRulesScore,
                PanchangaRulesScore = recommendation.ScoreBreakdown.PanchangaRulesScore,
                PersonalHoroscopeRulesScore = recommendation.ScoreBreakdown.PersonalHoroscopeRulesScore,
                MuhurthaChartRulesScore = recommendation.ScoreBreakdown.MuhurthaChartRulesScore,
                TotalScore = recommendation.ScoreBreakdown.TotalScore,
                HardRejected = recommendation.ScoreBreakdown.HardRejected
            }
        };
    }

    private static MuhurthaPanchangTimingsDto MapPanchangTimings(MuhurthaPanchangTimings timings)
    {
        return new MuhurthaPanchangTimingsDto
        {
            Tithi = MapTimingDetail(timings.Tithi),
            Nakshatra = MapTimingDetail(timings.Nakshatra),
            Karana = MapTimingDetail(timings.Karana),
            Yoga = MapTimingDetail(timings.Yoga),
            ChandraRashi = MapTimingDetail(timings.ChandraRashi),
            Lagna = MapTimingDetail(timings.Lagna)
        };
    }

    private static MuhurthaTimingDetailDto MapTimingDetail(MuhurthaTimingDetail detail)
    {
        return new MuhurthaTimingDetailDto
        {
            Name = detail.Name,
            StartTime = detail.StartTime,
            EndTime = detail.EndTime
        };
    }
    private static MuhurthaRuleResultDto MapRuleResult(MuhurthaRuleResult result)
    {
        return new MuhurthaRuleResultDto
        {
            RuleKey = result.RuleKey,
            Status = result.Status,
            Severity = result.Severity,
            ScoreImpact = result.ScoreImpact,
            HardRejected = result.HardRejected,
            StartTime = result.StartTime,
            EndTime = result.EndTime,
            Summary = result.Summary,
            Recommendation = result.Recommendation,
            Metadata = result.Metadata
        };
    }
}



