namespace KundaliEngine.Domain.Entities;

public class CouponOffer
{
    public Guid Id { get; set; }

    public string Name { get; set; } = string.Empty;

    public string Code { get; set; } = string.Empty;

    public int PercentOff { get; set; }

    public string PlanCodes { get; set; } = string.Empty;

    public string BillingCycles { get; set; } = string.Empty;

    public DateTimeOffset? StartsAt { get; set; }

    public DateTimeOffset? EndsAt { get; set; }

    public int? MaxRedemptions { get; set; }

    public int RedeemedCount { get; set; }

    public bool Active { get; set; } = true;

    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

    public DateTime? UpdatedAt { get; set; }
}
