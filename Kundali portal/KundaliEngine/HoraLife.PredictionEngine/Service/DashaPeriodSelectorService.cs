using HoraLife.PredictionEngine.Enums;
using HoraLife.PredictionEngine.Models;

namespace HoraLife.PredictionEngine.Services;

public sealed class DashaPeriodSelectorService
{
    public IReadOnlyList<DashaPeriod> Select(DashaTimeline timeline, PredictionRequest request)
    {
        ArgumentNullException.ThrowIfNull(timeline);
        ArgumentNullException.ThrowIfNull(request);

        return request.TimeScope switch
        {
            PredictionTimeScope.Current =>  SelectCurrent(timeline, request.ReferenceDate),

            PredictionTimeScope.Past => SelectPast(timeline, request.ReferenceDate),

            PredictionTimeScope.Future => SelectFuture(timeline, request.ReferenceDate),

            PredictionTimeScope.DateRange =>SelectDateRange(timeline,request.FromDate, request.ToDate),

            _ => throw new ArgumentOutOfRangeException(
                nameof(request.TimeScope),
                request.TimeScope,
                "Unsupported prediction time scope.")
        };
    }

    private static IReadOnlyList<DashaPeriod> SelectCurrent( DashaTimeline timeline, DateTimeOffset referenceDate)
    {
        return timeline.Periods
            .Where(period =>
                period.StartDate <= referenceDate &&
                referenceDate < period.EndDate)
            .OrderBy(period => period.Level)
            .ToArray();
    }

    private static IReadOnlyList<DashaPeriod> SelectPast( DashaTimeline timeline, DateTimeOffset referenceDate)
    {
        return timeline.Periods
            .Where(period => period.EndDate <= referenceDate)
            .OrderByDescending(period => period.EndDate)
            .ThenBy(period => period.Level)
            .ToArray();
    }

    private static IReadOnlyList<DashaPeriod> SelectFuture( DashaTimeline timeline, DateTimeOffset referenceDate)
    {
        return timeline.Periods
            .Where(period => period.StartDate >= referenceDate)
            .OrderBy(period => period.StartDate)
            .ThenBy(period => period.Level)
            .ToArray();
    }

    private static IReadOnlyList<DashaPeriod> SelectDateRange( DashaTimeline timeline, DateTimeOffset? fromDate, DateTimeOffset? toDate)
    {
        if (fromDate is null || toDate is null)
        {
            throw new ArgumentException(
                "FromDate and ToDate are required for DateRange predictions.");
        }

        if (fromDate >= toDate)
        {
            throw new ArgumentException(
                "FromDate must be earlier than ToDate.");
        }

        return timeline.Periods
            .Where(period =>
                period.StartDate < toDate &&
                period.EndDate > fromDate)
            .OrderBy(period => period.StartDate)
            .ThenBy(period => period.Level)
            .ToArray();
    }
}