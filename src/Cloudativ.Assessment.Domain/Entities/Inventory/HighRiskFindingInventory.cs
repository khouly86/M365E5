namespace Cloudativ.Assessment.Domain.Entities.Inventory;

/// <summary>
/// Auto-generated high-risk findings from inventory analysis.
/// </summary>
public class HighRiskFindingInventory : BaseEntity
{
    public Guid TenantId { get; set; }
    public Guid SnapshotId { get; set; }

    // Finding Identification
    public string FindingType { get; set; } = string.Empty;
    public string FindingCode { get; set; } = string.Empty;
    public string Category { get; set; } = string.Empty;

    // Severity and Impact
    public string Severity { get; set; } = string.Empty;
    public int SeverityOrder { get; set; }
    public string? ImpactDescription { get; set; }
    public double? RiskScore { get; set; }

    // Finding Details
    public string Title { get; set; } = string.Empty;
    public string? Description { get; set; }
    public int AffectedCount { get; set; }
    public string? AffectedResourcesJson { get; set; }
    public string? AffectedResourcesSample { get; set; }

    // Remediation
    public string? Remediation { get; set; }
    public string? RemediationSteps { get; set; }
    public string? RemediationUrl { get; set; }
    public string? ComplianceReferences { get; set; }

    // Detection Context
    public string? DetectionQuery { get; set; }
    public DateTime DetectedAt { get; set; } = DateTime.UtcNow;
    public bool IsNew { get; set; }
    public DateTime? FirstDetectedAt { get; set; }
    public int? DaysOpen { get; set; }

    // Change Tracking
    public int? PreviousAffectedCount { get; set; }
    public string? TrendDirection { get; set; }

    // Status
    public bool IsAcknowledged { get; set; }
    public string? AcknowledgedBy { get; set; }
    public DateTime? AcknowledgedAt { get; set; }
    public string? AcknowledgementNote { get; set; }
    public bool IsExcluded { get; set; }
    public string? ExclusionReason { get; set; }

    // Navigation
    public virtual Tenant Tenant { get; set; } = null!;
    public virtual InventorySnapshot Snapshot { get; set; } = null!;
}

/// <summary>
/// Standard high-risk finding types.
/// </summary>
public static class HighRiskFindingTypes
{
    public const string LegacyAuthInUse = "LEGACY_AUTH_IN_USE";
    public const string UsersWithoutMfa = "USERS_WITHOUT_MFA";
    public const string UsersExcludedFromCa = "USERS_EXCLUDED_FROM_CA";
    public const string ExternalForwardingEnabled = "EXTERNAL_FORWARDING_ENABLED";
    public const string OAuthAppsHighPrivilege = "OAUTH_APPS_HIGH_PRIVILEGE";
    public const string AnonymousSharingEnabled = "ANONYMOUS_SHARING_ENABLED";
    public const string GuestAccessUnrestricted = "GUEST_ACCESS_UNRESTRICTED";
    public const string AdminsWithoutPim = "ADMINS_WITHOUT_PIM";
    public const string GlobalAdminExcessive = "GLOBAL_ADMIN_EXCESSIVE";
    public const string BreakGlassNotConfigured = "BREAK_GLASS_NOT_CONFIGURED";
    public const string DevicesNonCompliant = "DEVICES_NON_COMPLIANT";
    public const string DefenderNotOnboarded = "DEFENDER_NOT_ONBOARDED";
    public const string SensitiveDataNoLabels = "SENSITIVE_DATA_NO_LABELS";
    public const string DlpNotConfigured = "DLP_NOT_CONFIGURED";
    public const string AuditLogDisabled = "AUDIT_LOG_DISABLED";
    public const string CredentialsExpiring = "CREDENTIALS_EXPIRING";
    public const string RiskyUsers = "RISKY_USERS";
    public const string StaleGuestAccounts = "STALE_GUEST_ACCOUNTS";
    public const string OrphanedSites = "ORPHANED_SITES";
    public const string OversharedContent = "OVERSHARED_CONTENT";
    public const string InactiveAdminAccounts = "INACTIVE_ADMIN_ACCOUNTS";
    public const string E5Underutilized = "E5_UNDERUTILIZED";
}

/// <summary>
/// Finding categories.
/// </summary>
public static class HighRiskFindingCategories
{
    public const string Identity = "Identity & Access";
    public const string Authentication = "Authentication";
    public const string Authorization = "Authorization";
    public const string DeviceSecurity = "Device Security";
    public const string DataProtection = "Data Protection";
    public const string EmailSecurity = "Email Security";
    public const string Collaboration = "Collaboration";
    public const string Applications = "Applications";
    public const string Monitoring = "Monitoring & Logging";
    public const string LicenseUtilization = "License Utilization";
}
