namespace Cloudativ.Assessment.Domain.Entities.Inventory;

/// <summary>
/// Device inventory from Intune managed devices.
/// </summary>
public class DeviceInventory : BaseEntity
{
    public Guid TenantId { get; set; }
    public Guid SnapshotId { get; set; }

    public string DeviceId { get; set; } = string.Empty;
    public string AzureAdDeviceId { get; set; } = string.Empty;
    public string DeviceName { get; set; } = string.Empty;
    public string OperatingSystem { get; set; } = string.Empty;
    public string? OsVersion { get; set; }
    public string? OsBuildNumber { get; set; }
    public string? Manufacturer { get; set; }
    public string? Model { get; set; }
    public string? SerialNumber { get; set; }
    public string? Imei { get; set; }

    // Ownership & Enrollment
    public string OwnerType { get; set; } = string.Empty;
    public string? ManagedDeviceOwnerType { get; set; }
    public string? EnrollmentType { get; set; }
    public string? DeviceEnrollmentType { get; set; }
    public DateTime? EnrolledDateTime { get; set; }
    public DateTime? LastSyncDateTime { get; set; }
    public string? ManagementAgent { get; set; }
    public string? ManagementState { get; set; }

    // Compliance
    public string ComplianceState { get; set; } = string.Empty;
    public string? ComplianceGracePeriodExpirationDateTime { get; set; }
    public bool IsManaged { get; set; }
    public bool IsSupervised { get; set; }
    public bool IsEncrypted { get; set; }
    public bool IsJailBroken { get; set; }
    public string? JailBroken { get; set; }

    // Security
    public bool HasDefenderForEndpoint { get; set; }
    public string? DefenderHealthState { get; set; }
    public string? RiskScore { get; set; }
    public string? ExposureLevel { get; set; }
    public string? DeviceThreatLevel { get; set; }

    // BitLocker/FileVault
    public bool EncryptionReportedState { get; set; }
    public bool RecoveryKeyEscrowed { get; set; }
    public string? EncryptionState { get; set; }

    // User
    public string? PrimaryUserUpn { get; set; }
    public string? PrimaryUserDisplayName { get; set; }
    public string? PrimaryUserId { get; set; }
    public string? UserDisplayName { get; set; }
    public string? EmailAddress { get; set; }

    // Azure AD Join Status
    public bool IsAzureAdRegistered { get; set; }
    public bool IsAzureAdJoined { get; set; }
    public bool IsHybridAzureAdJoined { get; set; }
    public string? TrustType { get; set; }
    public string? DeviceRegistrationState { get; set; }

    // Configuration
    public string? ConfigurationManagerClientEnabled { get; set; }
    public string? AutopilotEnrolled { get; set; }
    public bool RequireUserEnrollmentApproval { get; set; }

    // Storage
    public long? TotalStorageSpaceInBytes { get; set; }
    public long? FreeStorageSpaceInBytes { get; set; }

    // Categories & Notes
    public string? DeviceCategory { get; set; }
    public string? Notes { get; set; }

    // Navigation
    public virtual Tenant Tenant { get; set; } = null!;
    public virtual InventorySnapshot Snapshot { get; set; } = null!;
}
