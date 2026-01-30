using Cloudativ.Assessment.Application.DTOs;
using Cloudativ.Assessment.Domain.Enums;

namespace Cloudativ.Assessment.Application.Interfaces;

public interface ISubscriptionService
{
    // Query
    Task<SubscriptionDto?> GetByTenantIdAsync(Guid tenantId, CancellationToken ct = default);
    Task<IReadOnlyList<SubscriptionDto>> GetAllAsync(CancellationToken ct = default);
    Task<SubscriptionUsageDto> GetUsageAsync(Guid tenantId, CancellationToken ct = default);
    Task<IReadOnlyList<SubscriptionPlanInfo>> GetAvailablePlansAsync(CancellationToken ct = default);

    // Subscription Creation
    Task<SubscriptionDto> CreateTrialAsync(Guid tenantId, CancellationToken ct = default);

    // Assessment Limits
    Task<bool> CanRunAssessmentAsync(Guid tenantId, CancellationToken ct = default);
    Task IncrementAssessmentCountAsync(Guid tenantId, CancellationToken ct = default);
    Task ResetMonthlyCountersAsync(CancellationToken ct = default);

    // Subscription Management
    Task<SubscriptionDto> UpgradeAsync(UpgradeSubscriptionDto dto, CancellationToken ct = default);
    Task<SubscriptionDto> CancelAsync(Guid tenantId, CancellationToken ct = default);
    Task CheckExpiredSubscriptionsAsync(CancellationToken ct = default);

    // Stripe Integration
    Task<CheckoutSessionResult> CreateCheckoutSessionAsync(Guid tenantId, SubscriptionPlan plan, string successUrl, string cancelUrl, CancellationToken ct = default);
    Task HandleStripeWebhookAsync(string payload, string signature, CancellationToken ct = default);
}
