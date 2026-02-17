namespace Cloudativ.Assessment.Domain.Entities.Inventory;

/// <summary>
/// Microsoft Defender for Cloud Apps (MCAS) inventory.
/// </summary>
public class DefenderForCloudAppsInventory : BaseEntity
{
    public Guid TenantId { get; set; }
    public Guid SnapshotId { get; set; }

    // Connected Apps
    public int ConnectedAppCount { get; set; }
    public string? ConnectedAppsJson { get; set; }
    public bool Office365Connected { get; set; }
    public bool AzureConnected { get; set; }
    public bool AwsConnected { get; set; }
    public bool GcpConnected { get; set; }

    // OAuth Apps
    public int OAuthAppCount { get; set; }
    public int HighRiskOAuthApps { get; set; }
    public int MediumRiskOAuthApps { get; set; }
    public int LowRiskOAuthApps { get; set; }
    public string? OAuthAppsJson { get; set; }

    // App Governance
    public bool AppGovernanceEnabled { get; set; }
    public string? AppGovernancePoliciesJson { get; set; }
    public int AppGovernancePolicyCount { get; set; }
    public int AppGovernanceAlerts { get; set; }

    // Policies
    public int ActivityPolicyCount { get; set; }
    public int AnomalyPolicyCount { get; set; }
    public int SessionPolicyCount { get; set; }
    public int AccessPolicyCount { get; set; }
    public int FilePolicyCount { get; set; }
    public string? PoliciesJson { get; set; }
    public int EnabledPolicies { get; set; }
    public int DisabledPolicies { get; set; }

    // Shadow IT Discovery
    public bool CloudDiscoveryEnabled { get; set; }
    public int DiscoveredAppCount { get; set; }
    public int SanctionedApps { get; set; }
    public int UnsanctionedApps { get; set; }
    public int MonitoredApps { get; set; }
    public string? TopDiscoveredAppsJson { get; set; }

    // Alerts
    public int OpenAlerts { get; set; }
    public int HighSeverityAlerts { get; set; }
    public int MediumSeverityAlerts { get; set; }
    public int LowSeverityAlerts { get; set; }

    // Conditional Access App Control
    public bool SessionControlEnabled { get; set; }
    public int SessionControlledApps { get; set; }

    // Navigation
    public virtual Tenant Tenant { get; set; } = null!;
    public virtual InventorySnapshot Snapshot { get; set; } = null!;
}
