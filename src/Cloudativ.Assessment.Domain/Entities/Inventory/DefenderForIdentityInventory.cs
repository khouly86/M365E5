namespace Cloudativ.Assessment.Domain.Entities.Inventory;

/// <summary>
/// Microsoft Defender for Identity inventory and sensor health.
/// </summary>
public class DefenderForIdentityInventory : BaseEntity
{
    public Guid TenantId { get; set; }
    public Guid SnapshotId { get; set; }

    // Configuration
    public bool IsConfigured { get; set; }
    public bool IsLicensed { get; set; }
    public string? WorkspaceId { get; set; }

    // Sensors
    public int SensorCount { get; set; }
    public int HealthySensors { get; set; }
    public int UnhealthySensors { get; set; }
    public int OfflineSensors { get; set; }
    public string? SensorsJson { get; set; }

    // Domain Controller Coverage
    public int DomainControllersCovered { get; set; }
    public int TotalDomainControllers { get; set; }
    public double CoveragePercentage { get; set; }

    // Health Issues
    public int OpenHealthIssues { get; set; }
    public int HighSeverityHealthIssues { get; set; }
    public int MediumSeverityHealthIssues { get; set; }
    public int LowSeverityHealthIssues { get; set; }
    public string? HealthIssuesJson { get; set; }

    // Recent Alerts (summary)
    public int HighSeverityAlerts { get; set; }
    public int MediumSeverityAlerts { get; set; }
    public int LowSeverityAlerts { get; set; }
    public int Last30DaysAlerts { get; set; }

    // Detection Configuration
    public bool HoneytokenAccountsConfigured { get; set; }
    public int HoneytokenAccountCount { get; set; }
    public bool SensitiveGroupsConfigured { get; set; }

    // Navigation
    public virtual Tenant Tenant { get; set; } = null!;
    public virtual InventorySnapshot Snapshot { get; set; } = null!;
}
