using KundaliEngine.API.Services.Muhurtha.Models;
using KundaliEngine.Domain.Enums;
using KundaliEngine.Domain.Models.Muhurtha;

namespace KundaliEngine.API.Services.Muhurtha;

public class MuhurthaCandidateGenerator : IMuhurthaCandidateGenerator
{
    private readonly IMuhurthaSunriseSunsetService _sunriseSunsetService;
    private readonly IMuhurthaGenericTimeWindowService _genericTimeWindowService;
    private readonly IVastuMuhurthaEligibilityService _vastuEligibilityService;

    public MuhurthaCandidateGenerator(
        IMuhurthaSunriseSunsetService sunriseSunsetService,
        IMuhurthaGenericTimeWindowService genericTimeWindowService,
        IVastuMuhurthaEligibilityService vastuEligibilityService)
    {
        _sunriseSunsetService = sunriseSunsetService;
        _genericTimeWindowService = genericTimeWindowService;
        _vastuEligibilityService = vastuEligibilityService;
    }

    public Task<IReadOnlyList<MuhurthaCandidateSlot>> GenerateAsync(
        MuhurthaRuleContext context,
        CancellationToken cancellationToken = default)
    {
        var request = context.Request;
        var offset = request.RequestedStart.Offset;
        var slots = new List<MuhurthaCandidateSlot>();
        var currentDate = DateOnly.FromDateTime(request.RequestedStart.Date);
        var endDate = DateOnly.FromDateTime(request.RequestedEnd.Date);

        while (currentDate <= endDate)
        {
            var dayContext = BuildDayContext(currentDate, request, offset);
            if (dayContext.DayEnd > dayContext.DayStart)
            {
                slots.AddRange(FilterByVastuEligibility(request, BuildSlots(dayContext)));
            }

            currentDate = currentDate.AddDays(1);
        }

        return Task.FromResult<IReadOnlyList<MuhurthaCandidateSlot>>(slots);
    }

    private MuhurthaDayContext BuildDayContext(
        DateOnly date,
        MuhurthaRequest request,
        TimeSpan offset)
    {
        var (sunrise, sunset) = _sunriseSunsetService.Calculate(date, request.Location, offset);
        var dayStart = Max(
            request.RequestedStart,
            request.DailyStartTime.HasValue
                ? new DateTimeOffset(date.ToDateTime(TimeOnly.FromTimeSpan(request.DailyStartTime.Value)), offset)
                : sunrise,
            sunrise);
        var dayEnd = Min(
            request.RequestedEnd,
            request.DailyEndTime.HasValue
                ? new DateTimeOffset(date.ToDateTime(TimeOnly.FromTimeSpan(request.DailyEndTime.Value)), offset)
                : sunset,
            sunset);

        var dayContext = new MuhurthaDayContext
        {
            Date = date,
            Sunrise = sunrise,
            Sunset = sunset,
            DayStart = dayStart,
            DayEnd = dayEnd
        };
        dayContext.TimeWindows = _genericTimeWindowService.BuildWindows(dayContext).ToList();
        return dayContext;
    }

    private IEnumerable<MuhurthaCandidateSlot> FilterByVastuEligibility(
        MuhurthaRequest request,
        IEnumerable<MuhurthaCandidateSlot> candidates)
    {
        if (!_vastuEligibilityService.ShouldApply(request.EventType))
        {
            return candidates;
        }

        return candidates.Where(candidate =>
        {
            var eligibility = _vastuEligibilityService.Evaluate(request, candidate);
            if (eligibility is null)
            {
                return false;
            }

            candidate.Metadata["vastuEligibility.paksha"] = eligibility.Paksha;
            candidate.Metadata["vastuEligibility.tithi"] = eligibility.Tithi;
            candidate.Metadata["vastuEligibility.weekday"] = eligibility.Weekday;
            candidate.Metadata["vastuEligibility.moonSign"] = eligibility.MoonSign;
            candidate.Metadata["vastuEligibility.applicability"] = eligibility.Applicability;
            candidate.Metadata["vastuEligibility.ahuti"] = eligibility.Ahuti;
            candidate.Metadata["vastuEligibility.scoreImpact"] = eligibility.ScoreImpact.ToString();
            candidate.Metadata["vastuEligibility.source"] = eligibility.Source;
            return true;
        });
    }

    private static IEnumerable<MuhurthaCandidateSlot> BuildSlots(MuhurthaDayContext dayContext)
    {
        var boundaries = new SortedSet<DateTimeOffset>
        {
            dayContext.DayStart,
            dayContext.DayEnd
        };

        foreach (var window in dayContext.TimeWindows)
        {
            if (window.StartTime > dayContext.DayStart && window.StartTime < dayContext.DayEnd)
            {
                boundaries.Add(window.StartTime);
            }

            if (window.EndTime > dayContext.DayStart && window.EndTime < dayContext.DayEnd)
            {
                boundaries.Add(window.EndTime);
            }
        }

        var ordered = boundaries.ToList();
        for (var i = 0; i < ordered.Count - 1; i++)
        {
            var start = ordered[i];
            var end = ordered[i + 1];
            if (end <= start || end - start < TimeSpan.FromMinutes(5))
            {
                continue;
            }

            yield return new MuhurthaCandidateSlot
            {
                StartTime = start,
                EndTime = end,
                Status = MuhurthaCandidateStatus.Valid,
                Score = 50,
                Metadata =
                {
                    ["source"] = "BoundaryGenerator",
                    ["date"] = dayContext.Date.ToString("yyyy-MM-dd"),
                    ["sunrise"] = dayContext.Sunrise.ToString("O"),
                    ["sunset"] = dayContext.Sunset.ToString("O")
                }
            };
        }
    }

    private static DateTimeOffset Max(params DateTimeOffset[] values) => values.Max();

    private static DateTimeOffset Min(params DateTimeOffset[] values) => values.Min();
}



