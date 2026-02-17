namespace Cloudativ.Assessment.Domain.Entities.Inventory;

/// <summary>
/// Conditional Access policy inventory with conditions and controls.
/// </summary>
public class ConditionalAccessPolicyInventory : BaseEntity
{
    public Guid TenantId { get; set; }
    public Guid SnapshotId { get; set; }

    public string PolicyId { get; set; } = string.Empty;
    public string DisplayName { get; set; } = string.Empty;
    public string? Description { get; set; }
    public string State { get; set; } = string.Empty;
    public DateTime? CreatedDateTime { get; set; }
    public DateTime? ModifiedDateTime { get; set; }

    // User Conditions
    public string? IncludeUsersJson { get; set; }
    public string? ExcludeUsersJson { get; set; }
    public string? IncludeGroupsJson { get; set; }
    public string? ExcludeGroupsJson { get; set; }
    public string? IncludeRolesJson { get; set; }
    public string? ExcludeRolesJson { get; set; }
    public bool IncludesAllUsers { get; set; }
    public bool IncludesGuestUsers { get; set; }
    public int ExcludedUserCount { get; set; }
    public int ExcludedGroupCount { get; set; }

    // Application Conditions
    public string? IncludeApplicationsJson { get; set; }
    public string? ExcludeApplicationsJson { get; set; }
    public bool IncludesAllApps { get; set; }
    public bool IncludesOffice365 { get; set; }
    public int ExcludedAppCount { get; set; }

    // Client App Types
    public string? ClientAppTypesJson { get; set; }
    public bool IncludesBrowser { get; set; }
    public bool IncludesMobileApps { get; set; }
    public bool IncludesLegacyClients { get; set; }

    // Platform Conditions
    public string? PlatformsJson { get; set; }
    public bool IncludesAllPlatforms { get; set; }

    // Location Conditions
    public string? IncludeLocationsJson { get; set; }
    public string? ExcludeLocationsJson { get; set; }
    public bool IncludesAllLocations { get; set; }
    public bool ExcludesTrustedLocations { get; set; }

    // Device Conditions
    public string? DeviceStateJson { get; set; }
    public string? DeviceFilterJson { get; set; }

    // Risk Conditions
    public string? SignInRiskLevels { get; set; }
    public string? UserRiskLevels { get; set; }

    // Grant Controls
    public bool RequiresMfa { get; set; }
    public bool RequiresCompliantDevice { get; set; }
    public bool RequiresHybridAzureAdJoin { get; set; }
    public bool RequiresApprovedApp { get; set; }
    public bool RequiresAppProtection { get; set; }
    public bool RequiresPasswordChange { get; set; }
    public bool BlocksAccess { get; set; }
    public bool BlocksLegacyAuth { get; set; }
    public string? GrantControlsJson { get; set; }
    public string? GrantControlOperator { get; set; }

    // Session Controls
    public string? SessionControlsJson { get; set; }
    public bool HasSignInFrequency { get; set; }
    public string? SignInFrequencyValue { get; set; }
    public bool HasPersistentBrowser { get; set; }
    public bool HasCloudAppSecurity { get; set; }

    // Navigation
    public virtual Tenant Tenant { get; set; } = null!;
    public virtual InventorySnapshot Snapshot { get; set; } = null!;
}
