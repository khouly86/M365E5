namespace Cloudativ.Assessment.Domain.Entities.Inventory;

/// <summary>
/// Device compliance policy inventory.
/// </summary>
public class CompliancePolicyInventory : BaseEntity
{
    public Guid TenantId { get; set; }
    public Guid SnapshotId { get; set; }

    public string PolicyId { get; set; } = string.Empty;
    public string DisplayName { get; set; } = string.Empty;
    public string? Description { get; set; }
    public string Platform { get; set; } = string.Empty;
    public string? PolicyType { get; set; }
    public DateTime? CreatedDateTime { get; set; }
    public DateTime? LastModifiedDateTime { get; set; }
    public int Version { get; set; }

    // Policy Settings
    public string? SettingsJson { get; set; }
    public bool RequiresEncryption { get; set; }
    public bool RequiresMinOsVersion { get; set; }
    public string? MinOsVersion { get; set; }
    public string? MaxOsVersion { get; set; }
    public bool RequiresPasswordComplexity { get; set; }
    public int? MinPasswordLength { get; set; }
    public bool BlocksJailbroken { get; set; }
    public bool RequiresDefender { get; set; }
    public bool RequiresStorageEncryption { get; set; }
    public bool RequiresSecureBoot { get; set; }
    public bool RequiresCodeIntegrity { get; set; }

    // Assignment
    public int AssignedUserCount { get; set; }
    public int AssignedDeviceCount { get; set; }
    public string? AssignmentsJson { get; set; }
    public bool IsAssignedToAllDevices { get; set; }

    // Status
    public int CompliantCount { get; set; }
    public int NonCompliantCount { get; set; }
    public int ErrorCount { get; set; }
    public int ConflictCount { get; set; }
    public int NotApplicableCount { get; set; }

    // Actions for Non-Compliance
    public string? ScheduledActionsJson { get; set; }
    public int GracePeriodHours { get; set; }

    // Navigation
    public virtual Tenant Tenant { get; set; } = null!;
    public virtual InventorySnapshot Snapshot { get; set; } = null!;
}
