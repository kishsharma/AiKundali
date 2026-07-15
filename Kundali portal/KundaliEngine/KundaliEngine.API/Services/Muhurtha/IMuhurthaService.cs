using KundaliEngine.Domain.Models.Muhurtha;

namespace KundaliEngine.API.Services.Muhurtha;

public interface IMuhurthaService
{
    Task<MuhurthaResponse> FindMuhurthaAsync(
        MuhurthaRequest request,
        CancellationToken cancellationToken = default);
}
