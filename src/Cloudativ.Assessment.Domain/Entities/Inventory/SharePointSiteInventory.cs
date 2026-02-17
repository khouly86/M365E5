namespace Cloudativ.Assessment.Domain.Entities.Inventory;

/// <summary>
/// SharePoint site inventory.
/// </summary>
public class SharePointSiteInventory : BaseEntity
{
    public Guid TenantId { get; set; }
    public Guid SnapshotId { get; set; }

    public string SiteId { get; set; } = string.Empty;
    public string WebUrl { get; set; } = string.Empty;
    public string? Title { get; set; }
    public string? Description { get; set; }
    public string Template { get; set; } = string.Empty;
    public string? WebTemplate { get; set; }
    public DateTime? CreatedDateTime { get; set; }
    public DateTime? LastActivityDate { get; set; }
    public DateTime? LastModifiedDateTime { get; set; }
    public bool IsInactive { get; set; }
    public int InactiveDays { get; set; }

    // Site Type
    public bool IsTeamSite { get; set; }
    public bool IsCommunicationSite { get; set; }
    public bool IsHubSite { get; set; }
    public string? HubSiteId { get; set; }
    public bool IsGroupConnected { get; set; }
    public string? GroupId { get; set; }

    // Sharing
    public string SharingCapability { get; set; } = string.Empty;
    public bool HasExternalSharing { get; set; }
    public bool AllowsAnonymousSharing { get; set; }
    public int ExternalUserCount { get; set; }
    public int AnonymousLinkCount { get; set; }
    public string? SharingDomainRestrictionMode { get; set; }

    // Storage
    public long StorageUsedBytes { get; set; }
    public long StorageQuotaBytes { get; set; }
    public double StoragePercentUsed { get; set; }
    public long StorageWarningLevelBytes { get; set; }

    // Ownership
    public string? OwnerUpn { get; set; }
    public string? OwnerDisplayName { get; set; }
    public string? SecondaryOwnerUpn { get; set; }
    public bool IsOrphaned { get; set; }
    public int OwnerCount { get; set; }
    public int MemberCount { get; set; }

    // Classification & Sensitivity
    public string? SensitivityLabel { get; set; }
    public string? SensitivityLabelId { get; set; }
    public string? Classification { get; set; }

    // Features
    public bool IsReadOnly { get; set; }
    public bool IsLocked { get; set; }
    public string? LockState { get; set; }
    public bool DenyAddAndCustomizePages { get; set; }
    public string? ConditionalAccessPolicy { get; set; }

    // Content
    public int FileCount { get; set; }
    public int PageCount { get; set; }
    public int ListCount { get; set; }
    public int SubsiteCount { get; set; }

    // Navigation
    public virtual Tenant Tenant { get; set; } = null!;
    public virtual InventorySnapshot Snapshot { get; set; } = null!;
}
