using Cloudativ.Assessment.Domain.Enums;

namespace Cloudativ.Assessment.Domain.Entities.Inventory;

/// <summary>
/// User inventory with authentication, licensing, and risk information.
/// </summary>
public class UserInventory : BaseEntity
{
    public Guid TenantId { get; set; }
    public Guid SnapshotId { get; set; }

    // Core Identity
    public string ObjectId { get; set; } = string.Empty;
    public string UserPrincipalName { get; set; } = string.Empty;
    public string? DisplayName { get; set; }
    public string? Mail { get; set; }
    public string UserType { get; set; } = "Member";
    public bool AccountEnabled { get; set; }

    // Creation & Sign-in
    public DateTime? CreatedDateTime { get; set; }
    public DateTime? LastSignInDateTime { get; set; }
    public DateTime? LastNonInteractiveSignInDateTime { get; set; }
    public bool IsSignInBlocked { get; set; }

    // MFA & Authentication
    public bool IsMfaRegistered { get; set; }
    public bool IsMfaCapable { get; set; }
    public bool IsPasswordlessCapable { get; set; }
    public string? AuthMethodsJson { get; set; }
    public bool? HasPerUserMfa { get; set; }
    public bool IsSsprRegistered { get; set; }
    public bool IsSsprEnabled { get; set; }

    // Licensing - Legacy fields (kept for backwards compatibility)
    public string? AssignedLicensesJson { get; set; }
    public bool HasE5License { get; set; }
    public bool HasE3License { get; set; }
    public int LicenseCount { get; set; }

    // Licensing - New multi-license categorization
    public LicenseCategory PrimaryLicenseCategory { get; set; } = LicenseCategory.Unknown;
    public string? AllLicenseCategoriesJson { get; set; }
    public bool HasBusinessPremium { get; set; }
    public bool HasBusinessStandard { get; set; }
    public bool HasBusinessBasic { get; set; }
    public bool HasFrontlineLicense { get; set; }
    public bool HasEducationLicense { get; set; }
    public bool HasGovernmentLicense { get; set; }
    public string? PrimaryLicenseTierGroup { get; set; }

    // Risk
    public string? RiskLevel { get; set; }
    public string? RiskState { get; set; }
    public DateTime? RiskLastUpdated { get; set; }
    public string? RiskDetail { get; set; }

    // Administrative
    public bool IsPrivileged { get; set; }
    public bool IsGlobalAdmin { get; set; }
    public string? AssignedRolesJson { get; set; }
    public bool IsBreakGlassAccount { get; set; }
    public int DirectRoleCount { get; set; }

    // Department/Location
    public string? Department { get; set; }
    public string? JobTitle { get; set; }
    public string? UsageLocation { get; set; }
    public string? Country { get; set; }
    public string? OfficeLocation { get; set; }
    public string? CompanyName { get; set; }

    // On-Premises Sync
    public bool OnPremisesSyncEnabled { get; set; }
    public DateTime? OnPremisesLastSyncDateTime { get; set; }
    public string? OnPremisesDomainName { get; set; }
    public string? OnPremisesSamAccountName { get; set; }

    // Manager
    public string? ManagerId { get; set; }
    public string? ManagerDisplayName { get; set; }

    // Navigation
    public virtual Tenant Tenant { get; set; } = null!;
    public virtual InventorySnapshot Snapshot { get; set; } = null!;
}
