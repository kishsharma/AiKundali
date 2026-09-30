namespace KundaliEngine.API.DTOs.Billing;

public sealed class BillingPlanDto
{
    public string Code { get; set; } = string.Empty;

    public string Name { get; set; } = string.Empty;

    public string Tagline { get; set; } = string.Empty;

    public string Currency { get; set; } = "INR";

    public int MonthlyAmountPaise { get; set; }

    public int YearlyAmountPaise { get; set; }

    public int KundaliSaveLimit { get; set; }

    public int MatchSaveLimit { get; set; }

    public bool CanCheckout { get; set; }
}

public sealed class CreateCheckoutRequest
{
    public string PlanCode { get; set; } = string.Empty;

    public string BillingCycle { get; set; } = "monthly";

    public string? CouponCode { get; set; }
}

public sealed class CouponPreviewRequest
{
    public string Code { get; set; } = string.Empty;

    public string PlanCode { get; set; } = string.Empty;

    public string BillingCycle { get; set; } = "monthly";
}

public sealed class CouponPreviewDto
{
    public string Code { get; set; } = string.Empty;

    public string Name { get; set; } = string.Empty;

    public int PercentOff { get; set; }

    public string PlanCode { get; set; } = string.Empty;

    public string BillingCycle { get; set; } = "monthly";

    public int OriginalAmountPaise { get; set; }

    public int AmountPaise { get; set; }
}

public sealed class CheckoutSessionDto
{
    public string KeyId { get; set; } = string.Empty;

    public string PlanCode { get; set; } = string.Empty;

    public string PlanName { get; set; } = string.Empty;

    public string BillingCycle { get; set; } = "monthly";

    public string Currency { get; set; } = "INR";

    public int AmountPaise { get; set; }

    public string? SubscriptionId { get; set; }

    public string? OrderId { get; set; }

    public string Description { get; set; } = string.Empty;
}

public sealed class BillingStatusDto
{
    public string PlanCode { get; set; } = "Free";

    public bool IsPaid { get; set; }

    public bool IsPlus { get; set; }

    public DateTimeOffset? PlanExpiresAt { get; set; }

    public string? SubscriptionStatus { get; set; }

    public string? BillingCycle { get; set; }

    public bool CanCancel { get; set; }

    public bool DhruvaAiEnabled { get; set; }

    public int? LifeQuestionsRemaining { get; set; }
}

public sealed class CreateRazorpayOrderRequest
{
    public int Amount { get; set; }

    public string Currency { get; set; } = "INR";

    public string? Receipt { get; set; }
}

public sealed class RazorpayOrderCreatedDto
{
    public string OrderId { get; set; } = string.Empty;

    public int Amount { get; set; }

    public string Currency { get; set; } = "INR";
}

public sealed class VerifyRazorpayPaymentRequest
{
    public string? RazorpayPaymentId { get; set; }

    public string? RazorpayOrderId { get; set; }

    public string? RazorpaySignature { get; set; }

    [System.Text.Json.Serialization.JsonPropertyName("razorpay_payment_id")]
    public string? RazorpayPaymentIdSnake { get; set; }

    [System.Text.Json.Serialization.JsonPropertyName("razorpay_order_id")]
    public string? RazorpayOrderIdSnake { get; set; }

    [System.Text.Json.Serialization.JsonPropertyName("razorpay_signature")]
    public string? RazorpaySignatureSnake { get; set; }

    public string PaymentId => FirstNonEmpty(RazorpayPaymentId, RazorpayPaymentIdSnake);

    public string OrderId => FirstNonEmpty(RazorpayOrderId, RazorpayOrderIdSnake);

    public string Signature => FirstNonEmpty(RazorpaySignature, RazorpaySignatureSnake);

    private static string FirstNonEmpty(params string?[] values)
    {
        return values.FirstOrDefault(value => !string.IsNullOrWhiteSpace(value))?.Trim() ?? string.Empty;
    }
}

public sealed class VerifyRazorpayPaymentResponse
{
    public bool Verified { get; set; }

    public bool PlanActivated { get; set; }

    public string? PlanCode { get; set; }

    public string Message { get; set; } = string.Empty;
}
