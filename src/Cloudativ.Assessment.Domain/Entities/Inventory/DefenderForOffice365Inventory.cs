namespace Cloudativ.Assessment.Domain.Entities.Inventory;

/// <summary>
/// Microsoft Defender for Office 365 inventory and policy configuration.
/// </summary>
public class DefenderForOffice365Inventory : BaseEntity
{
    public Guid TenantId { get; set; }
    public Guid SnapshotId { get; set; }

    // Safe Links
    public bool SafeLinksEnabled { get; set; }
    public string? SafeLinksPoliciesJson { get; set; }
    public int SafeLinksPolicyCount { get; set; }
    public bool SafeLinksForOfficeApps { get; set; }
    public bool SafeLinksTrackUserClicks { get; set; }

    // Safe Attachments
    public bool SafeAttachmentsEnabled { get; set; }
    public string? SafeAttachmentsPoliciesJson { get; set; }
    public int SafeAttachmentsPolicyCount { get; set; }
    public string? SafeAttachmentsMode { get; set; }
    public bool SafeAttachmentsForSharePoint { get; set; }

    // Anti-Phish
    public string? AntiPhishPoliciesJson { get; set; }
    public int AntiPhishPolicyCount { get; set; }
    public bool ImpersonationProtectionEnabled { get; set; }
    public bool MailboxIntelligenceEnabled { get; set; }
    public bool SpoofIntelligenceEnabled { get; set; }
    public int ProtectedUsersCount { get; set; }
    public int ProtectedDomainsCount { get; set; }

    // Anti-Spam
    public string? AntiSpamPoliciesJson { get; set; }
    public int AntiSpamPolicyCount { get; set; }
    public string? DefaultSpamAction { get; set; }
    public string? HighConfidenceSpamAction { get; set; }

    // Anti-Malware
    public string? AntiMalwarePoliciesJson { get; set; }
    public int AntiMalwarePolicyCount { get; set; }
    public bool CommonAttachmentTypesFilter { get; set; }
    public bool ZeroHourAutoPurgeEnabled { get; set; }

    // Email Auth - DKIM
    public bool DkimEnabled { get; set; }
    public int DkimEnabledDomains { get; set; }
    public string? DkimStatusJson { get; set; }

    // Email Auth - DMARC
    public bool DmarcEnabled { get; set; }
    public string? DmarcPolicy { get; set; }
    public string? DmarcStatusJson { get; set; }

    // Email Auth - SPF
    public bool SpfConfigured { get; set; }
    public string? SpfStatusJson { get; set; }

    // Full Email Auth Status
    public string? EmailAuthStatusJson { get; set; }

    // Threat Explorer (summary)
    public int Last30DaysMalwareCount { get; set; }
    public int Last30DaysPhishCount { get; set; }
    public int Last30DaysSpamCount { get; set; }
    public int Last30DaysBlockedCount { get; set; }

    // Navigation
    public virtual Tenant Tenant { get; set; } = null!;
    public virtual InventorySnapshot Snapshot { get; set; } = null!;
}
