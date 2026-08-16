using KundaliEngine.API.DTOs.Responses;

namespace KundaliEngine.API.Services.Panchang;

public interface IPanchangEclipseService
{
    IReadOnlyList<PanchangEclipseDto> GetYearlyEclipses(int year, double latitude, double longitude, string timezone);
}
