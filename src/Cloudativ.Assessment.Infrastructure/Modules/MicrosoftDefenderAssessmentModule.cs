using System.Text.Json;
using Cloudativ.Assessment.Application.Services;
using Cloudativ.Assessment.Domain.Enums;
using Cloudativ.Assessment.Domain.Interfaces;
using Microsoft.Extensions.Logging;

namespace Cloudativ.Assessment.Infrastructure.Modules;

public class MicrosoftDefenderAssessmentModule : BaseAssessmentModule
{
    private readonly IScoringService _scoringService;

    public MicrosoftDefenderAssessmentModule(ILogger<MicrosoftDefenderAssessmentModule> logger, IScoringService scoringService)
        : base(logger)
    {
        _scoringService = scoringService;
    }

    public override AssessmentDomain Domain => AssessmentDomain.MicrosoftDefender;
    public override string DisplayName => "Microsoft Defender Suite";
    public override string Description => "Assesses Microsoft Defender for Endpoint, Identity, Office 365, and Cloud Apps configuration and threat status.";

    public override IReadOnlyList<string> RequiredPermissions => new[]
    {
        "SecurityEvents.Read.All",
        "ThreatIndicators.Read.All",
        "SecurityActions.Read.All",
        "Device.Read.All"
    };

    public override async Task<CollectionResult> CollectAsync(IGraphClientWrapper graphClient, CancellationToken cancellationToken = default)
    {
        var rawData = new Dictionary<string, object?>();
        var warnings = new List<string>();
        var unavailableEndpoints = new List<string>();

        try
        {
            // 1. Collect Security Alerts
            _logger.LogInformation("Collecting security alerts...");
            try
            {
                var alertsJson = await graphClient.GetRawJsonAsync(
                    "security/alerts_v2?$top=500",
                    cancellationToken);
                rawData["securityAlerts"] = alertsJson;
            }
            catch (Exception ex)
            {
                warnings.Add($"Failed to collect security alerts: {ex.Message}");
                unavailableEndpoints.Add("securityAlerts");
            }

            // 2. Collect Secure Score
            _logger.LogInformation("Collecting secure score...");
            try
            {
                var secureScoreJson = await graphClient.GetRawJsonAsync(
                    "security/secureScores?$top=1",
                    cancellationToken);
                rawData["secureScore"] = secureScoreJson;
            }
            catch (Exception ex)
            {
                warnings.Add($"Failed to collect secure score: {ex.Message}");
            }

            // 3. Collect Secure Score Control Profiles
            _logger.LogInformation("Collecting secure score control profiles...");
            try
            {
                var controlProfilesJson = await graphClient.GetRawJsonAsync(
                    "security/secureScoreControlProfiles",
                    cancellationToken);
                rawData["secureScoreControlProfiles"] = controlProfilesJson;
            }
            catch (Exception ex)
            {
                warnings.Add($"Failed to collect secure score control profiles: {ex.Message}");
            }

            // 4. Collect Threat Assessment Requests
            _logger.LogInformation("Collecting threat assessment requests...");
            try
            {
                var threatRequestsJson = await graphClient.GetRawJsonAsync(
                    "informationProtection/threatAssessmentRequests",
                    cancellationToken);
                rawData["threatAssessmentRequests"] = threatRequestsJson;
            }
            catch (Exception ex)
            {
                warnings.Add($"Failed to collect threat assessment requests: {ex.Message}");
            }

            // 5. Collect Incidents
            _logger.LogInformation("Collecting security incidents...");
            try
            {
                var incidentsJson = await graphClient.GetRawJsonAsync(
                    "security/incidents?$top=100",
                    cancellationToken);
                rawData["incidents"] = incidentsJson;
            }
            catch (Exception ex)
            {
                warnings.Add($"Failed to collect incidents: {ex.Message}");
            }

            // 6. Collect Risky Users (Defender for Identity)
            _logger.LogInformation("Collecting risky users...");
            try
            {
                var riskyUsersJson = await graphClient.GetRawJsonAsync(
                    "identityProtection/riskyUsers?$filter=riskState ne 'none'&$top=100",
                    cancellationToken);
                rawData["riskyUsers"] = riskyUsersJson;
            }
            catch (Exception ex)
            {
                warnings.Add($"Failed to collect risky users: {ex.Message}");
            }

            // 7. Collect Risk Detections
            _logger.LogInformation("Collecting risk detections...");
            try
            {
                var riskDetectionsJson = await graphClient.GetRawJsonAsync(
                    "identityProtection/riskDetections?$top=100&$orderby=detectedDateTime desc",
                    cancellationToken);
                rawData["riskDetections"] = riskDetectionsJson;
            }
            catch (Exception ex)
            {
                warnings.Add($"Failed to collect risk detections: {ex.Message}");
            }

            // 8. Collect Attack Simulation Training Status
            _logger.LogInformation("Collecting attack simulation data...");
            try
            {
                var attackSimJson = await graphClient.GetRawJsonAsync(
                    "https://graph.microsoft.com/beta/security/attackSimulation/simulations",
                    cancellationToken);
                rawData["attackSimulations"] = attackSimJson;
            }
            catch (Exception ex)
            {
                warnings.Add($"Failed to collect attack simulations: {ex.Message}");
            }

            // 9. Collect Managed Devices for Defender Status
            _logger.LogInformation("Collecting device defender status...");
            try
            {
                var devicesJson = await graphClient.GetRawJsonAsync(
                    "deviceManagement/managedDevices?$select=id,deviceName,complianceState,operatingSystem&$top=999",
                    cancellationToken);
                rawData["managedDevices"] = devicesJson;
            }
            catch (Exception ex)
            {
                warnings.Add($"Failed to collect managed devices: {ex.Message}");
            }

            // 10. Collect Identity Risk Policies (CA)
            _logger.LogInformation("Collecting identity risk policies...");
            try
            {
                var riskPoliciesJson = await graphClient.GetRawJsonAsync(
                    "identity/conditionalAccess/policies?$filter=contains(displayName, 'risk')",
                    cancellationToken);
                rawData["riskBasedPolicies"] = riskPoliciesJson;
            }
            catch (Exception ex)
            {
                warnings.Add($"Failed to collect risk-based policies: {ex.Message}");
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
            _logger.LogError(ex, "Failed to collect Microsoft Defender data");
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
            var securityAlerts = ParseJsonCollection<AlertInfo>(
                rawData.RawData.GetValueOrDefault("securityAlerts") as string);
            var secureScore = ParseSecureScore(rawData.RawData.GetValueOrDefault("secureScore") as string);
            var controlProfiles = ParseJsonCollection<ControlProfileInfo>(
                rawData.RawData.GetValueOrDefault("secureScoreControlProfiles") as string);
            var incidents = ParseJsonCollection<IncidentInfo>(
                rawData.RawData.GetValueOrDefault("incidents") as string);
            var riskyUsers = ParseJsonCollection<RiskyUserInfo>(
                rawData.RawData.GetValueOrDefault("riskyUsers") as string);
            var riskDetections = ParseJsonCollection<RiskDetectionInfo>(
                rawData.RawData.GetValueOrDefault("riskDetections") as string);
            var attackSimulations = ParseJsonCollection<AttackSimulationInfo>(
                rawData.RawData.GetValueOrDefault("attackSimulations") as string);
            var riskPolicies = ParseJsonCollection<CaPolicyInfo>(
                rawData.RawData.GetValueOrDefault("riskBasedPolicies") as string);

            // Categorize alerts
            var highSeverityAlerts = securityAlerts.Count(a => a.Severity == "high");
            var mediumSeverityAlerts = securityAlerts.Count(a => a.Severity == "medium");
            var activeAlerts = securityAlerts.Count(a => a.Status != "resolved");
            var activeIncidents = incidents.Count(i => i.Status != "resolved");
            var highRiskUsers = riskyUsers.Count(u => u.RiskLevel == "high");

            // Calculate metrics
            findings.Metrics["secureScorePercentage"] = secureScore?.CurrentScorePercentage ?? 0;
            findings.Metrics["totalAlerts"] = securityAlerts.Count;
            findings.Metrics["highSeverityAlerts"] = highSeverityAlerts;
            findings.Metrics["mediumSeverityAlerts"] = mediumSeverityAlerts;
            findings.Metrics["activeAlerts"] = activeAlerts;
            findings.Metrics["activeIncidents"] = activeIncidents;
            findings.Metrics["riskyUsersCount"] = riskyUsers.Count;
            findings.Metrics["highRiskUsers"] = highRiskUsers;
            findings.Metrics["riskDetections"] = riskDetections.Count;
            findings.Metrics["attackSimulations"] = attackSimulations.Count;

            // Check 1: Microsoft Secure Score
            var scorePercentage = secureScore?.CurrentScorePercentage ?? 0;
            if (scorePercentage < 40)
            {
                findings.Findings.Add(CreateFinding(
                    "MDE-001",
                    "Low Secure Score",
                    $"Microsoft Secure Score is {scorePercentage:F1}%",
                    "A low Secure Score indicates significant security improvements are possible.",
                    Severity.Critical,
                    false,
                    "Security Posture",
                    remediation: "Review Secure Score recommendations and implement high-impact improvements.",
                    references: "https://learn.microsoft.com/en-us/microsoft-365/security/defender/microsoft-secure-score"
                ));
            }
            else if (scorePercentage < 70)
            {
                findings.Findings.Add(CreateFinding(
                    "MDE-001",
                    "Moderate Secure Score",
                    $"Microsoft Secure Score is {scorePercentage:F1}%",
                    "There are opportunities to improve the security posture.",
                    Severity.Medium,
                    false,
                    "Security Posture",
                    remediation: "Continue implementing Secure Score recommendations to strengthen security."
                ));
            }
            else
            {
                findings.Findings.Add(CreateFinding(
                    "MDE-001",
                    "Good Secure Score",
                    $"Microsoft Secure Score is {scorePercentage:F1}%",
                    "The organization has a strong security posture according to Secure Score.",
                    Severity.Informational,
                    true,
                    "Security Posture"
                ));
            }

            // Check 2: Active Security Alerts
            if (highSeverityAlerts > 5)
            {
                findings.Findings.Add(CreateFinding(
                    "MDE-002",
                    "High Severity Alerts",
                    $"{highSeverityAlerts} high-severity security alerts detected",
                    "Multiple high-severity alerts require immediate investigation.",
                    Severity.Critical,
                    false,
                    "Threat Detection",
                    evidence: JsonSerializer.Serialize(securityAlerts.Where(a => a.Severity == "high").Take(10).Select(a => a.Title)),
                    remediation: "Investigate and respond to all high-severity security alerts immediately."
                ));
            }
            else if (highSeverityAlerts > 0)
            {
                findings.Findings.Add(CreateFinding(
                    "MDE-002",
                    "High Severity Alerts",
                    $"{highSeverityAlerts} high-severity security alerts detected",
                    "High-severity alerts should be investigated promptly.",
                    Severity.High,
                    false,
                    "Threat Detection",
                    remediation: "Investigate and address high-severity alerts."
                ));
            }
            else
            {
                findings.Findings.Add(CreateFinding(
                    "MDE-002",
                    "No High Severity Alerts",
                    "No high-severity security alerts detected",
                    "No critical security threats are currently active.",
                    Severity.Informational,
                    true,
                    "Threat Detection"
                ));
            }

            // Check 3: Active Incidents
            if (activeIncidents > 0)
            {
                findings.Findings.Add(CreateFinding(
                    "MDE-003",
                    "Active Security Incidents",
                    $"{activeIncidents} unresolved security incidents",
                    "Security incidents require investigation and resolution.",
                    activeIncidents > 5 ? Severity.High : Severity.Medium,
                    false,
                    "Incident Response",
                    remediation: "Review and resolve active security incidents.",
                    affectedResources: incidents.Where(i => i.Status != "resolved").Select(i => i.DisplayName ?? "Unknown").Take(10).ToList()
                ));
            }
            else
            {
                findings.Findings.Add(CreateFinding(
                    "MDE-003",
                    "No Active Incidents",
                    "No unresolved security incidents",
                    "All security incidents have been addressed.",
                    Severity.Informational,
                    true,
                    "Incident Response"
                ));
            }

            // Check 4: Risky Users
            if (highRiskUsers > 0)
            {
                findings.Findings.Add(CreateFinding(
                    "MDE-004",
                    "High-Risk Users Detected",
                    $"{highRiskUsers} users flagged as high risk",
                    "High-risk users may indicate compromised accounts or ongoing attacks.",
                    Severity.Critical,
                    false,
                    "Identity Protection",
                    remediation: "Investigate high-risk users and require password reset or block access.",
                    affectedResources: riskyUsers.Where(u => u.RiskLevel == "high").Select(u => u.UserDisplayName ?? "Unknown").Take(20).ToList()
                ));
            }
            else if (riskyUsers.Count > 0)
            {
                findings.Findings.Add(CreateFinding(
                    "MDE-004",
                    "Risky Users Detected",
                    $"{riskyUsers.Count} users flagged for risk",
                    "Users have been flagged by Identity Protection.",
                    Severity.Medium,
                    false,
                    "Identity Protection",
                    remediation: "Review and remediate risky user accounts."
                ));
            }
            else
            {
                findings.Findings.Add(CreateFinding(
                    "MDE-004",
                    "No Risky Users",
                    "No users currently flagged as risky",
                    "Identity Protection has not flagged any users for risk.",
                    Severity.Informational,
                    true,
                    "Identity Protection"
                ));
            }

            // Check 5: Risk-Based Conditional Access
            var hasSignInRiskPolicy = riskPolicies.Any(p =>
                p.State == "enabled" &&
                p.DisplayName?.Contains("sign-in", StringComparison.OrdinalIgnoreCase) == true);
            var hasUserRiskPolicy = riskPolicies.Any(p =>
                p.State == "enabled" &&
                p.DisplayName?.Contains("user", StringComparison.OrdinalIgnoreCase) == true);

            if (!hasSignInRiskPolicy && !hasUserRiskPolicy)
            {
                findings.Findings.Add(CreateFinding(
                    "MDE-005",
                    "No Risk-Based Policies",
                    "No risk-based Conditional Access policies detected",
                    "Risk-based policies automatically respond to identity threats.",
                    Severity.High,
                    false,
                    "Automated Response",
                    remediation: "Create Conditional Access policies that respond to sign-in and user risk levels.",
                    references: "https://learn.microsoft.com/en-us/azure/active-directory/identity-protection/howto-identity-protection-configure-risk-policies"
                ));
            }
            else if (!hasSignInRiskPolicy || !hasUserRiskPolicy)
            {
                findings.Findings.Add(CreateFinding(
                    "MDE-005",
                    "Partial Risk-Based Policies",
                    "Some risk-based policies are configured",
                    $"Sign-in risk policy: {(hasSignInRiskPolicy ? "Yes" : "No")}, User risk policy: {(hasUserRiskPolicy ? "Yes" : "No")}",
                    Severity.Medium,
                    false,
                    "Automated Response",
                    remediation: "Configure both sign-in risk and user risk Conditional Access policies."
                ));
            }
            else
            {
                findings.Findings.Add(CreateFinding(
                    "MDE-005",
                    "Risk-Based Policies Configured",
                    "Risk-based Conditional Access policies are in place",
                    "Both sign-in risk and user risk policies are configured.",
                    Severity.Informational,
                    true,
                    "Automated Response"
                ));
            }

            // Check 6: Attack Simulation Training
            if (attackSimulations.Count == 0)
            {
                findings.Findings.Add(CreateFinding(
                    "MDE-006",
                    "No Attack Simulations",
                    "No phishing simulations have been conducted",
                    "Attack simulation training helps identify users vulnerable to phishing.",
                    Severity.Medium,
                    false,
                    "Security Awareness",
                    remediation: "Configure and run phishing simulations to train users.",
                    references: "https://learn.microsoft.com/en-us/microsoft-365/security/office-365-security/attack-simulation-training-get-started"
                ));
            }
            else
            {
                findings.Findings.Add(CreateFinding(
                    "MDE-006",
                    "Attack Simulation Active",
                    $"{attackSimulations.Count} attack simulations configured",
                    "Phishing simulations are being used for security awareness training.",
                    Severity.Informational,
                    true,
                    "Security Awareness"
                ));
            }

            // Check 7: Recent Risk Detections
            var recentDetections = riskDetections.Count(d =>
                d.DetectedDateTime.HasValue &&
                d.DetectedDateTime > DateTime.UtcNow.AddDays(-7));

            if (recentDetections > 10)
            {
                findings.Findings.Add(CreateFinding(
                    "MDE-007",
                    "High Risk Detection Activity",
                    $"{recentDetections} risk detections in the past 7 days",
                    "A high number of recent risk detections may indicate an ongoing attack.",
                    Severity.High,
                    false,
                    "Threat Detection",
                    remediation: "Investigate recent risk detections and look for patterns indicating compromise."
                ));
            }
            else if (recentDetections > 0)
            {
                findings.Findings.Add(CreateFinding(
                    "MDE-007",
                    "Recent Risk Detections",
                    $"{recentDetections} risk detections in the past 7 days",
                    "Some risk events have been detected recently.",
                    Severity.Medium,
                    false,
                    "Threat Detection",
                    remediation: "Review recent risk detections for any suspicious patterns."
                ));
            }
            else
            {
                findings.Findings.Add(CreateFinding(
                    "MDE-007",
                    "No Recent Risk Detections",
                    "No risk detections in the past 7 days",
                    "No suspicious activities detected recently.",
                    Severity.Informational,
                    true,
                    "Threat Detection"
                ));
            }

            // Generate summary
            findings.Summary.Add($"Secure Score: {scorePercentage:F1}%");
            findings.Summary.Add($"Security Alerts: {securityAlerts.Count} ({highSeverityAlerts} high, {activeAlerts} active)");
            findings.Summary.Add($"Active Incidents: {activeIncidents}");
            findings.Summary.Add($"Risky Users: {riskyUsers.Count} ({highRiskUsers} high risk)");
            findings.Summary.Add($"Attack Simulations: {attackSimulations.Count}");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error normalizing Microsoft Defender findings");
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

    private SecureScoreInfo? ParseSecureScore(string? json)
    {
        if (string.IsNullOrEmpty(json))
            return null;

        try
        {
            using var doc = JsonDocument.Parse(json);
            if (doc.RootElement.TryGetProperty("value", out var valueElement) && valueElement.GetArrayLength() > 0)
            {
                var score = JsonSerializer.Deserialize<SecureScoreInfo>(valueElement[0].GetRawText(), new JsonSerializerOptions
                {
                    PropertyNameCaseInsensitive = true
                });

                // Calculate percentage
                if (score != null && score.MaxScore > 0)
                {
                    score.CurrentScorePercentage = (score.CurrentScore * 100.0 / score.MaxScore);
                }
                return score;
            }
            return null;
        }
        catch
        {
            return null;
        }
    }

    private class AlertInfo
    {
        public string? Id { get; set; }
        public string? Title { get; set; }
        public string? Category { get; set; }
        public string? Severity { get; set; }
        public string? Status { get; set; }
        public DateTime? CreatedDateTime { get; set; }
    }

    private class SecureScoreInfo
    {
        public string? Id { get; set; }
        public double CurrentScore { get; set; }
        public double MaxScore { get; set; }
        public double CurrentScorePercentage { get; set; }
    }

    private class ControlProfileInfo
    {
        public string? Id { get; set; }
        public string? ControlName { get; set; }
        public string? Title { get; set; }
        public bool? Deprecated { get; set; }
        public string? ImplementationCost { get; set; }
    }

    private class IncidentInfo
    {
        public string? Id { get; set; }
        public string? DisplayName { get; set; }
        public string? Status { get; set; }
        public string? Severity { get; set; }
        public DateTime? CreatedDateTime { get; set; }
    }

    private class RiskyUserInfo
    {
        public string? Id { get; set; }
        public string? UserDisplayName { get; set; }
        public string? UserPrincipalName { get; set; }
        public string? RiskLevel { get; set; }
        public string? RiskState { get; set; }
    }

    private class RiskDetectionInfo
    {
        public string? Id { get; set; }
        public string? RiskType { get; set; }
        public string? RiskLevel { get; set; }
        public DateTime? DetectedDateTime { get; set; }
        public string? UserDisplayName { get; set; }
    }

    private class AttackSimulationInfo
    {
        public string? Id { get; set; }
        public string? DisplayName { get; set; }
        public string? Status { get; set; }
        public string? AttackTechnique { get; set; }
    }

    private class CaPolicyInfo
    {
        public string? Id { get; set; }
        public string? DisplayName { get; set; }
        public string? State { get; set; }
    }

    #endregion
}
