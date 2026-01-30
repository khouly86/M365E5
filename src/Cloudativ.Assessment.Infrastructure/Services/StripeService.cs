using Cloudativ.Assessment.Application.DTOs;
using Cloudativ.Assessment.Application.Interfaces;
using Cloudativ.Assessment.Domain.Enums;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using Stripe;
using Stripe.Checkout;

namespace Cloudativ.Assessment.Infrastructure.Services;

public class StripeService : IStripeService
{
    private readonly ILogger<StripeService> _logger;
    private readonly string _webhookSecret;
    private readonly string _monthlyPriceId;
    private readonly string _yearlyPriceId;

    public StripeService(IConfiguration configuration, ILogger<StripeService> logger)
    {
        _logger = logger;

        var stripeSecretKey = configuration["Stripe:SecretKey"];
        if (!string.IsNullOrEmpty(stripeSecretKey))
        {
            StripeConfiguration.ApiKey = stripeSecretKey;
        }

        _webhookSecret = configuration["Stripe:WebhookSecret"] ?? string.Empty;
        _monthlyPriceId = configuration["Stripe:MonthlyPriceId"] ?? string.Empty;
        _yearlyPriceId = configuration["Stripe:YearlyPriceId"] ?? string.Empty;
    }

    public async Task<string> CreateCustomerAsync(Guid tenantId, string tenantName, string? email, CancellationToken ct = default)
    {
        var customerService = new CustomerService();

        var options = new CustomerCreateOptions
        {
            Name = tenantName,
            Email = email,
            Metadata = new Dictionary<string, string>
            {
                { "tenant_id", tenantId.ToString() }
            }
        };

        var customer = await customerService.CreateAsync(options, cancellationToken: ct);
        _logger.LogInformation("Created Stripe customer {CustomerId} for tenant {TenantId}", customer.Id, tenantId);

        return customer.Id;
    }

    public async Task<CheckoutSessionResult> CreateCheckoutSessionAsync(
        string customerId,
        SubscriptionPlan plan,
        string successUrl,
        string cancelUrl,
        CancellationToken ct = default)
    {
        try
        {
            var priceId = GetPriceIdForPlan(plan);
            if (string.IsNullOrEmpty(priceId))
            {
                return new CheckoutSessionResult
                {
                    Success = false,
                    ErrorMessage = "Price ID not configured for this plan"
                };
            }

            var sessionService = new SessionService();

            var options = new SessionCreateOptions
            {
                Customer = customerId,
                PaymentMethodTypes = new List<string> { "card" },
                LineItems = new List<SessionLineItemOptions>
                {
                    new SessionLineItemOptions
                    {
                        Price = priceId,
                        Quantity = 1
                    }
                },
                Mode = "subscription",
                SuccessUrl = successUrl,
                CancelUrl = cancelUrl
            };

            var session = await sessionService.CreateAsync(options, cancellationToken: ct);

            _logger.LogInformation("Created Stripe checkout session {SessionId} for customer {CustomerId}", session.Id, customerId);

            return new CheckoutSessionResult
            {
                Success = true,
                SessionId = session.Id,
                SessionUrl = session.Url
            };
        }
        catch (StripeException ex)
        {
            _logger.LogError(ex, "Failed to create Stripe checkout session for customer {CustomerId}", customerId);
            return new CheckoutSessionResult
            {
                Success = false,
                ErrorMessage = ex.Message
            };
        }
    }

    public async Task<string?> GetSubscriptionStatusAsync(string subscriptionId, CancellationToken ct = default)
    {
        try
        {
            var subscriptionService = new Stripe.SubscriptionService();
            var subscription = await subscriptionService.GetAsync(subscriptionId, cancellationToken: ct);
            return subscription.Status;
        }
        catch (StripeException ex)
        {
            _logger.LogError(ex, "Failed to get Stripe subscription status for {SubscriptionId}", subscriptionId);
            return null;
        }
    }

    public async Task CancelSubscriptionAsync(string subscriptionId, CancellationToken ct = default)
    {
        var subscriptionService = new Stripe.SubscriptionService();
        await subscriptionService.CancelAsync(subscriptionId, cancellationToken: ct);
        _logger.LogInformation("Cancelled Stripe subscription {SubscriptionId}", subscriptionId);
    }

    public Task<(bool IsValid, string? EventType, string? SubscriptionId, string? CustomerId, string? Status)> ValidateWebhookAsync(
        string payload,
        string signature,
        CancellationToken ct = default)
    {
        try
        {
            var stripeEvent = EventUtility.ConstructEvent(payload, signature, _webhookSecret);

            string? subscriptionId = null;
            string? customerId = null;
            string? status = null;

            if (stripeEvent.Data.Object is Stripe.Subscription subscription)
            {
                subscriptionId = subscription.Id;
                customerId = subscription.CustomerId;
                status = subscription.Status;
            }
            else if (stripeEvent.Data.Object is Invoice invoice)
            {
                subscriptionId = invoice.SubscriptionId;
                customerId = invoice.CustomerId;
            }

            return Task.FromResult((true, stripeEvent.Type, subscriptionId, customerId, status));
        }
        catch (StripeException ex)
        {
            _logger.LogError(ex, "Failed to validate Stripe webhook");
            return Task.FromResult<(bool, string?, string?, string?, string?)>((false, null, null, null, null));
        }
    }

    public string GetPriceIdForPlan(SubscriptionPlan plan)
    {
        return plan switch
        {
            SubscriptionPlan.Monthly => _monthlyPriceId,
            SubscriptionPlan.Yearly => _yearlyPriceId,
            _ => string.Empty
        };
    }
}
