using HoraLife.Store.Application.Addresses.Dtos;
using HoraLife.Store.Application.Addresses.Models;

namespace HoraLife.Store.Application.Interfaces;

public interface IStoreAddressService
{
    Task<IReadOnlyList<CustomerAddressDto>> GetAddressesAsync(
        Guid userId,
        CancellationToken cancellationToken = default);

    Task<CustomerAddressDto?> GetAddressByIdAsync(
        Guid userId,
        Guid addressId,
        CancellationToken cancellationToken = default);

    Task<AddressOperationResult<CustomerAddressDto>> CreateAddressAsync(
        Guid userId,
        SaveCustomerAddressModel model,
        CancellationToken cancellationToken = default);

    Task<AddressOperationResult<CustomerAddressDto>> UpdateAddressAsync(
        Guid userId,
        Guid addressId,
        SaveCustomerAddressModel model,
        CancellationToken cancellationToken = default);

    Task<AddressOperationResult<bool>> DeleteAddressAsync(
        Guid userId,
        Guid addressId,
        CancellationToken cancellationToken = default);

    Task<AddressOperationResult<CustomerAddressDto>> SetDefaultAddressAsync(
        Guid userId,
        Guid addressId,
        CancellationToken cancellationToken = default);
}
