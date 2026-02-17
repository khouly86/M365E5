namespace Cloudativ.Assessment.Domain.Entities.Inventory;

/// <summary>
/// SharePoint Online tenant-level settings.
/// </summary>
public class SharePointSettingsInventory : BaseEntity
{
    public Guid TenantId { get; set; }
    public Guid SnapshotId { get; set; }

    // Sharing Settings
    public string SharingCapability { get; set; } = string.Empty;
    public string DefaultSharingLinkType { get; set; } = string.Empty;
    public string DefaultLinkPermission { get; set; } = string.Empty;
    public int? DefaultLinkExpirationDays { get; set; }
    public bool RequireAcceptingAccountMatchInvitedAccount { get; set; }
    public bool RequireAnonymousLinksExpireInDays { get; set; }
    public int? AnonymousLinkExpirationDays { get; set; }
    public bool FileAnonymousLinkType { get; set; }
    public bool FolderAnonymousLinkType { get; set; }

    // External Access
    public bool AllowGuestUserSignIn { get; set; }
    public string? AllowedDomainList { get; set; }
    public string? BlockedDomainList { get; set; }
    public bool ExternalUserExpirationRequired { get; set; }
    public int? ExternalUserExpirationDays { get; set; }
    public bool ShowEveryoneClaim { get; set; }
    public bool ShowEveryoneExceptExternalUsersClaim { get; set; }

    // Conditional Access / Device Access
    public bool ConditionalAccessPolicyEnabled { get; set; }
    public string? UnmanagedDevicePolicy { get; set; }
    public bool BlockDownloadOfViewableFilesOnUnmanagedDevices { get; set; }
    public bool BlockDownloadOfAllFilesOnUnmanagedDevices { get; set; }
    public bool AllowEditing { get; set; }

    // OneDrive Specific
    public string OneDriveSharingCapability { get; set; } = string.Empty;
    public long? OneDriveStorageQuota { get; set; }
    public bool OneDriveForGuestsEnabled { get; set; }

    // Sites Summary
    public int TotalSiteCount { get; set; }
    public int CommunicationSiteCount { get; set; }
    public int TeamSiteCount { get; set; }
    public int ClassicSiteCount { get; set; }
    public int SitesWithExternalSharing { get; set; }
    public int InactiveSiteCount { get; set; }
    public long TotalStorageUsedBytes { get; set; }
    public long TotalStorageQuotaBytes { get; set; }

    // Features
    public bool CommentsOnSitePagesDisabled { get; set; }
    public bool DisallowInfectedFileDownload { get; set; }
    public bool ExternalServicesEnabled { get; set; }
    public bool LegacyAuthProtocolsEnabled { get; set; }
    public bool NotificationsInOneDriveForBusinessEnabled { get; set; }
    public bool NotificationsInSharePointEnabled { get; set; }

    // Versioning
    public int MajorVersionLimit { get; set; }
    public bool EnableAutoExpirationVersionTrim { get; set; }

    // Navigation
    public virtual Tenant Tenant { get; set; } = null!;
    public virtual InventorySnapshot Snapshot { get; set; } = null!;
}
