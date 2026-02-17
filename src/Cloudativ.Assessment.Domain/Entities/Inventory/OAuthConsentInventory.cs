namespace Cloudativ.Assessment.Domain.Entities.Inventory;

/// <summary>
/// OAuth consent settings and pending requests.
/// </summary>
public class OAuthConsentInventory : BaseEntity
{
    public Guid TenantId { get; set; }
    public Guid SnapshotId { get; set; }

    // User Consent Settings
    public bool UserConsentEnabled { get; set; }
    public string UserConsentScope { get; set; } = string.Empty;
    public string? UserConsentDescription { get; set; }
    public bool AllowUserConsentForRiskyApps { get; set; }
    public bool BlockUserConsentForRiskyApps { get; set; }

    // Admin Consent Workflow
    public bool AdminConsentWorkflowEnabled { get; set; }
    public string? AdminConsentReviewersJson { get; set; }
    public int AdminConsentReviewerCount { get; set; }
    public int RequestExpirationDays { get; set; }
    public bool NotifyReviewers { get; set; }
    public bool RemindersEnabled { get; set; }

    // Pending Admin Consent Requests
    public int PendingAdminConsentRequests { get; set; }
    public int ApprovedAdminConsentRequests { get; set; }
    public int DeniedAdminConsentRequests { get; set; }
    public int ExpiredAdminConsentRequests { get; set; }
    public string? PendingRequestsJson { get; set; }

    // Risk-based Consent
    public bool RiskyConsentBlocked { get; set; }
    public bool VerifiedPublisherRequired { get; set; }

    // Group Owner Consent
    public bool GroupOwnerConsentEnabled { get; set; }
    public string? GroupOwnerConsentScope { get; set; }

    // Permission Grant Policies
    public string? PermissionGrantPoliciesJson { get; set; }
    public int PermissionGrantPolicyCount { get; set; }

    // Existing OAuth Grants Summary
    public int TotalOAuthGrantCount { get; set; }
    public int AdminGrantedCount { get; set; }
    public int UserGrantedCount { get; set; }
    public int HighRiskGrantCount { get; set; }

    // Navigation
    public virtual Tenant Tenant { get; set; } = null!;
    public virtual InventorySnapshot Snapshot { get; set; } = null!;
}
