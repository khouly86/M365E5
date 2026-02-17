namespace Cloudativ.Assessment.Domain.Entities.Inventory;

/// <summary>
/// Device configuration profile inventory.
/// </summary>
public class ConfigurationProfileInventory : BaseEntity
{
    public Guid TenantId { get; set; }
    public Guid SnapshotId { get; set; }

    public string ProfileId { get; set; } = string.Empty;
    public string DisplayName { get; set; } = string.Empty;
    public string? Description { get; set; }
    public string Platform { get; set; } = string.Empty;
    public string? ProfileType { get; set; }
    public DateTime? CreatedDateTime { get; set; }
    public DateTime? LastModifiedDateTime { get; set; }
    public int Version { get; set; }

    // Template Info
    public string? TemplateId { get; set; }
    public string? TemplateDisplayName { get; set; }
    public bool IsSecurityBaseline { get; set; }

    // Settings
    public string? SettingsJson { get; set; }
    public int SettingCount { get; set; }

    // Assignment
    public int AssignedUserCount { get; set; }
    public int AssignedDeviceCount { get; set; }
    public string? AssignmentsJson { get; set; }
    public bool IsAssignedToAllDevices { get; set; }

    // Status
    public int SuccessCount { get; set; }
    public int FailureCount { get; set; }
    public int PendingCount { get; set; }
    public int ErrorCount { get; set; }
    public int ConflictCount { get; set; }
    public int NotApplicableCount { get; set; }

    // Navigation
    public virtual Tenant Tenant { get; set; } = null!;
    public virtual InventorySnapshot Snapshot { get; set; } = null!;
}
