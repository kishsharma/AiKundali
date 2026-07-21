using KundaliEngine.API.DTOs.Learning;

namespace KundaliEngine.API.Services.Learning;

public interface ILearningContentService
{
    Task<IReadOnlyList<LearningLevelSummary>> GetLevelsAsync(CancellationToken cancellationToken);
    Task<LearningLevelManifest?> GetLevelAsync(string levelCode, CancellationToken cancellationToken);
    Task<LearningSessionResponse?> GetSessionAsync(Guid sessionGuid, string? language, CancellationToken cancellationToken);
}
