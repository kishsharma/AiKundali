namespace KundaliEngine.Domain.Entities;

public class CouponRedemption
{
    public Guid Id { get; set; }

    public Guid CouponOfferId { get; set; }

    public Guid UserId { get; set; }

    public Guid? UserSubscriptionId { get; set; }

    public string? RazorpayOrderId { get; set; }

    public string PlanCode { get; set; } = string.Empty;

    public string BillingCycle { get; set; } = "monthly";

    public int PercentOff { get; set; }

    public int AmountPaise { get; set; }

    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
}
