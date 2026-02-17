namespace Cloudativ.Assessment.Domain.Entities.Inventory;

/// <summary>
/// Named location inventory for Conditional Access.
/// </summary>
public class NamedLocationInventory : BaseEntity
{
    public Guid TenantId { get; set; }
    public Guid SnapshotId { get; set; }

    public string LocationId { get; set; } = string.Empty;
    public string DisplayName { get; set; } = string.Empty;
    public string LocationType { get; set; } = string.Empty;
    public DateTime? CreatedDateTime { get; set; }
    public DateTime? ModifiedDateTime { get; set; }

    // IP Named Location
    public bool IsTrusted { get; set; }
    public string? IpRangesJson { get; set; }
    public int IpRangeCount { get; set; }

    // Country Named Location
    public string? CountriesAndRegionsJson { get; set; }
    public bool IncludeUnknownCountriesAndRegions { get; set; }
    public int CountryCount { get; set; }

    // Navigation
    public virtual Tenant Tenant { get; set; } = null!;
    public virtual InventorySnapshot Snapshot { get; set; } = null!;
}
