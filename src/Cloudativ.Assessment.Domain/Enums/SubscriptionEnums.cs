namespace Cloudativ.Assessment.Domain.Enums;

public enum SubscriptionPlan
{
    Trial = 0,
    Monthly = 1,
    Yearly = 2
}

public enum SubscriptionStatus
{
    Active = 0,
    Trial = 1,
    Expired = 2,
    Cancelled = 3,
    PastDue = 4
}
