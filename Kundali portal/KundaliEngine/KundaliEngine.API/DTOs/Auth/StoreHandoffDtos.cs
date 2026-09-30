namespace KundaliEngine.API.DTOs.Auth;

public class StoreHandoffResponse
{
    public string Code { get; set; } = string.Empty;

    public DateTimeOffset ExpiresAt { get; set; }
}

public class StoreHandoffRedeemRequest
{
    public string Code { get; set; } = string.Empty;
}
