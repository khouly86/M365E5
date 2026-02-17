namespace Cloudativ.Assessment.Domain.Entities.Inventory;

/// <summary>
/// Managed identity inventory (system-assigned and user-assigned).
/// </summary>
public class ManagedIdentityInventory : BaseEntity
{
    public Guid TenantId { get; set; }
    public Guid SnapshotId { get; set; }

    public string ObjectId { get; set; } = string.Empty;
    public string DisplayName { get; set; } = string.Empty;
    public string ManagedIdentityType { get; set; } = string.Empty;
    public string? AppId { get; set; }
    public string? ResourceId { get; set; }
    public string? AlternativeNames { get; set; }
    public DateTime? CreatedDateTime { get; set; }

    // Permissions
    public string? AssignedPermissionsJson { get; set; }
    public int PermissionCount { get; set; }

    // Associated Resource
    public string? AssociatedResourceType { get; set; }
    public string? AssociatedResourceName { get; set; }

    // Navigation
    public virtual Tenant Tenant { get; set; } = null!;
    public virtual InventorySnapshot Snapshot { get; set; } = null!;
}
