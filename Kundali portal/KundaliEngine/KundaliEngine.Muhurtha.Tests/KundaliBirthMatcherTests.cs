using KundaliEngine.API.Services.KundaliSave;
using KundaliEngine.Domain.Entities;

namespace KundaliEngine.Muhurtha.Tests;

public class KundaliBirthMatcherTests
{
    private static readonly Guid UserA = Guid.Parse("aaaaaaaa-aaaa-aaaa-aaaa-aaaaaaaaaaaa");
    private static readonly Guid UserB = Guid.Parse("bbbbbbbb-bbbb-bbbb-bbbb-bbbbbbbbbbbb");
    private static readonly DateOnly BirthDate = new(1992, 5, 12);
    private static readonly TimeOnly BirthMinute = new(10, 32, 0);
    private const decimal Latitude = 12.9716m;
    private const decimal Longitude = 77.5946m;

    [Fact]
    public void Match_ignores_seconds_within_the_same_minute()
    {
        var saved = Chart(UserA, new TimeOnly(10, 32, 47), Latitude, Longitude);

        var match = KundaliBirthMatcher.FindNewest(
            new[] { saved },
            UserA,
            BirthDate,
            BirthMinute,
            Latitude,
            Longitude);

        Assert.Same(saved, match);
        Assert.False(KundaliBirthMatcher.IsSameBirthMoment(
            saved,
            UserA,
            BirthDate,
            new TimeOnly(10, 33, 0),
            Latitude,
            Longitude));
    }

    [Fact]
    public void Match_treats_coordinates_past_four_decimals_as_the_same_place()
    {
        var saved = Chart(UserA, BirthMinute, 12.97161m, 77.594649m);

        Assert.NotNull(KundaliBirthMatcher.FindNewest(
            new[] { saved },
            UserA,
            BirthDate,
            BirthMinute,
            12.97164m,
            77.59461m));

        Assert.Null(KundaliBirthMatcher.FindNewest(
            new[] { saved },
            UserA,
            BirthDate,
            BirthMinute,
            12.97165m,
            77.5946m));
    }

    [Fact]
    public void Match_does_not_cross_users()
    {
        var saved = Chart(UserA, BirthMinute, Latitude, Longitude);

        Assert.Null(KundaliBirthMatcher.FindNewest(
            new[] { saved },
            UserB,
            BirthDate,
            BirthMinute,
            Latitude,
            Longitude));
    }

    [Fact]
    public void Ask_returns_the_existing_chart_without_adding_a_row()
    {
        var existing = Chart(UserA, BirthMinute, Latitude, Longitude);
        var rows = new List<Kundali> { existing };
        var match = KundaliBirthMatcher.FindNewest(rows, UserA, BirthDate, BirthMinute, Latitude, Longitude);

        Assert.Equal(KundaliSaveOutcome.AlreadySaved, KundaliBirthMatcher.Plan(match is not null, KundaliDuplicateMode.Ask));
        Assert.Equal(existing.Id, match!.Id);
        Assert.Single(rows);
    }

    [Fact]
    public void Update_keeps_the_existing_id()
    {
        var existing = Chart(UserA, BirthMinute, Latitude, Longitude);
        var match = KundaliBirthMatcher.FindNewest(new[] { existing }, UserA, BirthDate, BirthMinute, Latitude, Longitude);

        Assert.Equal(KundaliSaveOutcome.Update, KundaliBirthMatcher.Plan(match is not null, KundaliDuplicateMode.Update));
        Assert.Equal(existing.Id, match!.Id);
    }

    [Fact]
    public void Create_inserts_a_second_row_that_counts_toward_the_limit()
    {
        var existing = Chart(UserA, BirthMinute, Latitude, Longitude);
        var rows = new List<Kundali> { existing };
        var match = KundaliBirthMatcher.FindNewest(rows, UserA, BirthDate, BirthMinute, Latitude, Longitude);
        var outcome = KundaliBirthMatcher.Plan(match is not null, KundaliDuplicateMode.Create);

        Assert.Equal(KundaliSaveOutcome.Insert, outcome);
        rows.Add(Chart(UserA, BirthMinute, Latitude, Longitude));

        Assert.Equal(2, rows.Count);
        Assert.NotEqual(existing.Id, rows[1].Id);
    }

    [Fact]
    public void Reuse_returns_the_existing_chart_without_planning_an_update()
    {
        Assert.Equal(
            KundaliSaveOutcome.ReuseExisting,
            KundaliBirthMatcher.Plan(hasMatch: true, KundaliDuplicateMode.Reuse));
        Assert.Equal(
            KundaliSaveOutcome.Insert,
            KundaliBirthMatcher.Plan(hasMatch: false, KundaliDuplicateMode.Reuse));
    }

    [Fact]
    public void ParseMode_defaults_to_ask()
    {
        Assert.Equal(KundaliDuplicateMode.Ask, KundaliBirthMatcher.ParseMode(null));
        Assert.Equal(KundaliDuplicateMode.Ask, KundaliBirthMatcher.ParseMode(" "));
        Assert.Equal(KundaliDuplicateMode.Update, KundaliBirthMatcher.ParseMode("UPDATE"));
    }

    private static Kundali Chart(Guid userId, TimeOnly time, decimal latitude, decimal longitude)
    {
        return new Kundali
        {
            Id = Guid.NewGuid(),
            UserId = userId,
            FullName = "Asha",
            DateOfBirth = BirthDate,
            TimeOfBirth = time,
            BirthPlace = "Bengaluru",
            Latitude = latitude,
            Longitude = longitude,
            ChartDataJson = "{}",
            CreatedAt = DateTime.UtcNow
        };
    }
}
