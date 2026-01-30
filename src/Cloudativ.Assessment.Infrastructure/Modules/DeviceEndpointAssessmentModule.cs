using System.Text.Json;
using Cloudativ.Assessment.Application.Services;
using Cloudativ.Assessment.Domain.Enums;
using Cloudativ.Assessment.Domain.Interfaces;
using Microsoft.Extensions.Logging;

namespace Cloudativ.Assessment.Infrastructure.Modules;

public class DeviceEndpointAssessmentModule : BaseAssessmentModule
{
    private readonly IScoringService _scoringService;

    public DeviceEndpointAssessmentModule(ILogger<DeviceEndpointAssessmentModule> logger, IScoringService scoringService)
        : base(logger)
    {
        _scoringService = scoringService;
    }

    public override AssessmentDomain Domain => AssessmentDomain.DeviceEndpoint;
    public override string DisplayName => "Device & Endpoint Security";
    public override string Description => "Assesses Intune configuration, device compliance policies, BitLocker encryption, and endpoint management.";

    public override IReadOnlyList<string> RequiredPermissions => new[]
    {
        "DeviceManagementConfiguration.Read.All",
        "DeviceManagementManagedDevices.Read.All",
        "DeviceManagementServiceConfig.Read.All",
        "Device.Read.All"
    };

    public override async Task<CollectionResult> CollectAsync(IGraphClientWrapper graphClient, CancellationToken cancellationToken = default)
    {
        var rawData = new Dictionary<string, object?>();
        var warnings = new List<string>();
        var unavailableEndpoints = new List<string>();

        try
        {
            // 1. Collect Device Compliance Policies
            _logger.LogInformation("Collecting device compliance policies...");
            try
            {
                var compliancePoliciesJson = await graphClient.GetRawJsonAsync(
                    "deviceManagement/deviceCompliancePolicies",
                    cancellationToken);
                rawData["compliancePolicies"] = compliancePoliciesJson;
            }
            catch (Exception ex)
            {
                warnings.Add($"Failed to collect compliance policies: {ex.Message}");
                unavailableEndpoints.Add("compliancePolicies");
            }

            // 2. Collect Device Configuration Profiles
            _logger.LogInformation("Collecting device configuration profiles...");
            try
            {
                var configProfilesJson = await graphClient.GetRawJsonAsync(
                    "deviceManagement/deviceConfigurations",
                    cancellationToken);
                rawData["configurationProfiles"] = configProfilesJson;
            }
            catch (Exception ex)
            {
                warnings.Add($"Failed to collect configuration profiles: {ex.Message}");
            }

            // 3. Collect Managed Devices
            _logger.LogInformation("Collecting managed devices...");
            try
            {
                var managedDevicesJson = await graphClient.GetRawJsonAsync(
                    "deviceManagement/managedDevices?$select=id,deviceName,operatingSystem,osVersion,complianceState,isEncrypted,managementAgent,enrolledDateTime,lastSyncDateTime,deviceEnrollmentType&$top=999",
                    cancellationToken);
                rawData["managedDevices"] = managedDevicesJson;
            }
            catch (Exception ex)
            {
                warnings.Add($"Failed to collect managed devices: {ex.Message}");
                unavailableEndpoints.Add("managedDevices");
            }

            // 4. Collect Conditional Access Policies (device-related)
            _logger.LogInformation("Collecting conditional access policies...");
            try
            {
                var caPoliciesJson = await graphClient.GetRawJsonAsync(
                    "identity/conditionalAccess/policies",
                    cancellationToken);
                rawData["conditionalAccessPolicies"] = caPoliciesJson;
            }
            catch (Exception ex)
            {
                warnings.Add($"Failed to collect conditional access policies: {ex.Message}");
            }

            // 5. Collect Windows AutoPilot Profiles
            _logger.LogInformation("Collecting Windows AutoPilot profiles...");
            try
            {
                var autopilotProfilesJson = await graphClient.GetRawJsonAsync(
                    "deviceManagement/windowsAutopilotDeploymentProfiles",
                    cancellationToken);
                rawData["autopilotProfiles"] = autopilotProfilesJson;
            }
            catch (Exception ex)
            {
                warnings.Add($"Failed to collect AutoPilot profiles: {ex.Message}");
            }

            // 6. Collect Windows Information Protection Policies
            _logger.LogInformation("Collecting Windows Information Protection policies...");
            try
            {
                var wipPoliciesJson = await graphClient.GetRawJsonAsync(
                    "deviceAppManagement/windowsInformationProtectionPolicies",
                    cancellationToken);
                rawData["wipPolicies"] = wipPoliciesJson;
            }
            catch (Exception ex)
            {
                warnings.Add($"Failed to collect WIP policies: {ex.Message}");
            }

            // 7. Collect Mobile App Protection Policies
            _logger.LogInformation("Collecting mobile app protection policies...");
            try
            {
                var appProtectionPoliciesJson = await graphClient.GetRawJsonAsync(
                    "deviceAppManagement/managedAppPolicies",
                    cancellationToken);
                rawData["appProtectionPolicies"] = appProtectionPoliciesJson;
            }
            catch (Exception ex)
            {
                warnings.Add($"Failed to collect app protection policies: {ex.Message}");
            }

            // 8. Collect Device Enrollment Restrictions
            _logger.LogInformation("Collecting device enrollment restrictions...");
            try
            {
                var enrollmentRestrictionsJson = await graphClient.GetRawJsonAsync(
                    "deviceManagement/deviceEnrollmentConfigurations",
                    cancellationToken);
                rawData["enrollmentRestrictions"] = enrollmentRestrictionsJson;
            }
            catch (Exception ex)
            {
                warnings.Add($"Failed to collect enrollment restrictions: {ex.Message}");
            }

            // 9. Collect Security Baselines
            _logger.LogInformation("Collecting security baselines...");
            try
            {
                var securityBaselinesJson = await graphClient.GetRawJsonAsync(
                    "deviceManagement/intents",
                    cancellationToken);
                rawData["securityBaselines"] = securityBaselinesJson;
            }
            catch (Exception ex)
            {
                warnings.Add($"Failed to collect security baselines: {ex.Message}");
            }

            // 10. Collect BitLocker Recovery Keys Status
            _logger.LogInformation("Collecting BitLocker information...");
            try
            {
                var bitlockerKeysJson = await graphClient.GetRawJsonAsync(
                    "informationProtection/bitlocker/recoveryKeys?$select=id,createdDateTime,deviceId",
                    cancellationToken);
                rawData["bitlockerKeys"] = bitlockerKeysJson;
            }
            catch (Exception ex)
            {
                warnings.Add($"Failed to collect BitLocker keys: {ex.Message}");
            }

            return new CollectionResult
            {
                Domain = Domain,
                Success = true,
                RawData = rawData,
                Warnings = warnings,
                UnavailableEndpoints = unavailableEndpoints
            };
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to collect Device & Endpoint data");
            return CreateErrorResult($"Collection failed: {ex.Message}");
        }
    }

    public override NormalizedFindings Normalize(CollectionResult rawData)
    {
        var findings = new NormalizedFindings
        {
            Domain = Domain,
            Metrics = new Dictionary<string, object?>()
        };

        if (!rawData.Success)
        {
            findings.Summary.Add($"Collection failed: {rawData.ErrorMessage}");
            return findings;
        }

        try
        {
            // Parse collected data
            var compliancePolicies = ParseJsonCollection<CompliancePolicyInfo>(
                rawData.RawData.GetValueOrDefault("compliancePolicies") as string);
            var configProfiles = ParseJsonCollection<ConfigProfileInfo>(
                rawData.RawData.GetValueOrDefault("configurationProfiles") as string);
            var managedDevices = ParseJsonCollection<ManagedDeviceInfo>(
                rawData.RawData.GetValueOrDefault("managedDevices") as string);
            var conditionalAccessPolicies = ParseJsonCollection<CaPolicyInfo>(
                rawData.RawData.GetValueOrDefault("conditionalAccessPolicies") as string);
            var autopilotProfiles = ParseJsonCollection<AutopilotProfileInfo>(
                rawData.RawData.GetValueOrDefault("autopilotProfiles") as string);
            var appProtectionPolicies = ParseJsonCollection<AppProtectionPolicyInfo>(
                rawData.RawData.GetValueOrDefault("appProtectionPolicies") as string);
            var securityBaselines = ParseJsonCollection<SecurityBaselineInfo>(
                rawData.RawData.GetValueOrDefault("securityBaselines") as string);

            // Calculate metrics
            var totalDevices = managedDevices.Count;
            var compliantDevices = managedDevices.Count(d => d.ComplianceState == "compliant");
            var nonCompliantDevices = managedDevices.Count(d => d.ComplianceState == "noncompliant");
            var encryptedDevices = managedDevices.Count(d => d.IsEncrypted == true);
            var windowsDevices = managedDevices.Count(d => d.OperatingSystem?.Contains("Windows", StringComparison.OrdinalIgnoreCase) == true);
            var iosDevices = managedDevices.Count(d => d.OperatingSystem?.Contains("iOS", StringComparison.OrdinalIgnoreCase) == true);
            var androidDevices = managedDevices.Count(d => d.OperatingSystem?.Contains("Android", StringComparison.OrdinalIgnoreCase) == true);
            var macDevices = managedDevices.Count(d => d.OperatingSystem?.Contains("macOS", StringComparison.OrdinalIgnoreCase) == true);

            findings.Metrics["totalManagedDevices"] = totalDevices;
            findings.Metrics["compliantDevices"] = compliantDevices;
            findings.Metrics["nonCompliantDevices"] = nonCompliantDevices;
            findings.Metrics["encryptedDevices"] = encryptedDevices;
            findings.Metrics["windowsDevices"] = windowsDevices;
            findings.Metrics["iosDevices"] = iosDevices;
            findings.Metrics["androidDevices"] = androidDevices;
            findings.Metrics["macDevices"] = macDevices;
            findings.Metrics["compliancePoliciesCount"] = compliancePolicies.Count;
            findings.Metrics["configProfilesCount"] = configProfiles.Count;

            // Check 1: Device Compliance Policies
            if (compliancePolicies.Count == 0)
            {
                findings.Findings.Add(CreateFinding(
                    "DEV-001",
                    "No Compliance Policies",
                    "No device compliance policies configured",
                    "Device compliance policies are essential for ensuring devices meet security requirements.",
                    Severity.Critical,
                    false,
                    "Compliance",
                    remediation: "Create device compliance policies defining minimum security requirements (encryption, PIN, OS version).",
                    references: "https://learn.microsoft.com/en-us/mem/intune/protect/device-compliance-get-started"
                ));
            }
            else
            {
                findings.Findings.Add(CreateFinding(
                    "DEV-001",
                    "Compliance Policies Configured",
                    $"{compliancePolicies.Count} device compliance policies configured",
                    "Device compliance policies are in place to enforce security requirements.",
                    Severity.Informational,
                    true,
                    "Compliance"
                ));
            }

            // Check 2: Device Compliance Rate
            if (totalDevices > 0)
            {
                var complianceRate = (compliantDevices * 100.0 / totalDevices);
                if (complianceRate < 80)
                {
                    findings.Findings.Add(CreateFinding(
                        "DEV-002",
                        "Low Compliance Rate",
                        $"Only {complianceRate:F1}% of devices are compliant",
                        $"{nonCompliantDevices} out of {totalDevices} devices are non-compliant with security policies.",
                        Severity.High,
                        false,
                        "Compliance",
                        remediation: "Investigate non-compliant devices and remediate compliance issues or block access.",
                        affectedResources: managedDevices
                            .Where(d => d.ComplianceState == "noncompliant")
                            .Select(d => d.DeviceName ?? "Unknown")
                            .Take(20)
                            .ToList()
                    ));
                }
                else if (complianceRate < 95)
                {
                    findings.Findings.Add(CreateFinding(
                        "DEV-002",
                        "Moderate Compliance Rate",
                        $"{complianceRate:F1}% of devices are compliant",
                        $"{nonCompliantDevices} devices are non-compliant.",
                        Severity.Medium,
                        false,
                        "Compliance",
                        remediation: "Review and remediate non-compliant devices."
                    ));
                }
                else
                {
                    findings.Findings.Add(CreateFinding(
                        "DEV-002",
                        "Good Compliance Rate",
                        $"{complianceRate:F1}% of devices are compliant",
                        "Device compliance rate meets security standards.",
                        Severity.Informational,
                        true,
                        "Compliance"
                    ));
                }
            }

            // Check 3: Device Encryption
            if (totalDevices > 0)
            {
                var encryptionRate = (encryptedDevices * 100.0 / totalDevices);
                if (encryptionRate < 90)
                {
                    findings.Findings.Add(CreateFinding(
                        "DEV-003",
                        "Low Encryption Rate",
                        $"Only {encryptionRate:F1}% of devices are encrypted",
                        $"{totalDevices - encryptedDevices} devices do not have encryption enabled.",
                        Severity.High,
                        false,
                        "Encryption",
                        remediation: "Enable BitLocker for Windows and FileVault for Mac devices. Enable encryption for mobile devices.",
                        references: "https://learn.microsoft.com/en-us/mem/intune/protect/encrypt-devices"
                    ));
                }
                else
                {
                    findings.Findings.Add(CreateFinding(
                        "DEV-003",
                        "Device Encryption",
                        $"{encryptionRate:F1}% of devices are encrypted",
                        "Device encryption is enabled on most managed devices.",
                        Severity.Informational,
                        true,
                        "Encryption"
                    ));
                }
            }

            // Check 4: Conditional Access - Require Compliant Device
            var requiresCompliantDevice = conditionalAccessPolicies.Any(p =>
                p.State == "enabled" &&
                p.GrantControls?.BuiltInControls?.Contains("compliantDevice") == true);

            if (!requiresCompliantDevice)
            {
                findings.Findings.Add(CreateFinding(
                    "DEV-004",
                    "No Compliant Device Requirement",
                    "No Conditional Access policy requires compliant devices",
                    "Users can access resources from non-compliant devices.",
                    Severity.High,
                    false,
                    "Conditional Access",
                    remediation: "Create a Conditional Access policy requiring device compliance for resource access.",
                    references: "https://learn.microsoft.com/en-us/azure/active-directory/conditional-access/require-managed-devices"
                ));
            }
            else
            {
                findings.Findings.Add(CreateFinding(
                    "DEV-004",
                    "Compliant Device Required",
                    "Conditional Access requires compliant devices",
                    "A policy enforces device compliance for resource access.",
                    Severity.Informational,
                    true,
                    "Conditional Access"
                ));
            }

            // Check 5: Mobile App Protection
            if (appProtectionPolicies.Count == 0)
            {
                findings.Findings.Add(CreateFinding(
                    "DEV-005",
                    "No App Protection Policies",
                    "No mobile app protection policies configured",
                    "App protection policies protect corporate data on mobile devices (MAM without enrollment).",
                    Severity.Medium,
                    false,
                    "Mobile Security",
                    remediation: "Create app protection policies for iOS and Android to protect corporate data.",
                    references: "https://learn.microsoft.com/en-us/mem/intune/apps/app-protection-policy"
                ));
            }
            else
            {
                findings.Findings.Add(CreateFinding(
                    "DEV-005",
                    "App Protection Configured",
                    $"{appProtectionPolicies.Count} app protection policies configured",
                    "Mobile app protection policies are in place.",
                    Severity.Informational,
                    true,
                    "Mobile Security"
                ));
            }

            // Check 6: Windows AutoPilot
            if (windowsDevices > 0 && autopilotProfiles.Count == 0)
            {
                findings.Findings.Add(CreateFinding(
                    "DEV-006",
                    "No AutoPilot Profiles",
                    "Windows AutoPilot is not configured",
                    "AutoPilot enables zero-touch deployment and ensures devices are enrolled correctly.",
                    Severity.Low,
                    false,
                    "Provisioning",
                    remediation: "Configure Windows AutoPilot profiles for secure device provisioning."
                ));
            }
            else if (autopilotProfiles.Count > 0)
            {
                findings.Findings.Add(CreateFinding(
                    "DEV-006",
                    "AutoPilot Configured",
                    $"{autopilotProfiles.Count} Windows AutoPilot profiles configured",
                    "Zero-touch deployment is available for Windows devices.",
                    Severity.Informational,
                    true,
                    "Provisioning"
                ));
            }

            // Check 7: Security Baselines
            if (securityBaselines.Count == 0)
            {
                findings.Findings.Add(CreateFinding(
                    "DEV-007",
                    "No Security Baselines",
                    "No security baselines deployed",
                    "Security baselines provide a recommended configuration for Windows devices.",
                    Severity.Medium,
                    false,
                    "Security Configuration",
                    remediation: "Deploy Microsoft security baselines to ensure consistent security configuration.",
                    references: "https://learn.microsoft.com/en-us/mem/intune/protect/security-baselines"
                ));
            }
            else
            {
                findings.Findings.Add(CreateFinding(
                    "DEV-007",
                    "Security Baselines Deployed",
                    $"{securityBaselines.Count} security baselines deployed",
                    "Security baselines are configured for device hardening.",
                    Severity.Informational,
                    true,
                    "Security Configuration"
                ));
            }

            // Check 8: Stale Devices
            var staleThreshold = DateTime.UtcNow.AddDays(-90);
            var staleDevices = managedDevices
                .Where(d => d.LastSyncDateTime.HasValue && d.LastSyncDateTime < staleThreshold)
                .ToList();

            if (staleDevices.Count > 0)
            {
                findings.Findings.Add(CreateFinding(
                    "DEV-008",
                    "Stale Devices Detected",
                    $"{staleDevices.Count} devices haven't synced in 90+ days",
                    "Devices that haven't synced may be lost, stolen, or abandoned.",
                    Severity.Medium,
                    false,
                    "Device Hygiene",
                    remediation: "Review stale devices and retire or wipe as appropriate.",
                    affectedResources: staleDevices.Select(d => d.DeviceName ?? "Unknown").Take(20).ToList()
                ));
            }
            else if (totalDevices > 0)
            {
                findings.Findings.Add(CreateFinding(
                    "DEV-008",
                    "Active Device Fleet",
                    "All devices have synced within 90 days",
                    "Managed devices are actively checking in.",
                    Severity.Informational,
                    true,
                    "Device Hygiene"
                ));
            }

            // Generate summary
            findings.Summary.Add($"Managed Devices: {totalDevices} ({compliantDevices} compliant, {nonCompliantDevices} non-compliant)");
            findings.Summary.Add($"Platform Distribution: Windows: {windowsDevices}, iOS: {iosDevices}, Android: {androidDevices}, Mac: {macDevices}");
            findings.Summary.Add($"Encrypted Devices: {encryptedDevices}");
            findings.Summary.Add($"Compliance Policies: {compliancePolicies.Count}, Config Profiles: {configProfiles.Count}");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error normalizing Device & Endpoint findings");
            findings.Summary.Add($"Error during normalization: {ex.Message}");
        }

        return findings;
    }

    public override DomainScore Score(NormalizedFindings findings)
    {
        return _scoringService.CalculateDomainScore(findings);
    }

    #region Helper Classes and Methods

    private List<T> ParseJsonCollection<T>(string? json) where T : class, new()
    {
        if (string.IsNullOrEmpty(json))
            return new List<T>();

        try
        {
            using var doc = JsonDocument.Parse(json);
            var results = new List<T>();

            if (doc.RootElement.TryGetProperty("value", out var valueElement))
            {
                foreach (var item in valueElement.EnumerateArray())
                {
                    var obj = JsonSerializer.Deserialize<T>(item.GetRawText(), new JsonSerializerOptions
                    {
                        PropertyNameCaseInsensitive = true
                    });
                    if (obj != null)
                        results.Add(obj);
                }
            }

            return results;
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Failed to parse JSON collection");
            return new List<T>();
        }
    }

    private class CompliancePolicyInfo
    {
        public string? Id { get; set; }
        public string? DisplayName { get; set; }
        public string? Description { get; set; }
        public DateTime? CreatedDateTime { get; set; }
    }

    private class ConfigProfileInfo
    {
        public string? Id { get; set; }
        public string? DisplayName { get; set; }
        public string? Description { get; set; }
        public string? OdataType { get; set; }
    }

    private class ManagedDeviceInfo
    {
        public string? Id { get; set; }
        public string? DeviceName { get; set; }
        public string? OperatingSystem { get; set; }
        public string? OsVersion { get; set; }
        public string? ComplianceState { get; set; }
        public bool? IsEncrypted { get; set; }
        public string? ManagementAgent { get; set; }
        public DateTime? EnrolledDateTime { get; set; }
        public DateTime? LastSyncDateTime { get; set; }
        public string? DeviceEnrollmentType { get; set; }
    }

    private class CaPolicyInfo
    {
        public string? Id { get; set; }
        public string? DisplayName { get; set; }
        public string? State { get; set; }
        public CaGrantControls? GrantControls { get; set; }
    }

    private class CaGrantControls
    {
        public List<string>? BuiltInControls { get; set; }
    }

    private class AutopilotProfileInfo
    {
        public string? Id { get; set; }
        public string? DisplayName { get; set; }
        public string? Description { get; set; }
    }

    private class AppProtectionPolicyInfo
    {
        public string? Id { get; set; }
        public string? DisplayName { get; set; }
        public string? OdataType { get; set; }
    }

    private class SecurityBaselineInfo
    {
        public string? Id { get; set; }
        public string? DisplayName { get; set; }
        public string? Description { get; set; }
    }

    #endregion
}
