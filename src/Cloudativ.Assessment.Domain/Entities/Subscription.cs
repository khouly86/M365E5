using Cloudativ.Assessment.Domain.Enums;

namespace Cloudativ.Assessment.Domain.Entities;

public class Subscription : BaseEntity
{
    public Guid TenantId { get; set; }
    public SubscriptionPlan Plan { get; set; }
    public SubscriptionStatus Status { get; set; }

    // Dates
    public DateTime StartDate { get; set; }
    public DateTime EndDate { get; set; }
    public DateTime? TrialEndDate { get; set; }

    // Assessment Limits
    public int MonthlyAssessmentLimit { get; set; }
    public int AssessmentsUsedThisMonth { get; set; }
    public DateTime CurrentPeriodStart { get; set; }

    // Stripe Integration
    public string? StripeCustomerId { get; set; }
    public string? StripeSubscriptionId { get; set; }
    public string? StripePriceId { get; set; }

    // Pricing
    public decimal? MonthlyPrice { get; set; }
    public decimal? YearlyPrice { get; set; }

    // Navigation
    public virtual Tenant Tenant { get; set; } = null!;
}
