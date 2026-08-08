using KundaliEngine.API.DTOs.Responses;

namespace KundaliEngine.API.Services.Gochar;

public interface IRetrogradeTransitService
{
    CurrentRetrogradesResponse GetCurrentRetrogrades(bool includeNodes);
}

