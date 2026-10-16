using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using KundaliEngine.API.DTOs.Billing;
using KundaliEngine.Domain.Entities;
using KundaliEngine.Domain.Subscriptions;
using KundaliEngine.Infrastructure.Data;
using KundaliEngine.API.Services.Dhruva;
using KundaliEngine.API.Services.Entitlements;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;

namespace KundaliEngine.API.Services.Billing;

public interface IBillingService
{
    Task<IReadOnlyList<BillingPlanDto>> ListPlansAsync(CancellationToken cancellationToken);

    Task<CheckoutSessionDto> CreateCheckoutAsync(User user, CreateCheckoutRequest request, CancellationToken cancellationToken);

    Task<BillingStatusDto> ActivateWithoutPaymentAsync(User user, CreateCheckoutRequest request, CancellationToken cancellationToken);

    Task<CouponPreviewDto> PreviewCouponAsync(CouponPreviewRequest request, CancellationToken cancellationToken);

    Task<BillingStatusDto> GetStatusAsync(User user, CancellationToken cancellationToken);

    Task CancelAsync(User user, CancellationToken cancellationToken);

    Task<RazorpayOrderCreatedDto> CreatePaymentOrderAsync(CreateRazorpayOrderRequest request, CancellationToken cancellationToken);

    Task<VerifyRazorpayPaymentResponse> VerifyPaymentAsync(User? user, VerifyRazorpayPaymentRequest request, CancellationToken cancellationToken);

    Task HandleWebhookAsync(string payload, string signature, CancellationToken cancellationToken);

    Task OverrideUserPlanAsync(User user, string planCode, DateTimeOffset? expiresAt, CancellationToken cancellationToken);
}

public sealed class BillingService : IBillingService
{
    private readonly KundaliDbContext _dbContext;
    private readonly IEntitlementService _entitlementService;
    private readonly IDhruvaUsageService _dhruvaUsage;
    private readonly IRazorpayClient _razorpayClient;
    private readonly IPlanCatalog _planCatalog;
    private readonly ICouponOfferService _couponOffers;
    private readonly SubscriptionOptions _subscriptionOptions;
    private readonly RazorpayOptions _razorpayOptions;
    private readonly ILogger<BillingService> _logger;

    public BillingService(
        KundaliDbContext dbContext,
        IEntitlementService entitlementService,
        IDhruvaUsageService dhruvaUsage,
        IRazorpayClient razorpayClient,
        IPlanCatalog planCatalog,
        ICouponOfferService couponOffers,
        IOptions<SubscriptionOptions> subscriptionOptions,
        IOptions<RazorpayOptions> razorpayOptions,
        ILogger<BillingService> logger)
    {
        _dbContext = dbContext;
        _entitlementService = entitlementService;
        _dhruvaUsage = dhruvaUsage;
        _razorpayClient = razorpayClient;
        _planCatalog = planCatalog;
        _couponOffers = couponOffers;
        _subscriptionOptions = subscriptionOptions.Value;
        _razorpayOptions = razorpayOptions.Value;
        _logger = logger;
    }

    public async Task<IReadOnlyList<BillingPlanDto>> ListPlansAsync(CancellationToken cancellationToken)
    {
        var plans = await _planCatalog.GetPlansAsync(cancellationToken);
        return plans
            .Select(ToPublicPlan)
            .ToList();
    }

    public async Task<CheckoutSessionDto> CreateCheckoutAsync(User user, CreateCheckoutRequest request, CancellationToken cancellationToken)
    {
        if (!PlanCodes.TryParse(request.PlanCode, out var planCode) || !PlanCodes.IsPaid(planCode))
        {
            throw new ArgumentException("Choose Premium or Premium Plus.");
        }

        var cycle = NormalizeCycle(request.BillingCycle);
        var catalog = await _planCatalog.FindAsync(planCode, cancellationToken)
            ?? throw new ArgumentException("That plan is not available.");
        if (!catalog.CheckoutOpen)
        {
            throw new ArgumentException("That plan does not use the payment gateway.");
        }

        if (!_razorpayClient.IsConfigured)
        {
            throw new InvalidOperationException("Checkout is not configured yet.");
        }

        var amount = cycle == "yearly" ? catalog.YearlyAmountPaise : catalog.MonthlyAmountPaise;
        if (amount < 100)
        {
            throw new ArgumentException("Amount must be at least 100 paise.");
        }

        CouponQuote? quote = null;
        if (!string.IsNullOrWhiteSpace(request.CouponCode))
        {
            quote = await _couponOffers.QuoteAsync(request.CouponCode, planCode, cycle, amount, cancellationToken);
            amount = quote.AmountPaise;
        }

        var razorpayPlanId = quote == null
            ? cycle == "yearly" ? catalog.RazorpayYearlyPlanId : catalog.RazorpayMonthlyPlanId
            : null;
        var notes = new Dictionary<string, string>
        {
            ["userId"] = user.Id.ToString(),
            ["planCode"] = planCode,
            ["billingCycle"] = cycle
        };
        if (quote != null)
        {
            notes["couponCode"] = quote.Code;
        }

        var subscription = new UserSubscription
        {
            Id = Guid.NewGuid(),
            UserId = user.Id,
            PlanCode = planCode,
            BillingCycle = cycle,
            Status = "created",
            CreatedAt = DateTime.UtcNow,
            CouponOfferId = quote?.CouponOfferId,
            CouponCode = quote?.Code
        };

        string? subscriptionId = null;
        string? orderId = null;
        if (!string.IsNullOrWhiteSpace(razorpayPlanId))
        {
            var created = await _razorpayClient.CreateSubscriptionAsync(razorpayPlanId, notes, cancellationToken);
            subscriptionId = created.Id;
            subscription.RazorpaySubscriptionId = created.Id;
            subscription.Status = "created";
        }
        else
        {
            var order = await _razorpayClient.CreateOrderAsync(amount, _subscriptionOptions.Currency, notes, cancellationToken);
            orderId = order.Id;
            subscription.RazorpayOrderId = order.Id;
            subscription.Status = "created";
        }

        _dbContext.UserSubscriptions.Add(subscription);
        await _dbContext.SaveChangesAsync(cancellationToken);

        return new CheckoutSessionDto
        {
            KeyId = _razorpayClient.KeyId,
            PlanCode = planCode,
            PlanName = catalog.Name,
            BillingCycle = cycle,
            Currency = _subscriptionOptions.Currency,
            AmountPaise = amount,
            SubscriptionId = subscriptionId,
            OrderId = orderId,
            Description = quote == null
                ? $"{catalog.Name} {cycle} plan"
                : $"{catalog.Name} {cycle} plan · {quote.PercentOff}% off"
        };
    }

    public async Task<BillingStatusDto> ActivateWithoutPaymentAsync(User user, CreateCheckoutRequest request, CancellationToken cancellationToken)
    {
        if (!PlanCodes.TryParse(request.PlanCode, out var planCode) || !PlanCodes.IsPaid(planCode))
        {
            throw new ArgumentException("Choose Premium or Premium Plus.");
        }

        var cycle = NormalizeCycle(request.BillingCycle);
        var catalog = await _planCatalog.FindAsync(planCode, cancellationToken)
            ?? throw new ArgumentException("That plan is not available.");
        if (catalog.CheckoutOpen && _razorpayClient.IsConfigured)
        {
            throw new ArgumentException("That plan uses the payment gateway.");
        }

        var expiresAt = DateTimeOffset.UtcNow.AddMonths(cycle == "yearly" ? 12 : 1);
        var subscription = new UserSubscription
        {
            Id = Guid.NewGuid(),
            UserId = user.Id,
            PlanCode = planCode,
            BillingCycle = cycle,
            Status = "active",
            CurrentPeriodEnd = expiresAt,
            CreatedAt = DateTime.UtcNow,
            UpdatedAt = DateTime.UtcNow
        };
        _dbContext.UserSubscriptions.Add(subscription);
        await SupersedeOtherSubscriptionsAsync(user, subscription, cancellationToken);
        _entitlementService.ApplyPlan(user, planCode, expiresAt);
        await _dbContext.SaveChangesAsync(cancellationToken);
        return await GetStatusAsync(user, cancellationToken);
    }

    public async Task<CouponPreviewDto> PreviewCouponAsync(CouponPreviewRequest request, CancellationToken cancellationToken)
    {
        if (!PlanCodes.TryParse(request.PlanCode, out var planCode) || !PlanCodes.IsPaid(planCode))
        {
            throw new ArgumentException("Choose Premium or Premium Plus.");
        }

        var cycle = NormalizeCycle(request.BillingCycle);
        var catalog = await _planCatalog.FindAsync(planCode, cancellationToken)
            ?? throw new ArgumentException("That plan is not available.");
        if (!catalog.CheckoutOpen)
        {
            throw new ArgumentException("That plan is not open for checkout.");
        }

        var amount = cycle == "yearly" ? catalog.YearlyAmountPaise : catalog.MonthlyAmountPaise;
        var quote = await _couponOffers.QuoteAsync(request.Code, planCode, cycle, amount, cancellationToken);
        return new CouponPreviewDto
        {
            Code = quote.Code,
            Name = quote.Name,
            PercentOff = quote.PercentOff,
            PlanCode = planCode,
            BillingCycle = cycle,
            OriginalAmountPaise = quote.OriginalAmountPaise,
            AmountPaise = quote.AmountPaise
        };
    }

    public async Task<BillingStatusDto> GetStatusAsync(User user, CancellationToken cancellationToken)
    {
        var snapshot = _entitlementService.GetSnapshot(user);
        var allowance = await _dhruvaUsage.GetAllowanceAsync(user, cancellationToken);
        var latest = await GetEffectiveSubscriptionAsync(user.Id, cancellationToken);

        return new BillingStatusDto
        {
            PlanCode = snapshot.PlanCode,
            IsPaid = snapshot.IsPaid,
            IsPlus = snapshot.IsPlus,
            PlanExpiresAt = snapshot.PlanExpiresAt,
            SubscriptionStatus = latest?.Status,
            BillingCycle = latest?.BillingCycle,
            CanCancel = snapshot.IsPaid && !string.IsNullOrWhiteSpace(latest?.RazorpaySubscriptionId)
                && !string.Equals(latest.Status, "cancelled", StringComparison.OrdinalIgnoreCase)
                && !string.Equals(latest.Status, "superseded", StringComparison.OrdinalIgnoreCase),
            DhruvaAiEnabled = allowance.DhruvaAiEnabled,
            LifeQuestionsRemaining = allowance.LifeQuestionsRemaining
        };
    }

    public async Task OverrideUserPlanAsync(User user, string planCode, DateTimeOffset? expiresAt, CancellationToken cancellationToken)
    {
        var normalized = PlanCodes.Normalize(planCode);
        DateTimeOffset? paidExpiresAt = null;
        if (normalized != PlanCodes.Free)
        {
            paidExpiresAt = expiresAt is { } expiry && expiry > DateTimeOffset.UtcNow
                ? expiry
                : DateTimeOffset.UtcNow.AddYears(1);
        }

        var existing = await _dbContext.UserSubscriptions
            .Where(item => item.UserId == user.Id)
            .ToListAsync(cancellationToken);

        foreach (var record in existing)
        {
            if (!string.IsNullOrWhiteSpace(record.RazorpaySubscriptionId)
                && !string.Equals(record.Status, "cancelled", StringComparison.OrdinalIgnoreCase)
                && !string.Equals(record.Status, "superseded", StringComparison.OrdinalIgnoreCase))
            {
                try
                {
                    await _razorpayClient.CancelSubscriptionAsync(record.RazorpaySubscriptionId, true, cancellationToken);
                }
                catch (Exception ex)
                {
                    _logger.LogWarning(ex, "Could not cancel Razorpay subscription {SubscriptionId} while admin overrode the plan for {UserId}.", record.RazorpaySubscriptionId, user.Id);
                }
            }

            if (!string.Equals(record.Status, "superseded", StringComparison.OrdinalIgnoreCase))
            {
                record.Status = "superseded";
                record.UpdatedAt = DateTime.UtcNow;
            }
        }

        _dbContext.UserSubscriptions.Add(new UserSubscription
        {
            Id = Guid.NewGuid(),
            UserId = user.Id,
            PlanCode = normalized,
            BillingCycle = "admin",
            Status = normalized == PlanCodes.Free ? "admin-free" : "admin-active",
            CurrentPeriodEnd = paidExpiresAt,
            CreatedAt = DateTime.UtcNow,
            UpdatedAt = DateTime.UtcNow
        });

        _entitlementService.ApplyPlan(user, normalized, paidExpiresAt);
        await _dbContext.SaveChangesAsync(cancellationToken);
    }

    public async Task CancelAsync(User user, CancellationToken cancellationToken)
    {
        var latest = await _dbContext.UserSubscriptions
            .Where(item => item.UserId == user.Id && item.RazorpaySubscriptionId != null)
            .OrderByDescending(item => item.CreatedAt)
            .FirstOrDefaultAsync(cancellationToken);

        if (latest?.RazorpaySubscriptionId != null
            && !string.Equals(latest.Status, "cancelled", StringComparison.OrdinalIgnoreCase))
        {
            await _razorpayClient.CancelSubscriptionAsync(latest.RazorpaySubscriptionId, true, cancellationToken);
            latest.Status = "cancelling";
            latest.UpdatedAt = DateTime.UtcNow;
            await _dbContext.SaveChangesAsync(cancellationToken);
        }
    }

    public async Task<RazorpayOrderCreatedDto> CreatePaymentOrderAsync(
        CreateRazorpayOrderRequest request,
        CancellationToken cancellationToken)
    {
        if (!_razorpayClient.IsConfigured)
        {
            throw new InvalidOperationException("Razorpay is not configured.");
        }

        if (request.Amount < 100)
        {
            throw new ArgumentException("Amount must be at least 100 paise.");
        }

        var order = await _razorpayClient.CreateOrderAsync(
            request.Amount,
            string.IsNullOrWhiteSpace(request.Currency) ? "INR" : request.Currency.Trim(),
            new Dictionary<string, string>(),
            cancellationToken,
            request.Receipt);

        return new RazorpayOrderCreatedDto
        {
            OrderId = order.Id,
            Amount = order.Amount > 0 ? order.Amount : request.Amount,
            Currency = string.IsNullOrWhiteSpace(order.Currency) ? (request.Currency ?? "INR") : order.Currency
        };
    }

    public async Task<VerifyRazorpayPaymentResponse> VerifyPaymentAsync(
        User? user,
        VerifyRazorpayPaymentRequest request,
        CancellationToken cancellationToken)
    {
        if (string.IsNullOrWhiteSpace(request.OrderId)
            || string.IsNullOrWhiteSpace(request.PaymentId)
            || string.IsNullOrWhiteSpace(request.Signature))
        {
            throw new ArgumentException("razorpay_order_id, razorpay_payment_id, and razorpay_signature are required.");
        }

        if (string.IsNullOrWhiteSpace(_razorpayOptions.KeySecret))
        {
            throw new InvalidOperationException("Razorpay is not configured.");
        }

        var payload = $"{request.OrderId}|{request.PaymentId}";
        if (!IsValidSignature(payload, request.Signature, _razorpayOptions.KeySecret))
        {
            throw new InvalidOperationException("Payment signature mismatch.");
        }

        var activated = false;
        string? planCode = null;
        if (user != null)
        {
            var record = await _dbContext.UserSubscriptions
                .FirstOrDefaultAsync(item => item.RazorpayOrderId == request.OrderId && item.UserId == user.Id, cancellationToken);

            if (record != null)
            {
                if (await HasNewerAdminOverrideAsync(user.Id, record, cancellationToken))
                {
                    record.RazorpayPaymentId = request.PaymentId;
                    record.Status = "superseded";
                    record.UpdatedAt = DateTime.UtcNow;
                }
                else
                {
                    await ActivatePaidOrderAsync(user, record, request.PaymentId, cancellationToken);
                    activated = true;
                    planCode = record.PlanCode;
                }

                await _dbContext.SaveChangesAsync(cancellationToken);
            }
        }

        return new VerifyRazorpayPaymentResponse
        {
            Verified = true,
            PlanActivated = activated,
            PlanCode = planCode,
            Message = activated ? "Payment verified and plan activated." : "Payment verified."
        };
    }

    public async Task HandleWebhookAsync(string payload, string signature, CancellationToken cancellationToken)
    {
        if (string.IsNullOrWhiteSpace(_razorpayOptions.WebhookSecret))
        {
            throw new InvalidOperationException("Razorpay webhook secret is not configured.");
        }

        if (!IsValidSignature(payload, signature, _razorpayOptions.WebhookSecret))
        {
            throw new UnauthorizedAccessException("Invalid Razorpay signature.");
        }

        using var document = JsonDocument.Parse(payload);
        var root = document.RootElement;
        var eventName = root.GetProperty("event").GetString() ?? string.Empty;
        var payloadElement = root.GetProperty("payload");

        if (eventName.StartsWith("subscription.", StringComparison.OrdinalIgnoreCase)
            && payloadElement.TryGetProperty("subscription", out var subscriptionWrapper)
            && subscriptionWrapper.TryGetProperty("entity", out var subscriptionEntity))
        {
            await HandleSubscriptionEventAsync(eventName, subscriptionEntity, payloadElement, cancellationToken);
            return;
        }

        if (eventName.Equals("payment.captured", StringComparison.OrdinalIgnoreCase)
            && payloadElement.TryGetProperty("payment", out var paymentWrapper)
            && paymentWrapper.TryGetProperty("entity", out var paymentEntity))
        {
            await HandlePaymentCapturedAsync(paymentEntity, cancellationToken);
        }
    }

    private async Task HandleSubscriptionEventAsync(
        string eventName,
        JsonElement subscriptionEntity,
        JsonElement payloadElement,
        CancellationToken cancellationToken)
    {
        var razorpaySubscriptionId = subscriptionEntity.GetProperty("id").GetString();
        if (string.IsNullOrWhiteSpace(razorpaySubscriptionId))
        {
            return;
        }

        var notes = ReadNotes(subscriptionEntity);
        var user = await ResolveUserAsync(notes, cancellationToken);
        if (user == null)
        {
            _logger.LogWarning("Razorpay subscription {SubscriptionId} had no matching user.", razorpaySubscriptionId);
            return;
        }

        var record = await _dbContext.UserSubscriptions
            .FirstOrDefaultAsync(item => item.RazorpaySubscriptionId == razorpaySubscriptionId, cancellationToken)
            ?? await CreateRecordFromNotesAsync(user, notes, razorpaySubscriptionId, null, cancellationToken);

        var status = subscriptionEntity.TryGetProperty("status", out var statusElement)
            ? statusElement.GetString() ?? record.Status
            : record.Status;
        record.Status = status;
        record.UpdatedAt = DateTime.UtcNow;

        if (payloadElement.TryGetProperty("payment", out var paymentWrapper)
            && paymentWrapper.TryGetProperty("entity", out var paymentEntity)
            && paymentEntity.TryGetProperty("id", out var paymentId))
        {
            record.RazorpayPaymentId = paymentId.GetString();
        }

        var periodEnd = ReadUnixTimestamp(subscriptionEntity, "current_end")
            ?? ReadUnixTimestamp(subscriptionEntity, "charge_at");

        if (eventName is "subscription.activated" or "subscription.charged" or "subscription.authenticated")
        {
            var planCode = notes.GetValueOrDefault("planCode") ?? record.PlanCode;
            var expiresAt = periodEnd ?? DateTimeOffset.UtcNow.AddMonths(record.BillingCycle == "yearly" ? 12 : 1);
            record.CurrentPeriodEnd = expiresAt;
            await SupersedeOtherSubscriptionsAsync(user, record, cancellationToken);
            if (!await HasNewerAdminOverrideAsync(user.Id, record, cancellationToken))
            {
                _entitlementService.ApplyPlan(user, planCode, expiresAt);
            }
        }
        else if (eventName is "subscription.cancelled" or "subscription.completed" or "subscription.halted")
        {
            record.Status = eventName.Replace("subscription.", string.Empty, StringComparison.OrdinalIgnoreCase);
            if (!await HasNewerAdminOverrideAsync(user.Id, record, cancellationToken)
                && !await HasOtherActivePaidSubscriptionAsync(user.Id, record.Id, cancellationToken))
            {
                _entitlementService.ApplyPlan(user, PlanCodes.Free, null);
            }
        }

        await _dbContext.SaveChangesAsync(cancellationToken);
    }

    private async Task HandlePaymentCapturedAsync(JsonElement paymentEntity, CancellationToken cancellationToken)
    {
        var orderId = paymentEntity.TryGetProperty("order_id", out var orderIdElement)
            ? orderIdElement.GetString()
            : null;
        var paymentId = paymentEntity.TryGetProperty("id", out var paymentIdElement)
            ? paymentIdElement.GetString()
            : null;
        var notes = ReadNotes(paymentEntity);
        var user = await ResolveUserAsync(notes, cancellationToken);
        UserSubscription? record = null;

        if (!string.IsNullOrWhiteSpace(orderId))
        {
            record = await _dbContext.UserSubscriptions
                .FirstOrDefaultAsync(item => item.RazorpayOrderId == orderId, cancellationToken);
        }

        if (record == null && user != null)
        {
            record = await CreateRecordFromNotesAsync(user, notes, null, orderId, cancellationToken);
        }

        if (record == null)
        {
            return;
        }

        user ??= await _dbContext.Users.FirstOrDefaultAsync(item => item.Id == record.UserId, cancellationToken);
        if (user == null)
        {
            return;
        }

        record.RazorpayPaymentId = paymentId;
        record.Status = "active";
        record.UpdatedAt = DateTime.UtcNow;
        var expiresAt = DateTimeOffset.UtcNow.AddMonths(record.BillingCycle == "yearly" ? 12 : 1);
        record.CurrentPeriodEnd = expiresAt;
        await SupersedeOtherSubscriptionsAsync(user, record, cancellationToken);
        if (!await HasNewerAdminOverrideAsync(user.Id, record, cancellationToken))
        {
            _entitlementService.ApplyPlan(user, record.PlanCode, expiresAt);
        }

        await _couponOffers.RedeemAsync(record, cancellationToken);
        await _dbContext.SaveChangesAsync(cancellationToken);
    }

    private async Task ActivatePaidOrderAsync(User user, UserSubscription record, string? paymentId, CancellationToken cancellationToken)
    {
        record.RazorpayPaymentId = paymentId;
        record.Status = "active";
        record.UpdatedAt = DateTime.UtcNow;
        var expiresAt = DateTimeOffset.UtcNow.AddMonths(record.BillingCycle == "yearly" ? 12 : 1);
        record.CurrentPeriodEnd = expiresAt;
        await SupersedeOtherSubscriptionsAsync(user, record, cancellationToken);
        _entitlementService.ApplyPlan(user, record.PlanCode, expiresAt);
        await _couponOffers.RedeemAsync(record, cancellationToken);
    }

    private async Task<UserSubscription?> GetEffectiveSubscriptionAsync(Guid userId, CancellationToken cancellationToken)
    {
        var records = await _dbContext.UserSubscriptions
            .AsNoTracking()
            .Where(item => item.UserId == userId)
            .OrderByDescending(item => item.CreatedAt)
            .ToListAsync(cancellationToken);

        return records.FirstOrDefault(IsEffectiveSubscription);
    }

    private async Task SupersedeOtherSubscriptionsAsync(User user, UserSubscription keep, CancellationToken cancellationToken)
    {
        var existing = await _dbContext.UserSubscriptions
            .Where(item => item.UserId == user.Id && item.Id != keep.Id)
            .ToListAsync(cancellationToken);

        foreach (var record in existing)
        {
            if (string.Equals(record.Status, "superseded", StringComparison.OrdinalIgnoreCase)
                || string.Equals(record.Status, "cancelled", StringComparison.OrdinalIgnoreCase)
                || string.Equals(record.Status, "completed", StringComparison.OrdinalIgnoreCase))
            {
                continue;
            }

            if (!string.IsNullOrWhiteSpace(record.RazorpaySubscriptionId))
            {
                try
                {
                    await _razorpayClient.CancelSubscriptionAsync(record.RazorpaySubscriptionId, true, cancellationToken);
                }
                catch (Exception ex)
                {
                    _logger.LogWarning(
                        ex,
                        "Could not cancel Razorpay subscription {SubscriptionId} while switching plan for {UserId}.",
                        record.RazorpaySubscriptionId,
                        user.Id);
                }
            }

            record.Status = "superseded";
            record.UpdatedAt = DateTime.UtcNow;
        }
    }

    private async Task<bool> HasOtherActivePaidSubscriptionAsync(Guid userId, Guid exceptId, CancellationToken cancellationToken)
    {
        var records = await _dbContext.UserSubscriptions
            .AsNoTracking()
            .Where(item => item.UserId == userId && item.Id != exceptId)
            .ToListAsync(cancellationToken);

        return records.Any(IsEffectiveSubscription);
    }

    private static bool IsEffectiveSubscription(UserSubscription record)
    {
        var status = (record.Status ?? string.Empty).Trim().ToLowerInvariant();
        return status is "active" or "authenticated" or "charged" or "admin-active" or "cancelling";
    }

    private async Task<bool> HasNewerAdminOverrideAsync(Guid userId, UserSubscription triggeringRecord, CancellationToken cancellationToken)
    {
        var latestAdmin = await _dbContext.UserSubscriptions
            .AsNoTracking()
            .Where(item => item.UserId == userId && item.BillingCycle == "admin")
            .OrderByDescending(item => item.CreatedAt)
            .FirstOrDefaultAsync(cancellationToken);

        return latestAdmin != null && latestAdmin.CreatedAt >= triggeringRecord.CreatedAt;
    }

    private async Task<UserSubscription> CreateRecordFromNotesAsync(
        User user,
        IReadOnlyDictionary<string, string> notes,
        string? subscriptionId,
        string? orderId,
        CancellationToken cancellationToken)
    {
        var planCode = PlanCodes.Normalize(notes.GetValueOrDefault("planCode"));
        var record = new UserSubscription
        {
            Id = Guid.NewGuid(),
            UserId = user.Id,
            PlanCode = planCode,
            BillingCycle = NormalizeCycle(notes.GetValueOrDefault("billingCycle")),
            Status = "created",
            RazorpaySubscriptionId = subscriptionId,
            RazorpayOrderId = orderId,
            CreatedAt = DateTime.UtcNow
        };
        _dbContext.UserSubscriptions.Add(record);
        await _dbContext.SaveChangesAsync(cancellationToken);
        return record;
    }

    private async Task<User?> ResolveUserAsync(IReadOnlyDictionary<string, string> notes, CancellationToken cancellationToken)
    {
        if (!notes.TryGetValue("userId", out var userIdValue) || !Guid.TryParse(userIdValue, out var userId))
        {
            return null;
        }

        return await _dbContext.Users.FirstOrDefaultAsync(item => item.Id == userId, cancellationToken);
    }

    private BillingPlanDto ToPublicPlan(BillingPlanConfig plan)
    {
        return new BillingPlanDto
        {
            Code = plan.Code,
            Name = plan.Name,
            Tagline = plan.Tagline,
            Currency = _subscriptionOptions.Currency,
            MonthlyAmountPaise = plan.MonthlyAmountPaise,
            YearlyAmountPaise = plan.YearlyAmountPaise,
            KundaliSaveLimit = plan.KundaliSaveLimit,
            MatchSaveLimit = plan.MatchSaveLimit,
            CanCheckout = plan.CheckoutOpen && PlanCodes.IsPaid(plan.Code) && _razorpayClient.IsConfigured
        };
    }

    private static string NormalizeCycle(string? cycle)
    {
        return string.Equals(cycle, "yearly", StringComparison.OrdinalIgnoreCase) ? "yearly" : "monthly";
    }

    private static Dictionary<string, string> ReadNotes(JsonElement entity)
    {
        var notes = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
        if (!entity.TryGetProperty("notes", out var notesElement) || notesElement.ValueKind != JsonValueKind.Object)
        {
            return notes;
        }

        foreach (var property in notesElement.EnumerateObject())
        {
            notes[property.Name] = property.Value.ValueKind == JsonValueKind.String
                ? property.Value.GetString() ?? string.Empty
                : property.Value.ToString();
        }

        return notes;
    }

    private static DateTimeOffset? ReadUnixTimestamp(JsonElement entity, string propertyName)
    {
        if (!entity.TryGetProperty(propertyName, out var value) || value.ValueKind != JsonValueKind.Number)
        {
            return null;
        }

        return DateTimeOffset.FromUnixTimeSeconds(value.GetInt64());
    }

    private static bool IsValidSignature(string payload, string signature, string secret)
    {
        if (string.IsNullOrWhiteSpace(signature))
        {
            return false;
        }

        using var hmac = new HMACSHA256(Encoding.UTF8.GetBytes(secret));
        var hash = hmac.ComputeHash(Encoding.UTF8.GetBytes(payload));
        var computed = Convert.ToHexString(hash).ToLowerInvariant();
        return CryptographicOperations.FixedTimeEquals(
            Encoding.UTF8.GetBytes(computed),
            Encoding.UTF8.GetBytes(signature.ToLowerInvariant()));
    }
}
