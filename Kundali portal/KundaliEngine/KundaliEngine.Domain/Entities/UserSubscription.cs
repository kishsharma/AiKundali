namespace KundaliEngine.Domain.Entities;

public class UserSubscription
{
    public Guid Id { get; set; }

    public Guid UserId { get; set; }

    public User User { get; set; } = null!;

    public string PlanCode { get; set; } = "Free";

    public string BillingCycle { get; set; } = "monthly";

    public string Status { get; set; } = "created";

    public string? RazorpaySubscriptionId { get; set; }

    public string? RazorpayOrderId { get; set; }

    public string? RazorpayPaymentId { get; set; }

    public Guid? CouponOfferId { get; set; }

    public string? CouponCode { get; set; }

    public DateTimeOffset? CurrentPeriodEnd { get; set; }

    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

    public DateTime? UpdatedAt { get; set; }
}
