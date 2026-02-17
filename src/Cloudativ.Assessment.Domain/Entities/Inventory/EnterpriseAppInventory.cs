namespace Cloudativ.Assessment.Domain.Entities.Inventory;

/// <summary>
/// Enterprise application (service principal) inventory with permissions and credentials.
/// </summary>
public class EnterpriseAppInventory : BaseEntity
{
    public Guid TenantId { get; set; }
    public Guid SnapshotId { get; set; }

    public string ObjectId { get; set; } = string.Empty;
    public string AppId { get; set; } = string.Empty;
    public string DisplayName { get; set; } = string.Empty;
    public string? PublisherName { get; set; }
    public bool AccountEnabled { get; set; }
    public DateTime? CreatedDateTime { get; set; }

    // Type & Publisher
    public bool IsMicrosoftApp { get; set; }
    public bool IsVerifiedPublisher { get; set; }
    public string? VerifiedPublisherName { get; set; }
    public string? AppOwnerOrganizationId { get; set; }
    public string? ServicePrincipalType { get; set; }
    public string? SignInAudience { get; set; }

    // Users & Sign-ins
    public int UserAssignmentCount { get; set; }
    public int GroupAssignmentCount { get; set; }
    public DateTime? LastSignInDateTime { get; set; }
    public int Last30DaysSignIns { get; set; }

    // Permissions - Delegated
    public string? DelegatedPermissionsJson { get; set; }
    public int DelegatedPermissionCount { get; set; }
    public string? DelegatedPermissionConsentType { get; set; }

    // Permissions - Application
    public string? ApplicationPermissionsJson { get; set; }
    public int ApplicationPermissionCount { get; set; }

    // High Risk Permissions Detection
    public bool HasHighPrivilegePermissions { get; set; }
    public bool HasMailReadWrite { get; set; }
    public bool HasMailSend { get; set; }
    public bool HasDirectoryReadWriteAll { get; set; }
    public bool HasFilesReadWriteAll { get; set; }
    public bool HasUserReadWriteAll { get; set; }
    public bool HasGroupReadWriteAll { get; set; }
    public bool HasRoleManagementReadWriteDirectory { get; set; }
    public bool HasApplicationReadWriteAll { get; set; }
    public string? HighRiskPermissionsJson { get; set; }

    // Credentials - Password
    public int PasswordCredentialCount { get; set; }
    public DateTime? NextPasswordExpiration { get; set; }
    public bool HasExpiredPasswords { get; set; }
    public bool HasExpiringPasswords { get; set; }
    public string? PasswordCredentialsJson { get; set; }

    // Credentials - Certificates
    public int CertificateCredentialCount { get; set; }
    public DateTime? NextCertificateExpiration { get; set; }
    public bool HasExpiredCertificates { get; set; }
    public bool HasExpiringCertificates { get; set; }
    public string? CertificateCredentialsJson { get; set; }

    // Combined Credential Status
    public DateTime? NextCredentialExpiration { get; set; }
    public bool HasExpiredCredentials { get; set; }
    public bool HasExpiringCredentials { get; set; }
    public int DaysUntilCredentialExpiration { get; set; }

    // Owners
    public int OwnerCount { get; set; }
    public string? OwnersJson { get; set; }

    // Tags & Homepage
    public string? TagsJson { get; set; }
    public string? Homepage { get; set; }
    public bool IsAppRoleAssignmentRequired { get; set; }

    // Notes
    public string? Notes { get; set; }

    // Navigation
    public virtual Tenant Tenant { get; set; } = null!;
    public virtual InventorySnapshot Snapshot { get; set; } = null!;
}
