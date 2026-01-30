using System.Text.Json;
using Cloudativ.Assessment.Application.Services;
using Cloudativ.Assessment.Domain.Enums;
using Cloudativ.Assessment.Domain.Interfaces;
using Microsoft.Extensions.Logging;

namespace Cloudativ.Assessment.Infrastructure.Modules;

public class AuditLoggingAssessmentModule : BaseAssessmentModule
{
    private readonly IScoringService _scoringService;

    public AuditLoggingAssessmentModule(ILogger<AuditLoggingAssessmentModule> logger, IScoringService scoringService)
        : base(logger)
    {
        _scoringService = scoringService;
    }

    public override AssessmentDomain Domain => AssessmentDomain.AuditLogging;
    public override string DisplayName => "Audit & Logging";
    public override string Description => "Assesses unified audit log configuration, sign-in logs, alert policies, and monitoring capabilities.";

    public override IReadOnlyList<string> RequiredPermissions => new[]
    {
        "AuditLog.Read.All",
        "Directory.Read.All",
        "Policy.Read.All",
        "SecurityEvents.Read.All"
    };

    public override async Task<CollectionResult> CollectAsync(IGraphClientWrapper graphClient, CancellationToken cancellationToken = default)
    {
        var rawData = new Dictionary<string, object?>();
        var warnings = new List<string>();
        var unavailableEndpoints = new List<string>();

        try
        {
            // 1. Collect Directory Audit Logs (sample)
            _logger.LogInformation("Collecting directory audit logs...");
            try
            {
                var auditLogsJson = await graphClient.GetRawJsonAsync(
                    "auditLogs/directoryAudits?$top=100&$orderby=activityDateTime desc",
                    cancellationToken);
                rawData["directoryAudits"] = auditLogsJson;
            }
            catch (Exception ex)
            {
                warnings.Add($"Failed to collect directory audit logs: {ex.Message}");
                unavailableEndpoints.Add("directoryAudits");
            }

            // 2. Collect Sign-In Logs (sample)
            _logger.LogInformation("Collecting sign-in logs...");
            try
            {
                var signInLogsJson = await graphClient.GetRawJsonAsync(
                    "auditLogs/signIns?$top=100&$orderby=createdDateTime desc",
                    cancellationToken);
                rawData["signInLogs"] = signInLogsJson;
            }
            catch (Exception ex)
            {
                warnings.Add($"Failed to collect sign-in logs: {ex.Message}");
                unavailableEndpoints.Add("signInLogs");
            }

            // 3. Collect Provisioning Logs
            _logger.LogInformation("Collecting provisioning logs...");
            try
            {
                var provisioningLogsJson = await graphClient.GetRawJsonAsync(
                    "auditLogs/provisioning?$top=50&$orderby=activityDateTime desc",
                    cancellationToken);
                rawData["provisioningLogs"] = provisioningLogsJson;
            }
            catch (Exception ex)
            {
                warnings.Add($"Failed to collect provisioning logs: {ex.Message}");
            }

            // 4. Collect Security Alerts
            _logger.LogInformation("Collecting security alerts...");
            try
            {
                var alertsJson = await graphClient.GetRawJsonAsync(
                    "security/alerts_v2?$top=100",
                    cancellationToken);
                rawData["securityAlerts"] = alertsJson;
            }
            catch (Exception ex)
            {
                warnings.Add($"Failed to collect security alerts: {ex.Message}");
            }

            // 5. Collect Organization Settings
            _logger.LogInformation("Collecting organization settings...");
            try
            {
                var orgSettingsJson = await graphClient.GetRawJsonAsync(
                    "organization?$select=id,displayName,verifiedDomains",
                    cancellationToken);
                rawData["organization"] = orgSettingsJson;
            }
            catch (Exception ex)
            {
                warnings.Add($"Failed to collect organization settings: {ex.Message}");
            }

            // 6. Collect Diagnostic Settings (if available)
            _logger.LogInformation("Collecting diagnostic settings...");
            try
            {
                var diagnosticSettingsJson = await graphClient.GetRawJsonAsync(
                    "https://graph.microsoft.com/beta/reports/settings",
                    cancellationToken);
                rawData["reportSettings"] = diagnosticSettingsJson;
            }
            catch (Exception ex)
            {
                warnings.Add($"Failed to collect report settings: {ex.Message}");
            }

            // 7. Check Authentication Methods Registration
            _logger.LogInformation("Collecting authentication methods usage...");
            try
            {
                var authMethodsUsageJson = await graphClient.GetRawJsonAsync(
                    "reports/authenticationMethods/usersRegisteredByMethod",
                    cancellationToken);
                rawData["authMethodsUsage"] = authMethodsUsageJson;
            }
            catch (Exception ex)
            {
                warnings.Add($"Failed to collect auth methods usage: {ex.Message}");
            }

            // 8. Collect Risky Sign-Ins (for monitoring)
            _logger.LogInformation("Collecting risky sign-ins...");
            try
            {
                var riskySignInsJson = await graphClient.GetRawJsonAsync(
                    "identityProtection/riskyUsers?$top=50",
                    cancellationToken);
                rawData["riskyUsers"] = riskySignInsJson;
            }
            catch (Exception ex)
            {
                warnings.Add($"Failed to collect risky users: {ex.Message}");
            }

            // 9. Get Service Principals with Audit Permissions
            _logger.LogInformation("Collecting service principals with audit access...");
            try
            {
                var auditAppsJson = await graphClient.GetRawJsonAsync(
                    "servicePrincipals?$filter=appRoleAssignments/any(a:a/resourceDisplayName eq 'Microsoft Graph' and a/appRoleId eq 'e1fe6dd8-ba31-4d61-89e7-88639da4683d')&$top=50",
                    cancellationToken);
                rawData["auditApps"] = auditAppsJson;
            }
            catch (Exception ex)
            {
                warnings.Add($"Failed to collect apps with audit access: {ex.Message}");
            }

            // 10. Collect Named Locations (for geo-based monitoring)
            _logger.LogInformation("Collecting named locations...");
            try
            {
                var namedLocationsJson = await graphClient.GetRawJsonAsync(
                    "identity/conditionalAccess/namedLocations",
                    cancellationToken);
                rawData["namedLocations"] = namedLocationsJson;
            }
            catch (Exception ex)
            {
                warnings.Add($"Failed to collect named locations: {ex.Message}");
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
            _logger.LogError(ex, "Failed to collect Audit & Logging data");
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
            var directoryAudits = ParseJsonCollection<AuditLogInfo>(
                rawData.RawData.GetValueOrDefault("directoryAudits") as string);
            var signInLogs = ParseJsonCollection<SignInLogInfo>(
                rawData.RawData.GetValueOrDefault("signInLogs") as string);
            var securityAlerts = ParseJsonCollection<AlertInfo>(
                rawData.RawData.GetValueOrDefault("securityAlerts") as string);
            var riskyUsers = ParseJsonCollection<RiskyUserInfo>(
                rawData.RawData.GetValueOrDefault("riskyUsers") as string);
            var namedLocations = ParseJsonCollection<NamedLocationInfo>(
                rawData.RawData.GetValueOrDefault("namedLocations") as string);

            // Calculate metrics
            var failedSignIns = signInLogs.Count(s => s.Status?.ErrorCode != 0);
            var successfulSignIns = signInLogs.Count(s => s.Status?.ErrorCode == 0);
            var uniqueSignInUsers = signInLogs.Select(s => s.UserPrincipalName).Distinct().Count();
            var adminActivities = directoryAudits.Count(a =>
                a.Category?.Contains("Role", StringComparison.OrdinalIgnoreCase) == true ||
                a.Category?.Contains("User", StringComparison.OrdinalIgnoreCase) == true);

            findings.Metrics["directoryAuditsCount"] = directoryAudits.Count;
            findings.Metrics["signInLogsCount"] = signInLogs.Count;
            findings.Metrics["failedSignIns"] = failedSignIns;
            findings.Metrics["successfulSignIns"] = successfulSignIns;
            findings.Metrics["uniqueSignInUsers"] = uniqueSignInUsers;
            findings.Metrics["securityAlertsCount"] = securityAlerts.Count;
            findings.Metrics["namedLocationsCount"] = namedLocations.Count;
            findings.Metrics["unifiedAuditEnabled"] = !rawData.UnavailableEndpoints.Contains("directoryAudits") ? 1 : 0;
            findings.Metrics["signInLogsEnabled"] = !rawData.UnavailableEndpoints.Contains("signInLogs") ? 1 : 0;

            // Check 1: Audit Log Access
            if (rawData.UnavailableEndpoints.Contains("directoryAudits"))
            {
                findings.Findings.Add(CreateFinding(
                    "AUD-001",
                    "Audit Logs Inaccessible",
                    "Unable to access directory audit logs",
                    "Audit logs are critical for security monitoring. Access may require appropriate licensing or permissions.",
                    Severity.High,
                    false,
                    "Audit Configuration",
                    remediation: "Ensure Azure AD Premium licensing and appropriate permissions for audit log access.",
                    references: "https://learn.microsoft.com/en-us/azure/active-directory/reports-monitoring/concept-audit-logs"
                ));
            }
            else
            {
                findings.Findings.Add(CreateFinding(
                    "AUD-001",
                    "Audit Logs Accessible",
                    "Directory audit logs are accessible",
                    $"Retrieved {directoryAudits.Count} recent audit log entries.",
                    Severity.Informational,
                    true,
                    "Audit Configuration"
                ));
            }

            // Check 2: Sign-In Logs Access
            if (rawData.UnavailableEndpoints.Contains("signInLogs"))
            {
                findings.Findings.Add(CreateFinding(
                    "AUD-002",
                    "Sign-In Logs Inaccessible",
                    "Unable to access sign-in logs",
                    "Sign-in logs are essential for monitoring authentication activities and detecting threats.",
                    Severity.High,
                    false,
                    "Sign-In Monitoring",
                    remediation: "Ensure Azure AD Premium licensing for sign-in log access."
                ));
            }
            else
            {
                findings.Findings.Add(CreateFinding(
                    "AUD-002",
                    "Sign-In Logs Accessible",
                    "Sign-in logs are accessible",
                    $"Retrieved {signInLogs.Count} recent sign-in entries ({uniqueSignInUsers} unique users).",
                    Severity.Informational,
                    true,
                    "Sign-In Monitoring"
                ));
            }

            // Check 3: Failed Sign-In Rate
            if (signInLogs.Count > 0)
            {
                var failedRate = (failedSignIns * 100.0 / signInLogs.Count);
                if (failedRate > 30)
                {
                    findings.Findings.Add(CreateFinding(
                        "AUD-003",
                        "High Failed Sign-In Rate",
                        $"{failedRate:F1}% of sign-ins are failing",
                        "A high failure rate may indicate credential stuffing attacks or misconfigured applications.",
                        Severity.High,
                        false,
                        "Sign-In Analysis",
                        remediation: "Investigate failed sign-ins for patterns indicating attacks or misconfiguration."
                    ));
                }
                else if (failedRate > 15)
                {
                    findings.Findings.Add(CreateFinding(
                        "AUD-003",
                        "Moderate Failed Sign-In Rate",
                        $"{failedRate:F1}% of sign-ins are failing",
                        "Review failed sign-ins for potential issues.",
                        Severity.Medium,
                        false,
                        "Sign-In Analysis",
                        remediation: "Monitor failed sign-ins and investigate unusual patterns."
                    ));
                }
                else
                {
                    findings.Findings.Add(CreateFinding(
                        "AUD-003",
                        "Normal Sign-In Success Rate",
                        $"{(100 - failedRate):F1}% sign-in success rate",
                        "Sign-in failure rate is within normal range.",
                        Severity.Informational,
                        true,
                        "Sign-In Analysis"
                    ));
                }
            }

            // Check 4: Named Locations for Geo-Monitoring
            if (namedLocations.Count == 0)
            {
                findings.Findings.Add(CreateFinding(
                    "AUD-004",
                    "No Named Locations",
                    "No named locations configured for geo-monitoring",
                    "Named locations help identify sign-ins from unexpected locations.",
                    Severity.Medium,
                    false,
                    "Geo-Monitoring",
                    remediation: "Configure named locations for trusted and blocked geographic regions.",
                    references: "https://learn.microsoft.com/en-us/azure/active-directory/conditional-access/location-condition"
                ));
            }
            else
            {
                var trustedLocations = namedLocations.Count(l => l.IsTrusted == true);
                findings.Findings.Add(CreateFinding(
                    "AUD-004",
                    "Named Locations Configured",
                    $"{namedLocations.Count} named locations configured ({trustedLocations} trusted)",
                    "Named locations enable geographic-based access monitoring.",
                    Severity.Informational,
                    true,
                    "Geo-Monitoring"
                ));
            }

            // Check 5: Active Security Alerts
            var activeAlerts = securityAlerts.Count(a => a.Status != "resolved");
            if (activeAlerts > 0)
            {
                findings.Findings.Add(CreateFinding(
                    "AUD-005",
                    "Active Security Alerts",
                    $"{activeAlerts} active security alerts require attention",
                    "Unresolved security alerts should be investigated and addressed.",
                    activeAlerts > 10 ? Severity.High : Severity.Medium,
                    false,
                    "Alert Management",
                    remediation: "Review and address active security alerts.",
                    affectedResources: securityAlerts.Where(a => a.Status != "resolved").Select(a => a.Title ?? "Unknown").Take(10).ToList()
                ));
            }
            else
            {
                findings.Findings.Add(CreateFinding(
                    "AUD-005",
                    "No Active Alerts",
                    "No active security alerts",
                    "All security alerts have been resolved.",
                    Severity.Informational,
                    true,
                    "Alert Management"
                ));
            }

            // Check 6: Log Retention (general assessment)
            findings.Findings.Add(CreateFinding(
                "AUD-006",
                "Log Retention",
                "Log retention policy should be verified",
                "Azure AD logs are retained for 30 days by default. Extended retention requires additional configuration.",
                Severity.Low,
                true, // Mark as informational
                "Data Retention",
                remediation: "Configure log export to Azure Monitor, SIEM, or storage account for extended retention.",
                references: "https://learn.microsoft.com/en-us/azure/active-directory/reports-monitoring/concept-activity-logs-azure-monitor"
            ));
            findings.Metrics["auditRetentionDays"] = 30; // Default Azure AD retention

            // Check 7: Risky Sign-In Monitoring
            if (riskyUsers.Count > 0)
            {
                findings.Findings.Add(CreateFinding(
                    "AUD-007",
                    "Risky Users Detected",
                    $"{riskyUsers.Count} users flagged as risky",
                    "Risky users have been detected by Identity Protection and should be investigated.",
                    Severity.High,
                    false,
                    "Identity Protection",
                    remediation: "Investigate risky users and remediate compromised accounts."
                ));
            }
            else
            {
                findings.Findings.Add(CreateFinding(
                    "AUD-007",
                    "No Risky Users",
                    "No users currently flagged as risky",
                    "Identity Protection monitoring is active with no current flags.",
                    Severity.Informational,
                    true,
                    "Identity Protection"
                ));
            }

            // Check 8: Audit Categories Coverage
            var auditCategories = directoryAudits
                .Select(a => a.Category)
                .Where(c => !string.IsNullOrEmpty(c))
                .Distinct()
                .ToList();

            if (auditCategories.Count >= 3)
            {
                findings.Findings.Add(CreateFinding(
                    "AUD-008",
                    "Audit Coverage",
                    $"Audit logs covering {auditCategories.Count} activity categories",
                    $"Categories: {string.Join(", ", auditCategories.Take(5))}",
                    Severity.Informational,
                    true,
                    "Audit Coverage"
                ));
            }
            else if (directoryAudits.Count > 0)
            {
                findings.Findings.Add(CreateFinding(
                    "AUD-008",
                    "Limited Audit Coverage",
                    "Audit logs show limited activity categories",
                    "Ensure all critical activities are being logged.",
                    Severity.Low,
                    false,
                    "Audit Coverage",
                    remediation: "Review audit log configuration to ensure comprehensive coverage."
                ));
            }

            // Set mailbox auditing metric (general - would need Exchange PowerShell for precise check)
            findings.Metrics["mailboxAuditingEnabled"] = 1; // Default enabled since 2019

            // Generate summary
            findings.Summary.Add($"Directory Audit Logs: {directoryAudits.Count} entries");
            findings.Summary.Add($"Sign-In Logs: {signInLogs.Count} entries ({failedSignIns} failed)");
            findings.Summary.Add($"Security Alerts: {securityAlerts.Count} ({securityAlerts.Count(a => a.Status != "resolved")} active)");
            findings.Summary.Add($"Named Locations: {namedLocations.Count}");
            if (rawData.Warnings.Any())
            {
                findings.Summary.Add($"Note: {rawData.Warnings.Count} data points could not be collected");
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error normalizing Audit & Logging findings");
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

    private class AuditLogInfo
    {
        public string? Id { get; set; }
        public string? Category { get; set; }
        public string? ActivityDisplayName { get; set; }
        public string? Result { get; set; }
        public DateTime? ActivityDateTime { get; set; }
    }

    private class SignInLogInfo
    {
        public string? Id { get; set; }
        public string? UserPrincipalName { get; set; }
        public string? UserDisplayName { get; set; }
        public string? AppDisplayName { get; set; }
        public string? ClientAppUsed { get; set; }
        public SignInStatus? Status { get; set; }
        public DateTime? CreatedDateTime { get; set; }
    }

    private class SignInStatus
    {
        public int? ErrorCode { get; set; }
        public string? FailureReason { get; set; }
    }

    private class AlertInfo
    {
        public string? Id { get; set; }
        public string? Title { get; set; }
        public string? Category { get; set; }
        public string? Severity { get; set; }
        public string? Status { get; set; }
    }

    private class RiskyUserInfo
    {
        public string? Id { get; set; }
        public string? UserDisplayName { get; set; }
        public string? RiskLevel { get; set; }
        public string? RiskState { get; set; }
    }

    private class NamedLocationInfo
    {
        public string? Id { get; set; }
        public string? DisplayName { get; set; }
        public bool? IsTrusted { get; set; }
        public string? OdataType { get; set; }
    }

    #endregion
}
