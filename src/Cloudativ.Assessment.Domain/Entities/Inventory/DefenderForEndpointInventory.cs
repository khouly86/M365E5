namespace Cloudativ.Assessment.Domain.Entities.Inventory;

/// <summary>
/// Microsoft Defender for Endpoint inventory and health status.
/// </summary>
public class DefenderForEndpointInventory : BaseEntity
{
    public Guid TenantId { get; set; }
    public Guid SnapshotId { get; set; }

    // Onboarding
    public int OnboardedDeviceCount { get; set; }
    public int TotalManagedDeviceCount { get; set; }
    public double OnboardingCoverage { get; set; }
    public int WindowsOnboarded { get; set; }
    public int MacOsOnboarded { get; set; }
    public int LinuxOnboarded { get; set; }
    public int MobileOnboarded { get; set; }

    // Sensor Health
    public int ActiveSensors { get; set; }
    public int InactiveSensors { get; set; }
    public int MisconfiguredSensors { get; set; }
    public int ImpairedCommunication { get; set; }
    public int NoSensorData { get; set; }

    // Risk Distribution
    public int HighRiskDevices { get; set; }
    public int MediumRiskDevices { get; set; }
    public int LowRiskDevices { get; set; }
    public int NoRiskInfoDevices { get; set; }

    // Features
    public bool TamperProtectionEnabled { get; set; }
    public bool EdrInBlockMode { get; set; }
    public bool NetworkProtectionEnabled { get; set; }
    public bool WebProtectionEnabled { get; set; }
    public bool CloudProtectionEnabled { get; set; }
    public bool PuaProtectionEnabled { get; set; }
    public bool RealTimeProtectionEnabled { get; set; }

    // ASR Rules
    public bool AsrRulesConfigured { get; set; }
    public string? AsrRulesJson { get; set; }
    public int AsrRulesCount { get; set; }
    public int AsrRulesBlockMode { get; set; }
    public int AsrRulesAuditMode { get; set; }

    // TVM (Threat & Vulnerability Management)
    public double? ExposureScore { get; set; }
    public double? SecureScore { get; set; }
    public int VulnerabilityCount { get; set; }
    public int CriticalVulnerabilities { get; set; }
    public int HighVulnerabilities { get; set; }
    public int MediumVulnerabilities { get; set; }
    public int MissingPatches { get; set; }
    public int MissingKbCount { get; set; }
    public string? TopVulnerabilitiesJson { get; set; }

    // Alerts Summary
    public int ActiveAlerts { get; set; }
    public int HighSeverityAlerts { get; set; }
    public int MediumSeverityAlerts { get; set; }
    public int LowSeverityAlerts { get; set; }
    public int InformationalAlerts { get; set; }

    // Navigation
    public virtual Tenant Tenant { get; set; } = null!;
    public virtual InventorySnapshot Snapshot { get; set; } = null!;
}
