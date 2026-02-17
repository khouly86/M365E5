namespace Cloudativ.Assessment.Domain.Entities.Inventory;

/// <summary>
/// Group inventory including membership and settings.
/// </summary>
public class GroupInventory : BaseEntity
{
    public Guid TenantId { get; set; }
    public Guid SnapshotId { get; set; }

    public string ObjectId { get; set; } = string.Empty;
    public string DisplayName { get; set; } = string.Empty;
    public string? Description { get; set; }
    public string? Mail { get; set; }
    public string? MailNickname { get; set; }

    // Group Type
    public string GroupType { get; set; } = string.Empty;
    public bool IsSecurityGroup { get; set; }
    public bool IsMicrosoft365Group { get; set; }
    public bool IsMailEnabled { get; set; }
    public bool IsDistributionList { get; set; }
    public bool IsUnifiedGroup { get; set; }

    // Membership
    public bool IsDynamicMembership { get; set; }
    public string? MembershipRule { get; set; }
    public string? MembershipRuleProcessingState { get; set; }
    public int MemberCount { get; set; }
    public int OwnerCount { get; set; }
    public bool HasExternalMembers { get; set; }
    public int ExternalMemberCount { get; set; }

    // Settings
    public bool IsRoleAssignable { get; set; }
    public string? Visibility { get; set; }
    public bool IsAssignableToRole { get; set; }

    // Classification
    public string? Classification { get; set; }
    public string? SensitivityLabel { get; set; }

    // Teams
    public bool HasTeam { get; set; }
    public string? TeamId { get; set; }

    // Timestamps
    public DateTime? CreatedDateTime { get; set; }
    public DateTime? RenewedDateTime { get; set; }
    public DateTime? ExpirationDateTime { get; set; }

    // On-Premises
    public bool OnPremisesSyncEnabled { get; set; }
    public string? OnPremisesSamAccountName { get; set; }

    // Navigation
    public virtual Tenant Tenant { get; set; } = null!;
    public virtual InventorySnapshot Snapshot { get; set; } = null!;
}
