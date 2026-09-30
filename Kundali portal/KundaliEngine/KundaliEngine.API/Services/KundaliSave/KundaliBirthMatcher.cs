using KundaliEngine.Domain.Entities;

namespace KundaliEngine.API.Services.KundaliSave;

public enum KundaliDuplicateMode
{
    Ask,
    Update,
    Create,
    Reuse
}

public enum KundaliSaveOutcome
{
    Insert,
    Update,
    ReuseExisting,
    AlreadySaved
}

public static class KundaliBirthMatcher
{
    public const int CoordinateDecimals = 4;

    public static decimal RoundCoordinate(decimal value) =>
        Math.Round(value, CoordinateDecimals, MidpointRounding.AwayFromZero);

    public static KundaliDuplicateMode ParseMode(string? onDuplicate)
    {
        return onDuplicate?.Trim().ToLowerInvariant() switch
        {
            "update" => KundaliDuplicateMode.Update,
            "create" => KundaliDuplicateMode.Create,
            "reuse" => KundaliDuplicateMode.Reuse,
            _ => KundaliDuplicateMode.Ask
        };
    }

    public static bool IsSameBirthMoment(
        Kundali candidate,
        Guid userId,
        DateOnly dateOfBirth,
        TimeOnly timeOfBirth,
        decimal latitude,
        decimal longitude)
    {
        if (candidate.UserId != userId || candidate.DateOfBirth != dateOfBirth)
        {
            return false;
        }

        if (candidate.TimeOfBirth.Hour != timeOfBirth.Hour || candidate.TimeOfBirth.Minute != timeOfBirth.Minute)
        {
            return false;
        }

        if (candidate.Latitude is null || candidate.Longitude is null)
        {
            return false;
        }

        return RoundCoordinate(candidate.Latitude.Value) == RoundCoordinate(latitude)
            && RoundCoordinate(candidate.Longitude.Value) == RoundCoordinate(longitude);
    }

    public static Kundali? FindNewest(
        IEnumerable<Kundali> kundalis,
        Guid userId,
        DateOnly dateOfBirth,
        TimeOnly timeOfBirth,
        decimal latitude,
        decimal longitude)
    {
        return kundalis
            .Where(candidate => IsSameBirthMoment(candidate, userId, dateOfBirth, timeOfBirth, latitude, longitude))
            .OrderByDescending(candidate => candidate.CreatedAt)
            .ThenByDescending(candidate => candidate.Id)
            .FirstOrDefault();
    }

    public static KundaliSaveOutcome Plan(bool hasMatch, KundaliDuplicateMode mode)
    {
        if (!hasMatch)
        {
            return KundaliSaveOutcome.Insert;
        }

        return mode switch
        {
            KundaliDuplicateMode.Update => KundaliSaveOutcome.Update,
            KundaliDuplicateMode.Create => KundaliSaveOutcome.Insert,
            KundaliDuplicateMode.Reuse => KundaliSaveOutcome.ReuseExisting,
            _ => KundaliSaveOutcome.AlreadySaved
        };
    }
}
