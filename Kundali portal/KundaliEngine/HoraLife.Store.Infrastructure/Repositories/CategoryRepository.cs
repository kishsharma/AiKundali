using HoraLife.Store.Application.Interfaces;
using HoraLife.Store.Domain.Entities;
using HoraLife.Store.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;

namespace HoraLife.Store.Infrastructure.Repositories;

public sealed class CategoryRepository : ICategoryRepository
{
    private readonly StoreDbContext _dbContext;

    public CategoryRepository(StoreDbContext dbContext)
    {
        _dbContext = dbContext;
    }

    public async Task<IReadOnlyList<Category>> GetActiveAsync(
        CancellationToken cancellationToken = default)
    {
        return await _dbContext.Categories
            .AsNoTracking()
            .Where(category => category.IsActive)
            .Include(category => category.Products
                .Where(product => product.IsActive))
            .OrderBy(category => category.DisplayOrder)
            .ThenBy(category => category.Name)
            .ToListAsync(cancellationToken);
    }
}
