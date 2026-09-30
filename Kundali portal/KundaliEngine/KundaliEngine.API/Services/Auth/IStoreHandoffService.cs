using KundaliEngine.API.DTOs.Auth;

namespace KundaliEngine.API.Services.Auth;

public interface IStoreHandoffService
{
    StoreHandoffResponse Issue(Guid userId);

    Task<AuthResponse> RedeemAsync(string code, CancellationToken cancellationToken);
}
