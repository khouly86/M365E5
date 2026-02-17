namespace Cloudativ.Assessment.Domain.Entities.Inventory;

/// <summary>
/// Service principal inventory with permissions and ownership.
/// </summary>
public class ServicePrincipalInventory : BaseEntity
{
    public Guid TenantId { get; set; }
    public Guid SnapshotId { get; set; }

    public string ObjectId { get; set; } = string.Empty;
    public string AppId { get; set; } = string.Empty;
    public string DisplayName { get; set; } = string.Empty;
    public string ServicePrincipalType { get; set; } = string.Empty;
    public bool AccountEnabled { get; set; }
    public string? PublisherName { get; set; }
    public string? VerifiedPublisher { get; set; }
    public bool IsMicrosoftFirstParty { get; set; }
    public DateTime? CreatedDateTime { get; set; }
    public string? AppOwnerOrganizationId { get; set; }

    // Sign-in
    public string? SignInAudience { get; set; }
    public DateTime? LastSignInDateTime { get; set; }

    // Permissions
    public string? DelegatedPermissionsJson { get; set; }
    public string? ApplicationPermissionsJson { get; set; }
    public int DelegatedPermissionCount { get; set; }
    public int ApplicationPermissionCount { get; set; }

    // High Privilege Detection
    public bool HasHighPrivilegePermissions { get; set; }
    public bool HasMailReadWrite { get; set; }
    public bool HasDirectoryReadWriteAll { get; set; }
    public bool HasFilesReadWriteAll { get; set; }
    public bool HasUserReadWriteAll { get; set; }
    public bool HasRoleManagementReadWriteDirectory { get; set; }
    public string? HighPrivilegePermissionsJson { get; set; }

    // Owners
    public int OwnerCount { get; set; }
    public string? OwnersJson { get; set; }

    // Tags
    public string? TagsJson { get; set; }
    public bool IsAppRoleAssignmentRequired { get; set; }

    // Navigation
    public virtual Tenant Tenant { get; set; } = null!;
    public virtual InventorySnapshot Snapshot { get; set; } = null!;
}
