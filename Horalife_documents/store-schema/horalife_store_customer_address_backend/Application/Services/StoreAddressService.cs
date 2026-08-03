using HoraLife.Store.Application.Addresses.Dtos;
using HoraLife.Store.Application.Addresses.Models;
using HoraLife.Store.Application.Interfaces;
using HoraLife.Store.Domain.Entities;
using HoraLife.Store.Domain.Enums;

namespace HoraLife.Store.Application.Services;

public sealed class StoreAddressService : IStoreAddressService
{
    private readonly ICustomerAddressRepository _addressRepository;

    public StoreAddressService(
        ICustomerAddressRepository addressRepository)
    {
        _addressRepository = addressRepository;
    }

    public async Task<IReadOnlyList<CustomerAddressDto>> GetAddressesAsync(
        Guid userId,
        CancellationToken cancellationToken = default)
    {
        var addresses =
            await _addressRepository.GetByUserIdAsync(
                userId,
                trackChanges: false,
                cancellationToken);

        return addresses
            .OrderBy(x => x.AddressType)
            .ThenByDescending(x => x.IsDefault)
            .ThenByDescending(x => x.UpdatedAt)
            .Select(Map)
            .ToList();
    }

    public async Task<CustomerAddressDto?> GetAddressByIdAsync(
        Guid userId,
        Guid addressId,
        CancellationToken cancellationToken = default)
    {
        var address =
            await _addressRepository.GetByIdAsync(
                userId,
                addressId,
                trackChanges: false,
                cancellationToken);

        return address is null
            ? null
            : Map(address);
    }

    public async Task<AddressOperationResult<CustomerAddressDto>>
        CreateAddressAsync(
            Guid userId,
            SaveCustomerAddressModel model,
            CancellationToken cancellationToken = default)
    {
        var validation = NormalizeAndValidate(model);

        if (!validation.Succeeded)
        {
            return AddressOperationResult<CustomerAddressDto>.Failure(
                validation.ErrorCode,
                validation.ErrorMessage!);
        }

        var normalized = validation.Value!;

        var existingAddresses =
            (await _addressRepository.GetByUserIdAsync(
                userId,
                trackChanges: true,
                cancellationToken))
            .ToList();

        var address = new CustomerAddress
        {
            Id = Guid.NewGuid(),
            UserId = userId,
            AddressType = normalized.AddressType,
            FullName = normalized.FullName,
            Phone = normalized.Phone,
            AddressLine1 = normalized.AddressLine1,
            AddressLine2 = normalized.AddressLine2,
            City = normalized.City,
            State = normalized.State,
            PostalCode = normalized.PostalCode,
            CountryCode = normalized.CountryCode,
            IsDefault = normalized.IsDefault
        };

        existingAddresses.Add(address);

        var shouldPreferNewAddress =
            normalized.IsDefault ||
            existingAddresses.Count(x =>
                x.AddressType == normalized.AddressType) == 1;

        NormalizeDefaultForType(
            existingAddresses,
            normalized.AddressType,
            shouldPreferNewAddress ? address.Id : null);

        await _addressRepository.AddAsync(
            address,
            cancellationToken);

        await _addressRepository.SaveChangesAsync(
            cancellationToken);

        return AddressOperationResult<CustomerAddressDto>.Success(
            Map(address));
    }

    public async Task<AddressOperationResult<CustomerAddressDto>>
        UpdateAddressAsync(
            Guid userId,
            Guid addressId,
            SaveCustomerAddressModel model,
            CancellationToken cancellationToken = default)
    {
        var validation = NormalizeAndValidate(model);

        if (!validation.Succeeded)
        {
            return AddressOperationResult<CustomerAddressDto>.Failure(
                validation.ErrorCode,
                validation.ErrorMessage!);
        }

        var normalized = validation.Value!;

        var addresses =
            (await _addressRepository.GetByUserIdAsync(
                userId,
                trackChanges: true,
                cancellationToken))
            .ToList();

        var address = addresses.SingleOrDefault(
            x => x.Id == addressId);

        if (address is null)
        {
            return AddressOperationResult<CustomerAddressDto>.Failure(
                AddressErrorCode.AddressNotFound,
                "The requested address was not found.");
        }

        var previousType = address.AddressType;
        var wasDefault = address.IsDefault;

        address.AddressType = normalized.AddressType;
        address.FullName = normalized.FullName;
        address.Phone = normalized.Phone;
        address.AddressLine1 = normalized.AddressLine1;
        address.AddressLine2 = normalized.AddressLine2;
        address.City = normalized.City;
        address.State = normalized.State;
        address.PostalCode = normalized.PostalCode;
        address.CountryCode = normalized.CountryCode;
        address.IsDefault = normalized.IsDefault;

        if (previousType != address.AddressType)
        {
            NormalizeDefaultForType(
                addresses,
                previousType);
        }

        NormalizeDefaultForType(
            addresses,
            address.AddressType,
            normalized.IsDefault ? address.Id : null,
            wasDefault && !normalized.IsDefault
                ? address.Id
                : null);

        await _addressRepository.SaveChangesAsync(
            cancellationToken);

        return AddressOperationResult<CustomerAddressDto>.Success(
            Map(address));
    }

    public async Task<AddressOperationResult<bool>> DeleteAddressAsync(
        Guid userId,
        Guid addressId,
        CancellationToken cancellationToken = default)
    {
        var addresses =
            (await _addressRepository.GetByUserIdAsync(
                userId,
                trackChanges: true,
                cancellationToken))
            .ToList();

        var address = addresses.SingleOrDefault(
            x => x.Id == addressId);

        if (address is null)
        {
            return AddressOperationResult<bool>.Failure(
                AddressErrorCode.AddressNotFound,
                "The requested address was not found.");
        }

        var deletedAddressType = address.AddressType;

        addresses.Remove(address);
        _addressRepository.Remove(address);

        NormalizeDefaultForType(
            addresses,
            deletedAddressType);

        await _addressRepository.SaveChangesAsync(
            cancellationToken);

        return AddressOperationResult<bool>.Success(true);
    }

    public async Task<AddressOperationResult<CustomerAddressDto>>
        SetDefaultAddressAsync(
            Guid userId,
            Guid addressId,
            CancellationToken cancellationToken = default)
    {
        var addresses =
            (await _addressRepository.GetByUserIdAsync(
                userId,
                trackChanges: true,
                cancellationToken))
            .ToList();

        var address = addresses.SingleOrDefault(
            x => x.Id == addressId);

        if (address is null)
        {
            return AddressOperationResult<CustomerAddressDto>.Failure(
                AddressErrorCode.AddressNotFound,
                "The requested address was not found.");
        }

        NormalizeDefaultForType(
            addresses,
            address.AddressType,
            preferredAddressId: address.Id);

        await _addressRepository.SaveChangesAsync(
            cancellationToken);

        return AddressOperationResult<CustomerAddressDto>.Success(
            Map(address));
    }

    private static void NormalizeDefaultForType(
        IReadOnlyCollection<CustomerAddress> addresses,
        AddressType addressType,
        Guid? preferredAddressId = null,
        Guid? avoidAddressId = null)
    {
        var matchingAddresses = addresses
            .Where(x => x.AddressType == addressType)
            .ToList();

        if (matchingAddresses.Count == 0)
        {
            return;
        }

        var selectedAddress =
            preferredAddressId.HasValue
                ? matchingAddresses.SingleOrDefault(
                    x => x.Id == preferredAddressId.Value)
                : null;

        selectedAddress ??= matchingAddresses
            .Where(x => x.Id != avoidAddressId)
            .OrderByDescending(x => x.IsDefault)
            .ThenByDescending(x => x.UpdatedAt)
            .ThenBy(x => x.CreatedAt)
            .FirstOrDefault();

        selectedAddress ??= matchingAddresses[0];

        foreach (var address in matchingAddresses)
        {
            address.IsDefault =
                address.Id == selectedAddress.Id;
        }
    }

    private static AddressOperationResult<SaveCustomerAddressModel>
        NormalizeAndValidate(
            SaveCustomerAddressModel model)
    {
        ArgumentNullException.ThrowIfNull(model);

        if (!Enum.IsDefined(model.AddressType))
        {
            return ValidationFailure(
                "Address type must be either shipping or billing.");
        }

        var fullName = model.FullName.Trim();
        var phone = model.Phone.Trim();
        var addressLine1 = model.AddressLine1.Trim();
        var addressLine2 = string.IsNullOrWhiteSpace(
            model.AddressLine2)
                ? null
                : model.AddressLine2.Trim();
        var city = model.City.Trim();
        var state = model.State.Trim();
        var postalCode = model.PostalCode.Trim();
        var countryCode = model.CountryCode
            .Trim()
            .ToUpperInvariant();

        if (fullName.Length is < 2 or > 200)
        {
            return ValidationFailure(
                "Full name must contain between 2 and 200 characters.");
        }

        if (phone.Length is < 7 or > 30)
        {
            return ValidationFailure(
                "Phone must contain between 7 and 30 characters.");
        }

        if (addressLine1.Length is < 3 or > 250)
        {
            return ValidationFailure(
                "Address line 1 must contain between 3 and 250 characters.");
        }

        if (addressLine2?.Length > 250)
        {
            return ValidationFailure(
                "Address line 2 cannot exceed 250 characters.");
        }

        if (city.Length is < 2 or > 100)
        {
            return ValidationFailure(
                "City must contain between 2 and 100 characters.");
        }

        if (state.Length is < 2 or > 100)
        {
            return ValidationFailure(
                "State must contain between 2 and 100 characters.");
        }

        if (postalCode.Length is < 3 or > 20)
        {
            return ValidationFailure(
                "Postal code must contain between 3 and 20 characters.");
        }

        if (countryCode.Length != 2 ||
            !countryCode.All(char.IsLetter))
        {
            return ValidationFailure(
                "Country code must be a valid two-letter ISO code.");
        }

        return AddressOperationResult<SaveCustomerAddressModel>.Success(
            new SaveCustomerAddressModel(
                model.AddressType,
                fullName,
                phone,
                addressLine1,
                addressLine2,
                city,
                state,
                postalCode,
                countryCode,
                model.IsDefault));
    }

    private static AddressOperationResult<SaveCustomerAddressModel>
        ValidationFailure(
            string message)
    {
        return AddressOperationResult<SaveCustomerAddressModel>.Failure(
            AddressErrorCode.ValidationFailed,
            message);
    }

    private static CustomerAddressDto Map(
        CustomerAddress address)
    {
        return new CustomerAddressDto(
            address.Id,
            address.AddressType
                .ToString()
                .ToLowerInvariant(),
            address.FullName,
            address.Phone,
            address.AddressLine1,
            address.AddressLine2,
            address.City,
            address.State,
            address.PostalCode,
            address.CountryCode.Trim(),
            address.IsDefault,
            address.CreatedAt,
            address.UpdatedAt);
    }
}
