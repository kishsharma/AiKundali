using HoraLife.Store.Domain.Entities;

namespace HoraLife.Store.Application.Interfaces;

public interface ICustomerAddressRepository
{
    Task<IReadOnlyList<CustomerAddress>> GetByUserIdAsync(
        Guid userId,
        bool trackChanges,
        CancellationToken cancellationToken = default);

    Task<CustomerAddress?> GetByIdAsync(
        Guid userId,
        Guid addressId,
        bool trackChanges,
        CancellationToken cancellationToken = default);

    Task AddAsync(
        CustomerAddress address,
        CancellationToken cancellationToken = default);

    void Remove(CustomerAddress address);

    Task<int> SaveChangesAsync(
        CancellationToken cancellationToken = default);
}
