namespace Cloudativ.Assessment.Domain.Entities.Inventory;

/// <summary>
/// Sensitivity label inventory from Microsoft Purview.
/// </summary>
public class SensitivityLabelInventory : BaseEntity
{
    public Guid TenantId { get; set; }
    public Guid SnapshotId { get; set; }

    public string LabelId { get; set; } = string.Empty;
    public string DisplayName { get; set; } = string.Empty;
    public string? Description { get; set; }
    public string? Tooltip { get; set; }
    public int Priority { get; set; }
    public bool IsActive { get; set; }
    public string? ParentLabelId { get; set; }

    // Scope
    public bool AppliesToFiles { get; set; }
    public bool AppliesToEmails { get; set; }
    public bool AppliesToSites { get; set; }
    public bool AppliesToGroups { get; set; }
    public bool AppliesToMeetings { get; set; }
    public bool AppliesToSchematizedData { get; set; }

    // Protection Settings
    public bool HasEncryption { get; set; }
    public string? EncryptionSettingsJson { get; set; }
    public string? EncryptionProtectionType { get; set; }
    public bool HasContentMarking { get; set; }
    public bool HasHeader { get; set; }
    public bool HasFooter { get; set; }
    public bool HasWatermark { get; set; }
    public string? ContentMarkingJson { get; set; }

    // Auto-labeling
    public bool AutoLabelingEnabled { get; set; }
    public string? AutoLabelingConditionsJson { get; set; }
    public int AutoLabelingConditionCount { get; set; }

    // Sublabels
    public bool HasSublabels { get; set; }
    public int SublabelCount { get; set; }
    public string? SublabelsJson { get; set; }

    // Color
    public string? Color { get; set; }

    // Navigation
    public virtual Tenant Tenant { get; set; } = null!;
    public virtual InventorySnapshot Snapshot { get; set; } = null!;
}
