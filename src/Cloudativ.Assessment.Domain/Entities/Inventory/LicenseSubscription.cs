using Cloudativ.Assessment.Domain.Enums;

namespace Cloudativ.Assessment.Domain.Entities.Inventory;

/// <summary>
/// License subscription information including SKUs and usage.
/// </summary>
public class LicenseSubscription : BaseEntity
{
    public Guid TenantId { get; set; }
    public Guid SnapshotId { get; set; }

    public string SkuId { get; set; } = string.Empty;
    public string SkuPartNumber { get; set; } = string.Empty;
    public string? DisplayName { get; set; }
    public int PrepaidUnits { get; set; }
    public int ConsumedUnits { get; set; }
    public int AvailableUnits { get; set; }
    public int SuspendedUnits { get; set; }
    public int WarningUnits { get; set; }
    public bool IsTrial { get; set; }
    public DateTime? ExpirationDate { get; set; }
    public string? AppliesTo { get; set; }
    public string? ServicePlansJson { get; set; }
    public string? CapabilityStatus { get; set; }

    // License categorization
    public LicenseCategory LicenseCategory { get; set; } = LicenseCategory.Unknown;
    public string? IncludedFeaturesJson { get; set; }
    public bool IsPrimaryLicense { get; set; }
    public string? TierGroup { get; set; }
    public decimal EstimatedMonthlyPricePerUser { get; set; }

    // Navigation
    public virtual Tenant Tenant { get; set; } = null!;
    public virtual InventorySnapshot Snapshot { get; set; } = null!;
}
