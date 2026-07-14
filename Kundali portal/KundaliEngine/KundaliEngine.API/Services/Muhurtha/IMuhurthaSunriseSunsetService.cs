using KundaliEngine.Domain.Models.Muhurtha;

namespace KundaliEngine.API.Services.Muhurtha;

public interface IMuhurthaSunriseSunsetService
{
    (DateTimeOffset Sunrise, DateTimeOffset Sunset) Calculate(
        DateOnly date,
        MuhurthaLocation location,
        TimeSpan offset);
}
