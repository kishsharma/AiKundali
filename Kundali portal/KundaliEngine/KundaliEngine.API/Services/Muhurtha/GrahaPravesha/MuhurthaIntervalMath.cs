namespace KundaliEngine.API.Services.Muhurtha.GrahaPravesha;

public readonly record struct TimeRange(DateTimeOffset Start, DateTimeOffset End);

public static class MuhurthaIntervalMath
{
    public static IReadOnlyList<TimeRange> Subtract(TimeRange source, IEnumerable<TimeRange> blocked)
    {
        var free = new List<TimeRange> { source };
        foreach (var block in blocked.Where(item => item.End > item.Start).OrderBy(item => item.Start))
        {
            var next = new List<TimeRange>();
            foreach (var span in free)
            {
                if (block.End <= span.Start || block.Start >= span.End)
                {
                    next.Add(span);
                    continue;
                }

                if (block.Start > span.Start)
                {
                    next.Add(new TimeRange(span.Start, block.Start));
                }

                if (block.End < span.End)
                {
                    next.Add(new TimeRange(block.End, span.End));
                }
            }

            free = next;
        }

        return free.Where(span => span.End > span.Start).ToList();
    }

    public static IReadOnlyList<TimeRange> Intersections(TimeRange source, IEnumerable<TimeRange> blocked)
    {
        var hits = new List<TimeRange>();
        foreach (var block in blocked.Where(item => item.End > item.Start))
        {
            var start = source.Start > block.Start ? source.Start : block.Start;
            var end = source.End < block.End ? source.End : block.End;
            if (end > start)
            {
                hits.Add(new TimeRange(start, end));
            }
        }

        return hits;
    }

    public static IEnumerable<DateOnly> EnumerateDates(DateOnly from, DateOnly to)
    {
        if (to < from)
        {
            yield break;
        }

        for (var date = from; date <= to; date = date.AddDays(1))
        {
            yield return date;
        }
    }
}

public readonly record struct BoundarySlice(DateTimeOffset Start, DateTimeOffset End, string Key);

public static class PanchangBoundaryWalker
{
    public static IReadOnlyList<BoundarySlice> Split(
        DateTimeOffset start,
        DateTimeOffset end,
        Func<DateTimeOffset, string> keyAt,
        TimeSpan precision)
    {
        if (end <= start)
        {
            return Array.Empty<BoundarySlice>();
        }

        if (precision <= TimeSpan.Zero)
        {
            precision = TimeSpan.FromSeconds(1);
        }

        var slices = new List<BoundarySlice>();
        var cursor = start;
        while (cursor < end)
        {
            var key = keyAt(cursor);
            var boundary = FindChange(cursor, end, key, keyAt, precision);
            if (boundary <= cursor)
            {
                boundary = cursor + precision;
                if (boundary > end)
                {
                    boundary = end;
                }
            }

            slices.Add(new BoundarySlice(cursor, boundary, key));
            cursor = boundary;
        }

        return slices;
    }

    private static DateTimeOffset FindChange(
        DateTimeOffset start,
        DateTimeOffset end,
        string key,
        Func<DateTimeOffset, string> keyAt,
        TimeSpan precision)
    {
        var probe = end - precision;
        if (probe <= start)
        {
            return end;
        }

        if (string.Equals(keyAt(probe), key, StringComparison.Ordinal) &&
            string.Equals(keyAt(end - TimeSpan.FromTicks(1)), key, StringComparison.Ordinal))
        {
            return end;
        }

        var lo = start;
        var hi = end;
        var fine = TimeSpan.FromTicks(1);
        while (hi - lo > fine)
        {
            var mid = lo + TimeSpan.FromTicks((hi - lo).Ticks / 2);
            if (mid <= lo)
            {
                break;
            }

            if (string.Equals(keyAt(mid), key, StringComparison.Ordinal))
            {
                lo = mid;
            }
            else
            {
                hi = mid;
            }
        }

        return hi;
    }
}
