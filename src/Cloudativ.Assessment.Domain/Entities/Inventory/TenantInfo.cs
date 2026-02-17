namespace Cloudativ.Assessment.Domain.Entities.Inventory;

/// <summary>
/// Tenant baseline information including domains, settings, and service health.
/// </summary>
public class TenantInfo : BaseEntity
{
    public Guid TenantId { get; set; }
    public Guid SnapshotId { get; set; }

    // Tenant Identity
    public string AzureTenantId { get; set; } = string.Empty;
    public string DisplayName { get; set; } = string.Empty;
    public string? TechnicalNotificationMails { get; set; }

    // Domains
    public string? VerifiedDomainsJson { get; set; }
    public string? PrimaryDomain { get; set; }
    public int VerifiedDomainCount { get; set; }

    // Geo/Data Residency
    public string? PreferredDataLocation { get; set; }
    public string? DefaultUsageLocation { get; set; }
    public bool IsMultiGeoEnabled { get; set; }
    public string? MultiGeoLocationsJson { get; set; }

    // Organization Settings
    public bool ModernAuthEnabled { get; set; }
    public bool SmtpAuthEnabled { get; set; }
    public bool LegacyProtocolsEnabled { get; set; }
    public string? OrganizationSettingsJson { get; set; }

    // Service Health
    public string? ServiceHealthJson { get; set; }
    public string? MessageCenterHighlightsJson { get; set; }
    public int ActiveServiceIssues { get; set; }
    public int MessageCenterItemCount { get; set; }

    // Navigation
    public virtual Tenant Tenant { get; set; } = null!;
    public virtual InventorySnapshot Snapshot { get; set; } = null!;
}
