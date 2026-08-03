using HoraLife.Store.Domain.Entities;

namespace HoraLife.Store.Application.Interfaces;

public interface ICategoryRepository
{
    Task<IReadOnlyList<Category>> GetActiveAsync(
        CancellationToken cancellationToken = default);
}
