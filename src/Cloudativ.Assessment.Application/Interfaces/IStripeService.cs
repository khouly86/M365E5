using Cloudativ.Assessment.Application.DTOs;
using Cloudativ.Assessment.Domain.Enums;

namespace Cloudativ.Assessment.Application.Interfaces;

public interface IStripeService
{
    Task<string> CreateCustomerAsync(Guid tenantId, string tenantName, string? email, CancellationToken ct = default);
    Task<CheckoutSessionResult> CreateCheckoutSessionAsync(string customerId, SubscriptionPlan plan, string successUrl, string cancelUrl, CancellationToken ct = default);
    Task<string?> GetSubscriptionStatusAsync(string subscriptionId, CancellationToken ct = default);
    Task CancelSubscriptionAsync(string subscriptionId, CancellationToken ct = default);
    Task<(bool IsValid, string? EventType, string? SubscriptionId, string? CustomerId, string? Status)> ValidateWebhookAsync(string payload, string signature, CancellationToken ct = default);
    string GetPriceIdForPlan(SubscriptionPlan plan);
}
