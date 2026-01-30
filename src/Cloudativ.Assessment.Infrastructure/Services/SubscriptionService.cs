using Cloudativ.Assessment.Application.DTOs;
using Cloudativ.Assessment.Application.Interfaces;
using Cloudativ.Assessment.Domain.Entities;
using Cloudativ.Assessment.Domain.Enums;
using Cloudativ.Assessment.Domain.Interfaces;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;

namespace Cloudativ.Assessment.Infrastructure.Services;

public class SubscriptionService : ISubscriptionService
{
    private readonly IUnitOfWork _unitOfWork;
    private readonly IStripeService _stripeService;
    private readonly ILogger<SubscriptionService> _logger;
    private readonly SubscriptionSettings _settings;

    public SubscriptionService(
        IUnitOfWork unitOfWork,
        IStripeService stripeService,
        IConfiguration configuration,
        ILogger<SubscriptionService> logger)
    {
        _unitOfWork = unitOfWork;
        _stripeService = stripeService;
        _logger = logger;

        _settings = new SubscriptionSettings
        {
            TrialDays = configuration.GetValue<int>("Subscription:TrialDays", 14),
            TrialAssessmentLimit = configuration.GetValue<int>("Subscription:TrialAssessmentLimit", 3),
            MonthlyAssessmentLimit = configuration.GetValue<int>("Subscription:MonthlyAssessmentLimit", 10),
            YearlyAssessmentLimit = configuration.GetValue<int>("Subscription:YearlyAssessmentLimit", -1),
            MonthlyPrice = configuration.GetValue<decimal>("Subscription:MonthlyPrice", 49.99m),
            YearlyPrice = configuration.GetValue<decimal>("Subscription:YearlyPrice", 499.99m)
        };
    }

    public async Task<SubscriptionDto?> GetByTenantIdAsync(Guid tenantId, CancellationToken ct = default)
    {
        var subscription = await _unitOfWork.Subscriptions.GetByTenantIdAsync(tenantId, ct);
        if (subscription == null)
            return null;

        var tenant = await _unitOfWork.Tenants.GetByIdAsync(tenantId, ct);
        return MapToDto(subscription, tenant?.Name ?? "Unknown");
    }

    public async Task<IReadOnlyList<SubscriptionDto>> GetAllAsync(CancellationToken ct = default)
    {
        var subscriptions = await _unitOfWork.Subscriptions.GetAllAsync(ct);
        var tenants = await _unitOfWork.Tenants.GetAllAsync(ct);
        var tenantDict = tenants.ToDictionary(t => t.Id, t => t.Name);

        return subscriptions.Select(s => MapToDto(s, tenantDict.GetValueOrDefault(s.TenantId, "Unknown"))).ToList();
    }

    public async Task<SubscriptionUsageDto> GetUsageAsync(Guid tenantId, CancellationToken ct = default)
    {
        var subscription = await _unitOfWork.Subscriptions.GetByTenantIdAsync(tenantId, ct);
        if (subscription == null)
        {
            return new SubscriptionUsageDto
            {
                AssessmentsUsed = 0,
                AssessmentsLimit = 0,
                AssessmentsRemaining = 0,
                UsagePercentage = 100,
                CanRunAssessment = false,
                LimitReachedMessage = "No active subscription. Please subscribe to run assessments."
            };
        }

        var limit = subscription.MonthlyAssessmentLimit;
        var used = subscription.AssessmentsUsedThisMonth;
        var remaining = limit == -1 ? int.MaxValue : Math.Max(0, limit - used);
        var usagePercentage = limit == -1 ? 0 : (limit > 0 ? (double)used / limit * 100 : 100);
        var canRun = await CanRunAssessmentAsync(tenantId, ct);

        string? limitMessage = null;
        if (!canRun)
        {
            if (subscription.Status == SubscriptionStatus.Expired)
                limitMessage = "Your subscription has expired. Please renew to continue running assessments.";
            else if (subscription.Status == SubscriptionStatus.Cancelled)
                limitMessage = "Your subscription has been cancelled. Please subscribe to run assessments.";
            else if (limit != -1 && used >= limit)
                limitMessage = $"You've reached your monthly limit of {limit} assessments. Upgrade your plan for more assessments.";
        }

        return new SubscriptionUsageDto
        {
            AssessmentsUsed = used,
            AssessmentsLimit = limit,
            AssessmentsRemaining = limit == -1 ? -1 : remaining,
            UsagePercentage = usagePercentage,
            PeriodStart = subscription.CurrentPeriodStart,
            PeriodEnd = subscription.CurrentPeriodStart.AddMonths(1),
            CanRunAssessment = canRun,
            LimitReachedMessage = limitMessage
        };
    }

    public Task<IReadOnlyList<SubscriptionPlanInfo>> GetAvailablePlansAsync(CancellationToken ct = default)
    {
        var plans = new List<SubscriptionPlanInfo>
        {
            new SubscriptionPlanInfo
            {
                Plan = SubscriptionPlan.Trial,
                Name = "Free Trial",
                Description = "Try our full features for 14 days",
                Price = 0,
                PriceDisplay = "Free",
                BillingPeriod = "14 days",
                AssessmentsPerMonth = _settings.TrialAssessmentLimit,
                AssessmentsDisplay = $"{_settings.TrialAssessmentLimit} assessments",
                Features = new List<string>
                {
                    "All 9 assessment domains",
                    "Full security scoring",
                    "PDF/HTML reports",
                    "Email notifications"
                },
                IsRecommended = false
            },
            new SubscriptionPlanInfo
            {
                Plan = SubscriptionPlan.Monthly,
                Name = "Monthly",
                Description = "Perfect for growing teams",
                Price = _settings.MonthlyPrice,
                PriceDisplay = $"${_settings.MonthlyPrice}/mo",
                BillingPeriod = "Monthly",
                AssessmentsPerMonth = _settings.MonthlyAssessmentLimit,
                AssessmentsDisplay = $"{_settings.MonthlyAssessmentLimit} assessments/month",
                Features = new List<string>
                {
                    "All 9 assessment domains",
                    "Full security scoring",
                    "PDF/HTML reports",
                    "Email notifications",
                    "Priority email support"
                },
                IsRecommended = false
            },
            new SubscriptionPlanInfo
            {
                Plan = SubscriptionPlan.Yearly,
                Name = "Yearly",
                Description = "Best value for enterprises",
                Price = _settings.YearlyPrice,
                PriceDisplay = $"${_settings.YearlyPrice}/yr",
                BillingPeriod = "Yearly",
                AssessmentsPerMonth = -1,
                AssessmentsDisplay = "Unlimited assessments",
                Features = new List<string>
                {
                    "All 9 assessment domains",
                    "Full security scoring",
                    "PDF/HTML reports",
                    "Email notifications",
                    "Priority support",
                    "Custom integrations",
                    "Dedicated account manager"
                },
                IsRecommended = true
            }
        };

        return Task.FromResult<IReadOnlyList<SubscriptionPlanInfo>>(plans);
    }

    public async Task<SubscriptionDto> CreateTrialAsync(Guid tenantId, CancellationToken ct = default)
    {
        var existing = await _unitOfWork.Subscriptions.GetByTenantIdAsync(tenantId, ct);
        if (existing != null)
        {
            throw new InvalidOperationException("Tenant already has a subscription");
        }

        var tenant = await _unitOfWork.Tenants.GetByIdAsync(tenantId, ct);
        if (tenant == null)
        {
            throw new InvalidOperationException($"Tenant with ID '{tenantId}' not found");
        }

        var now = DateTime.UtcNow;
        var subscription = new Subscription
        {
            TenantId = tenantId,
            Plan = SubscriptionPlan.Trial,
            Status = SubscriptionStatus.Trial,
            StartDate = now,
            EndDate = now.AddDays(_settings.TrialDays),
            TrialEndDate = now.AddDays(_settings.TrialDays),
            MonthlyAssessmentLimit = _settings.TrialAssessmentLimit,
            AssessmentsUsedThisMonth = 0,
            CurrentPeriodStart = now
        };

        await _unitOfWork.Subscriptions.AddAsync(subscription, ct);
        await _unitOfWork.SaveChangesAsync(ct);

        _logger.LogInformation("Created trial subscription for tenant {TenantId}, expires {EndDate}", tenantId, subscription.EndDate);

        return MapToDto(subscription, tenant.Name);
    }

    public async Task<bool> CanRunAssessmentAsync(Guid tenantId, CancellationToken ct = default)
    {
        var subscription = await _unitOfWork.Subscriptions.GetByTenantIdAsync(tenantId, ct);
        if (subscription == null)
            return false;

        // Check status
        if (subscription.Status == SubscriptionStatus.Expired ||
            subscription.Status == SubscriptionStatus.Cancelled)
            return false;

        // Check if trial has expired
        if (subscription.Status == SubscriptionStatus.Trial &&
            subscription.TrialEndDate.HasValue &&
            DateTime.UtcNow > subscription.TrialEndDate.Value)
            return false;

        // Check assessment limit (-1 means unlimited)
        if (subscription.MonthlyAssessmentLimit == -1)
            return true;

        return subscription.AssessmentsUsedThisMonth < subscription.MonthlyAssessmentLimit;
    }

    public async Task IncrementAssessmentCountAsync(Guid tenantId, CancellationToken ct = default)
    {
        var subscription = await _unitOfWork.Subscriptions.GetByTenantIdAsync(tenantId, ct);
        if (subscription == null)
        {
            _logger.LogWarning("Attempted to increment assessment count for tenant {TenantId} without subscription", tenantId);
            return;
        }

        subscription.AssessmentsUsedThisMonth++;
        await _unitOfWork.Subscriptions.UpdateAsync(subscription, ct);
        await _unitOfWork.SaveChangesAsync(ct);

        _logger.LogInformation("Incremented assessment count for tenant {TenantId} to {Count}", tenantId, subscription.AssessmentsUsedThisMonth);
    }

    public async Task ResetMonthlyCountersAsync(CancellationToken ct = default)
    {
        var now = DateTime.UtcNow;
        var firstOfMonth = new DateTime(now.Year, now.Month, 1, 0, 0, 0, DateTimeKind.Utc);

        var subscriptionsToReset = await _unitOfWork.Subscriptions.GetSubscriptionsNeedingResetAsync(firstOfMonth, ct);

        foreach (var subscription in subscriptionsToReset)
        {
            subscription.AssessmentsUsedThisMonth = 0;
            subscription.CurrentPeriodStart = firstOfMonth;
            await _unitOfWork.Subscriptions.UpdateAsync(subscription, ct);
        }

        if (subscriptionsToReset.Any())
        {
            await _unitOfWork.SaveChangesAsync(ct);
            _logger.LogInformation("Reset monthly assessment counters for {Count} subscriptions", subscriptionsToReset.Count);
        }
    }

    public async Task<SubscriptionDto> UpgradeAsync(UpgradeSubscriptionDto dto, CancellationToken ct = default)
    {
        var subscription = await _unitOfWork.Subscriptions.GetByTenantIdAsync(dto.TenantId, ct);
        if (subscription == null)
        {
            throw new InvalidOperationException($"No subscription found for tenant {dto.TenantId}");
        }

        var tenant = await _unitOfWork.Tenants.GetByIdAsync(dto.TenantId, ct);
        if (tenant == null)
        {
            throw new InvalidOperationException($"Tenant with ID '{dto.TenantId}' not found");
        }

        var now = DateTime.UtcNow;

        subscription.Plan = dto.NewPlan;
        subscription.Status = SubscriptionStatus.Active;

        switch (dto.NewPlan)
        {
            case SubscriptionPlan.Monthly:
                subscription.EndDate = now.AddMonths(1);
                subscription.MonthlyAssessmentLimit = _settings.MonthlyAssessmentLimit;
                subscription.MonthlyPrice = _settings.MonthlyPrice;
                break;
            case SubscriptionPlan.Yearly:
                subscription.EndDate = now.AddYears(1);
                subscription.MonthlyAssessmentLimit = _settings.YearlyAssessmentLimit;
                subscription.YearlyPrice = _settings.YearlyPrice;
                break;
        }

        subscription.TrialEndDate = null;
        subscription.CurrentPeriodStart = now;
        subscription.AssessmentsUsedThisMonth = 0;

        await _unitOfWork.Subscriptions.UpdateAsync(subscription, ct);
        await _unitOfWork.SaveChangesAsync(ct);

        _logger.LogInformation("Upgraded subscription for tenant {TenantId} to {Plan}", dto.TenantId, dto.NewPlan);

        return MapToDto(subscription, tenant.Name);
    }

    public async Task<SubscriptionDto> CancelAsync(Guid tenantId, CancellationToken ct = default)
    {
        var subscription = await _unitOfWork.Subscriptions.GetByTenantIdAsync(tenantId, ct);
        if (subscription == null)
        {
            throw new InvalidOperationException($"No subscription found for tenant {tenantId}");
        }

        var tenant = await _unitOfWork.Tenants.GetByIdAsync(tenantId, ct);

        // Cancel in Stripe if applicable
        if (!string.IsNullOrEmpty(subscription.StripeSubscriptionId))
        {
            await _stripeService.CancelSubscriptionAsync(subscription.StripeSubscriptionId, ct);
        }

        subscription.Status = SubscriptionStatus.Cancelled;
        await _unitOfWork.Subscriptions.UpdateAsync(subscription, ct);
        await _unitOfWork.SaveChangesAsync(ct);

        _logger.LogInformation("Cancelled subscription for tenant {TenantId}", tenantId);

        return MapToDto(subscription, tenant?.Name ?? "Unknown");
    }

    public async Task CheckExpiredSubscriptionsAsync(CancellationToken ct = default)
    {
        var expiredSubscriptions = await _unitOfWork.Subscriptions.GetExpiredSubscriptionsAsync(ct);

        foreach (var subscription in expiredSubscriptions)
        {
            subscription.Status = SubscriptionStatus.Expired;
            await _unitOfWork.Subscriptions.UpdateAsync(subscription, ct);
        }

        if (expiredSubscriptions.Any())
        {
            await _unitOfWork.SaveChangesAsync(ct);
            _logger.LogInformation("Marked {Count} subscriptions as expired", expiredSubscriptions.Count);
        }
    }

    public async Task<CheckoutSessionResult> CreateCheckoutSessionAsync(
        Guid tenantId,
        SubscriptionPlan plan,
        string successUrl,
        string cancelUrl,
        CancellationToken ct = default)
    {
        var subscription = await _unitOfWork.Subscriptions.GetByTenantIdAsync(tenantId, ct);
        var tenant = await _unitOfWork.Tenants.GetByIdAsync(tenantId, ct);

        if (tenant == null)
        {
            return new CheckoutSessionResult
            {
                Success = false,
                ErrorMessage = "Tenant not found"
            };
        }

        string customerId;

        if (subscription?.StripeCustomerId != null)
        {
            customerId = subscription.StripeCustomerId;
        }
        else
        {
            customerId = await _stripeService.CreateCustomerAsync(tenantId, tenant.Name, tenant.ContactEmail, ct);

            if (subscription != null)
            {
                subscription.StripeCustomerId = customerId;
                await _unitOfWork.Subscriptions.UpdateAsync(subscription, ct);
                await _unitOfWork.SaveChangesAsync(ct);
            }
        }

        return await _stripeService.CreateCheckoutSessionAsync(customerId, plan, successUrl, cancelUrl, ct);
    }

    public async Task HandleStripeWebhookAsync(string payload, string signature, CancellationToken ct = default)
    {
        var (isValid, eventType, subscriptionId, customerId, status) = await _stripeService.ValidateWebhookAsync(payload, signature, ct);

        if (!isValid)
        {
            _logger.LogWarning("Invalid Stripe webhook signature");
            return;
        }

        _logger.LogInformation("Processing Stripe webhook event: {EventType}", eventType);

        switch (eventType)
        {
            case "customer.subscription.created":
            case "customer.subscription.updated":
                if (!string.IsNullOrEmpty(customerId))
                {
                    var subscription = await _unitOfWork.Subscriptions.GetByStripeCustomerIdAsync(customerId, ct);
                    if (subscription != null && !string.IsNullOrEmpty(subscriptionId))
                    {
                        subscription.StripeSubscriptionId = subscriptionId;

                        if (status == "active")
                        {
                            subscription.Status = SubscriptionStatus.Active;
                        }
                        else if (status == "past_due")
                        {
                            subscription.Status = SubscriptionStatus.PastDue;
                        }

                        await _unitOfWork.Subscriptions.UpdateAsync(subscription, ct);
                        await _unitOfWork.SaveChangesAsync(ct);
                    }
                }
                break;

            case "customer.subscription.deleted":
                if (!string.IsNullOrEmpty(subscriptionId))
                {
                    var subscription = await _unitOfWork.Subscriptions.GetByStripeSubscriptionIdAsync(subscriptionId, ct);
                    if (subscription != null)
                    {
                        subscription.Status = SubscriptionStatus.Cancelled;
                        await _unitOfWork.Subscriptions.UpdateAsync(subscription, ct);
                        await _unitOfWork.SaveChangesAsync(ct);
                    }
                }
                break;

            case "invoice.payment_succeeded":
                _logger.LogInformation("Payment succeeded for subscription {SubscriptionId}", subscriptionId);
                break;

            case "invoice.payment_failed":
                if (!string.IsNullOrEmpty(customerId))
                {
                    var subscription = await _unitOfWork.Subscriptions.GetByStripeCustomerIdAsync(customerId, ct);
                    if (subscription != null)
                    {
                        subscription.Status = SubscriptionStatus.PastDue;
                        await _unitOfWork.Subscriptions.UpdateAsync(subscription, ct);
                        await _unitOfWork.SaveChangesAsync(ct);
                    }
                }
                break;
        }
    }

    private SubscriptionDto MapToDto(Subscription subscription, string tenantName)
    {
        var now = DateTime.UtcNow;
        var daysRemaining = Math.Max(0, (int)(subscription.EndDate - now).TotalDays);
        var isTrialExpired = subscription.Status == SubscriptionStatus.Trial &&
                             subscription.TrialEndDate.HasValue &&
                             now > subscription.TrialEndDate.Value;

        var limit = subscription.MonthlyAssessmentLimit;
        var remaining = limit == -1 ? int.MaxValue : Math.Max(0, limit - subscription.AssessmentsUsedThisMonth);

        var canRun = subscription.Status != SubscriptionStatus.Expired &&
                     subscription.Status != SubscriptionStatus.Cancelled &&
                     !isTrialExpired &&
                     (limit == -1 || subscription.AssessmentsUsedThisMonth < limit);

        return new SubscriptionDto
        {
            Id = subscription.Id,
            TenantId = subscription.TenantId,
            TenantName = tenantName,
            Plan = subscription.Plan,
            Status = subscription.Status,
            StartDate = subscription.StartDate,
            EndDate = subscription.EndDate,
            TrialEndDate = subscription.TrialEndDate,
            MonthlyAssessmentLimit = limit,
            AssessmentsUsedThisMonth = subscription.AssessmentsUsedThisMonth,
            AssessmentsRemaining = limit == -1 ? -1 : remaining,
            CanRunAssessment = canRun,
            DaysRemaining = daysRemaining,
            IsTrialExpired = isTrialExpired,
            StripeCustomerId = subscription.StripeCustomerId,
            StripeSubscriptionId = subscription.StripeSubscriptionId,
            MonthlyPrice = subscription.MonthlyPrice ?? _settings.MonthlyPrice,
            YearlyPrice = subscription.YearlyPrice ?? _settings.YearlyPrice
        };
    }

    private class SubscriptionSettings
    {
        public int TrialDays { get; set; }
        public int TrialAssessmentLimit { get; set; }
        public int MonthlyAssessmentLimit { get; set; }
        public int YearlyAssessmentLimit { get; set; }
        public decimal MonthlyPrice { get; set; }
        public decimal YearlyPrice { get; set; }
    }
}
