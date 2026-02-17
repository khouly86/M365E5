namespace Cloudativ.Assessment.Domain.Entities.Inventory;

/// <summary>
/// Directory role inventory with membership information.
/// </summary>
public class DirectoryRoleInventory : BaseEntity
{
    public Guid TenantId { get; set; }
    public Guid SnapshotId { get; set; }

    public string RoleTemplateId { get; set; } = string.Empty;
    public string RoleId { get; set; } = string.Empty;
    public string DisplayName { get; set; } = string.Empty;
    public string? Description { get; set; }

    // Role Properties
    public bool IsBuiltIn { get; set; }
    public bool IsPrivileged { get; set; }
    public bool IsGlobalAdmin { get; set; }
    public bool IsEnabled { get; set; }

    // Membership Counts
    public int MemberCount { get; set; }
    public int UserMemberCount { get; set; }
    public int ServicePrincipalMemberCount { get; set; }
    public int GroupMemberCount { get; set; }

    // Members Summary
    public string? MembersJson { get; set; }
    public string? UserMembersJson { get; set; }
    public string? ServicePrincipalMembersJson { get; set; }

    // PIM
    public int EligibleMemberCount { get; set; }
    public int ActiveMemberCount { get; set; }
    public bool HasPimConfiguration { get; set; }

    // Navigation
    public virtual Tenant Tenant { get; set; } = null!;
    public virtual InventorySnapshot Snapshot { get; set; } = null!;
}
