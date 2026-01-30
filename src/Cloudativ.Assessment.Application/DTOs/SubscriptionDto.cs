using Cloudativ.Assessment.Domain.Enums;

namespace Cloudativ.Assessment.Application.DTOs;

public record SubscriptionDto
{
    public Guid Id { get; init; }
    public Guid TenantId { get; init; }
    public string TenantName { get; init; } = string.Empty;
    public SubscriptionPlan Plan { get; init; }
    public SubscriptionStatus Status { get; init; }
    public DateTime StartDate { get; init; }
    public DateTime EndDate { get; init; }
    public DateTime? TrialEndDate { get; init; }
    public int MonthlyAssessmentLimit { get; init; }
    public int AssessmentsUsedThisMonth { get; init; }
    public int AssessmentsRemaining { get; init; }
    public bool CanRunAssessment { get; init; }
    public int DaysRemaining { get; init; }
    public bool IsTrialExpired { get; init; }
    public string? StripeCustomerId { get; init; }
    public string? StripeSubscriptionId { get; init; }
    public decimal? MonthlyPrice { get; init; }
    public decimal? YearlyPrice { get; init; }
}

public record CreateSubscriptionDto
{
    public Guid TenantId { get; init; }
    public SubscriptionPlan Plan { get; init; }
}

public record UpgradeSubscriptionDto
{
    public Guid TenantId { get; init; }
    public SubscriptionPlan NewPlan { get; init; }
    public string? StripePaymentMethodId { get; init; }
}

public record SubscriptionPlanInfo
{
    public SubscriptionPlan Plan { get; init; }
    public string Name { get; init; } = string.Empty;
    public string Description { get; init; } = string.Empty;
    public decimal Price { get; init; }
    public string PriceDisplay { get; init; } = string.Empty;
    public string BillingPeriod { get; init; } = string.Empty;
    public int AssessmentsPerMonth { get; init; }
    public string AssessmentsDisplay { get; init; } = string.Empty;
    public List<string> Features { get; init; } = new();
    public bool IsRecommended { get; init; }
}

public record CheckoutSessionResult
{
    public bool Success { get; init; }
    public string? SessionId { get; init; }
    public string? SessionUrl { get; init; }
    public string? ErrorMessage { get; init; }
}

public record SubscriptionUsageDto
{
    public int AssessmentsUsed { get; init; }
    public int AssessmentsLimit { get; init; }
    public int AssessmentsRemaining { get; init; }
    public double UsagePercentage { get; init; }
    public DateTime PeriodStart { get; init; }
    public DateTime PeriodEnd { get; init; }
    public bool CanRunAssessment { get; init; }
    public string? LimitReachedMessage { get; init; }
}
