using HoraLife.Store.Application.Interfaces;
using HoraLife.Store.Domain.Entities;
using HoraLife.Store.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;

namespace HoraLife.Store.Infrastructure.Repositories;

public sealed class CustomerAddressRepository :
    ICustomerAddressRepository
{
    private readonly StoreDbContext _dbContext;

    public CustomerAddressRepository(
        StoreDbContext dbContext)
    {
        _dbContext = dbContext;
    }

    public async Task<IReadOnlyList<CustomerAddress>> GetByUserIdAsync(
        Guid userId,
        bool trackChanges,
        CancellationToken cancellationToken = default)
    {
        IQueryable<CustomerAddress> query =
            _dbContext.CustomerAddresses
                .Where(x => x.UserId == userId);

        if (!trackChanges)
        {
            query = query.AsNoTracking();
        }

        return await query
            .OrderBy(x => x.AddressType)
            .ThenByDescending(x => x.IsDefault)
            .ThenByDescending(x => x.UpdatedAt)
            .ToListAsync(cancellationToken);
    }

    public async Task<CustomerAddress?> GetByIdAsync(
        Guid userId,
        Guid addressId,
        bool trackChanges,
        CancellationToken cancellationToken = default)
    {
        IQueryable<CustomerAddress> query =
            _dbContext.CustomerAddresses
                .Where(x =>
                    x.UserId == userId &&
                    x.Id == addressId);

        if (!trackChanges)
        {
            query = query.AsNoTracking();
        }

        return await query.SingleOrDefaultAsync(
            cancellationToken);
    }

    public async Task AddAsync(
        CustomerAddress address,
        CancellationToken cancellationToken = default)
    {
        await _dbContext.CustomerAddresses.AddAsync(
            address,
            cancellationToken);
    }

    public void Remove(CustomerAddress address)
    {
        _dbContext.CustomerAddresses.Remove(address);
    }

    public Task<int> SaveChangesAsync(
        CancellationToken cancellationToken = default)
    {
        return _dbContext.SaveChangesAsync(
            cancellationToken);
    }
}
