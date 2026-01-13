using System.Text.Json;
using Cloudativ.Assessment.Application.Services;
using Cloudativ.Assessment.Domain.Enums;
using Cloudativ.Assessment.Domain.Interfaces;
using Microsoft.Extensions.Logging;

namespace Cloudativ.Assessment.Infrastructure.Modules;

public class DlpAssessmentModule : BaseAssessmentModule
{
    private readonly IScoringService _scoringService;

    public DlpAssessmentModule(ILogger<DlpAssessmentModule> logger, IScoringService scoringService)
        : base(logger)
    {
        _scoringService = scoringService;
    }

    public override AssessmentDomain Domain => AssessmentDomain.DataProtectionCompliance;
    public override string DisplayName => "Data Protection & Compliance";
    public override string Description => "Assesses DLP policies, sensitivity labels, retention policies, and compliance posture.";

    public override IReadOnlyList<string> RequiredPermissions => new[]
    {
        "InformationProtectionPolicy.Read.All",
        "Policy.Read.All",
        "Directory.Read.All"
    };

    public override async Task<CollectionResult> CollectAsync(IGraphClientWrapper graphClient, CancellationToken cancellationToken = default)
    {
        var rawData = new Dictionary<string, object?>();
        var warnings = new List<string>();
        var unavailableEndpoints = new List<string>();

        try
        {
            // 1. Collect Sensitivity Labels
            _logger.LogInformation("Collecting sensitivity labels...");
            try
            {
                var labelsJson = await graphClient.GetRawJsonAsync(
                    "informationProtection/policy/labels",
                    cancellationToken);
                rawData["sensitivityLabels"] = labelsJson;
            }
            catch (Exception ex)
            {
                warnings.Add($"Failed to collect sensitivity labels: {ex.Message}");
                unavailableEndpoints.Add("sensitivityLabels");
            }

            // 2. Collect Label Policies (beta endpoint)
            _logger.LogInformation("Collecting label policies...");
            try
            {
                var labelPoliciesJson = await graphClient.GetRawJsonAsync(
                    "https://graph.microsoft.com/beta/security/informationProtection/labelPolicySetting",
                    cancellationToken);
                rawData["labelPolicies"] = labelPoliciesJson;
            }
            catch (Exception ex)
            {
                warnings.Add($"Failed to collect label policies (may require additional license): {ex.Message}");
            }

            // 3. Collect DLP Policies from Security & Compliance
            _logger.LogInformation("Collecting DLP policies...");
            try
            {
                // Note: DLP policies are typically accessed via Security & Compliance PowerShell or Security Graph
                // Using beta endpoint
                var dlpPoliciesJson = await graphClient.GetRawJsonAsync(
                    "https://graph.microsoft.com/beta/security/dataLossPreventionPolicies",
                    cancellationToken);
                rawData["dlpPolicies"] = dlpPoliciesJson;
            }
            catch (Exception ex)
            {
                warnings.Add($"Failed to collect DLP policies: {ex.Message}");
                unavailableEndpoints.Add("dlpPolicies");
            }

            // 4. Collect Retention Labels
            _logger.LogInformation("Collecting retention labels...");
            try
            {
                var retentionLabelsJson = await graphClient.GetRawJsonAsync(
                    "https://graph.microsoft.com/beta/security/labels/retentionLabels",
                    cancellationToken);
                rawData["retentionLabels"] = retentionLabelsJson;
            }
            catch (Exception ex)
            {
                warnings.Add($"Failed to collect retention labels: {ex.Message}");
            }

            // 5. Collect Information Protection Policies
            _logger.LogInformation("Collecting information protection policies...");
            try
            {
                var ipPoliciesJson = await graphClient.GetRawJsonAsync(
                    "informationProtection/policy",
                    cancellationToken);
                rawData["informationProtectionPolicy"] = ipPoliciesJson;
            }
            catch (Exception ex)
            {
                warnings.Add($"Failed to collect information protection policies: {ex.Message}");
            }

            // 6. Collect Data Classification Overview (if available)
            _logger.LogInformation("Collecting data classification info...");
            try
            {
                var classificationJson = await graphClient.GetRawJsonAsync(
                    "https://graph.microsoft.com/beta/dataClassification/sensitiveTypes",
                    cancellationToken);
                rawData["sensitiveTypes"] = classificationJson;
            }
            catch (Exception ex)
            {
                warnings.Add($"Failed to collect sensitive types: {ex.Message}");
            }

            // 7. Collect Compliance Manager Data (if available)
            _logger.LogInformation("Collecting compliance manager data...");
            try
            {
                var complianceJson = await graphClient.GetRawJsonAsync(
                    "https://graph.microsoft.com/beta/security/complianceData",
                    cancellationToken);
                rawData["complianceData"] = complianceJson;
            }
            catch (Exception ex)
            {
                warnings.Add($"Failed to collect compliance data: {ex.Message}");
            }

            // 8. Collect Organization Settings for DLP
            _logger.LogInformation("Collecting organization settings...");
            try
            {
                var orgSettingsJson = await graphClient.GetRawJsonAsync(
                    "organization?$select=id,displayName,securityComplianceCenterUrl",
                    cancellationToken);
                rawData["organization"] = orgSettingsJson;
            }
            catch (Exception ex)
            {
                warnings.Add($"Failed to collect organization settings: {ex.Message}");
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
            _logger.LogError(ex, "Failed to collect DLP/Compliance data");
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
            var sensitivityLabels = ParseJsonCollection<SensitivityLabelInfo>(
                rawData.RawData.GetValueOrDefault("sensitivityLabels") as string);
            var dlpPolicies = ParseJsonCollection<DlpPolicyInfo>(
                rawData.RawData.GetValueOrDefault("dlpPolicies") as string);
            var retentionLabels = ParseJsonCollection<RetentionLabelInfo>(
                rawData.RawData.GetValueOrDefault("retentionLabels") as string);
            var sensitiveTypes = ParseJsonCollection<SensitiveTypeInfo>(
                rawData.RawData.GetValueOrDefault("sensitiveTypes") as string);

            // Calculate metrics
            var enabledDlpPolicies = dlpPolicies.Count(p => p.IsEnabled == true);
            var publishedLabels = sensitivityLabels.Count(l => l.IsActive == true);

            findings.Metrics["sensitivityLabelsCount"] = sensitivityLabels.Count;
            findings.Metrics["publishedLabelsCount"] = publishedLabels;
            findings.Metrics["dlpPoliciesCount"] = dlpPolicies.Count;
            findings.Metrics["enabledDlpPolicies"] = enabledDlpPolicies;
            findings.Metrics["retentionLabelsCount"] = retentionLabels.Count;
            findings.Metrics["sensitiveTypesCount"] = sensitiveTypes.Count;

            // Check 1: Sensitivity Labels Configuration
            if (sensitivityLabels.Count == 0)
            {
                findings.Findings.Add(CreateFinding(
                    "DLP-001",
                    "No Sensitivity Labels",
                    "No sensitivity labels configured",
                    "Sensitivity labels are essential for classifying and protecting sensitive data. None were found.",
                    Severity.High,
                    false,
                    "Information Protection",
                    remediation: "Configure sensitivity labels to classify documents containing sensitive information.",
                    references: "https://learn.microsoft.com/en-us/microsoft-365/compliance/sensitivity-labels"
                ));
            }
            else if (publishedLabels == 0)
            {
                findings.Findings.Add(CreateFinding(
                    "DLP-001",
                    "No Published Sensitivity Labels",
                    "Sensitivity labels exist but none are published",
                    $"Found {sensitivityLabels.Count} sensitivity labels but none are published/active.",
                    Severity.Medium,
                    false,
                    "Information Protection",
                    remediation: "Publish sensitivity labels so users can apply them to documents and emails."
                ));
            }
            else
            {
                findings.Findings.Add(CreateFinding(
                    "DLP-001",
                    "Sensitivity Labels Configured",
                    "Sensitivity labels are configured and published",
                    $"Found {sensitivityLabels.Count} sensitivity labels with {publishedLabels} published.",
                    Severity.Informational,
                    true,
                    "Information Protection"
                ));
            }

            // Check 2: DLP Policies
            if (dlpPolicies.Count == 0)
            {
                findings.Findings.Add(CreateFinding(
                    "DLP-002",
                    "No DLP Policies",
                    "No Data Loss Prevention policies configured",
                    "DLP policies help prevent accidental sharing of sensitive information. None were found.",
                    Severity.Critical,
                    false,
                    "Data Loss Prevention",
                    remediation: "Create DLP policies to detect and protect sensitive information such as credit card numbers, SSNs, and financial data.",
                    references: "https://learn.microsoft.com/en-us/microsoft-365/compliance/dlp-learn-about-dlp"
                ));
            }
            else if (enabledDlpPolicies == 0)
            {
                findings.Findings.Add(CreateFinding(
                    "DLP-002",
                    "No Enabled DLP Policies",
                    "All DLP policies are disabled",
                    $"Found {dlpPolicies.Count} DLP policies but none are enabled.",
                    Severity.High,
                    false,
                    "Data Loss Prevention",
                    remediation: "Enable DLP policies after testing in simulation mode."
                ));
            }
            else
            {
                findings.Findings.Add(CreateFinding(
                    "DLP-002",
                    "DLP Policies Configured",
                    "DLP policies are configured and enabled",
                    $"Found {dlpPolicies.Count} DLP policies with {enabledDlpPolicies} enabled.",
                    Severity.Informational,
                    true,
                    "Data Loss Prevention"
                ));
            }

            // Check 3: Retention Labels
            if (retentionLabels.Count == 0)
            {
                findings.Findings.Add(CreateFinding(
                    "DLP-003",
                    "No Retention Labels",
                    "No retention labels configured",
                    "Retention labels help with records management and regulatory compliance.",
                    Severity.Medium,
                    false,
                    "Records Management",
                    remediation: "Create retention labels to manage document lifecycle and meet compliance requirements.",
                    references: "https://learn.microsoft.com/en-us/microsoft-365/compliance/retention"
                ));
            }
            else
            {
                findings.Findings.Add(CreateFinding(
                    "DLP-003",
                    "Retention Labels Configured",
                    "Retention labels are configured",
                    $"Found {retentionLabels.Count} retention labels for records management.",
                    Severity.Informational,
                    true,
                    "Records Management"
                ));
            }

            // Check 4: Default Label Policy
            var hasDefaultLabel = sensitivityLabels.Any(l => l.IsDefault == true);
            if (sensitivityLabels.Count > 0 && !hasDefaultLabel)
            {
                findings.Findings.Add(CreateFinding(
                    "DLP-004",
                    "No Default Sensitivity Label",
                    "No default sensitivity label is configured",
                    "A default label ensures all documents have at least basic classification applied.",
                    Severity.Medium,
                    false,
                    "Information Protection",
                    remediation: "Configure a default sensitivity label for documents and emails to ensure baseline classification."
                ));
            }
            else if (hasDefaultLabel)
            {
                findings.Findings.Add(CreateFinding(
                    "DLP-004",
                    "Default Label Configured",
                    "A default sensitivity label is configured",
                    "New documents will automatically receive a baseline classification.",
                    Severity.Informational,
                    true,
                    "Information Protection"
                ));
            }

            // Check 5: DLP Policy Coverage
            var hasEmailDlp = dlpPolicies.Any(p => p.Locations?.Any(l =>
                l.Contains("Exchange", StringComparison.OrdinalIgnoreCase)) == true);
            var hasSharePointDlp = dlpPolicies.Any(p => p.Locations?.Any(l =>
                l.Contains("SharePoint", StringComparison.OrdinalIgnoreCase) ||
                l.Contains("OneDrive", StringComparison.OrdinalIgnoreCase)) == true);
            var hasTeamsDlp = dlpPolicies.Any(p => p.Locations?.Any(l =>
                l.Contains("Teams", StringComparison.OrdinalIgnoreCase)) == true);

            if (dlpPolicies.Count > 0)
            {
                var coverageGaps = new List<string>();
                if (!hasEmailDlp) coverageGaps.Add("Exchange/Email");
                if (!hasSharePointDlp) coverageGaps.Add("SharePoint/OneDrive");
                if (!hasTeamsDlp) coverageGaps.Add("Microsoft Teams");

                if (coverageGaps.Any())
                {
                    findings.Findings.Add(CreateFinding(
                        "DLP-005",
                        "DLP Coverage Gaps",
                        "DLP policies do not cover all workloads",
                        $"DLP policies are missing coverage for: {string.Join(", ", coverageGaps)}",
                        Severity.Medium,
                        false,
                        "Data Loss Prevention",
                        remediation: $"Extend DLP policies to cover: {string.Join(", ", coverageGaps)}",
                        affectedResources: coverageGaps
                    ));
                }
                else
                {
                    findings.Findings.Add(CreateFinding(
                        "DLP-005",
                        "Comprehensive DLP Coverage",
                        "DLP policies cover all major workloads",
                        "DLP policies are applied to Exchange, SharePoint/OneDrive, and Teams.",
                        Severity.Informational,
                        true,
                        "Data Loss Prevention"
                    ));
                }
            }

            // Check 6: Sensitive Information Types
            if (sensitiveTypes.Count == 0 && dlpPolicies.Count == 0)
            {
                findings.Findings.Add(CreateFinding(
                    "DLP-006",
                    "No Sensitive Information Types",
                    "No custom sensitive information types configured",
                    "Custom sensitive information types help detect organization-specific sensitive data.",
                    Severity.Low,
                    false,
                    "Data Classification",
                    remediation: "Consider creating custom sensitive information types for organization-specific data patterns.",
                    references: "https://learn.microsoft.com/en-us/microsoft-365/compliance/create-a-custom-sensitive-information-type"
                ));
            }

            // Check 7: Label Encryption
            var encryptedLabels = sensitivityLabels.Count(l => l.ContentMarking?.Any() == true || l.Encryption != null);
            if (sensitivityLabels.Count > 0 && encryptedLabels == 0)
            {
                findings.Findings.Add(CreateFinding(
                    "DLP-007",
                    "No Labels with Encryption",
                    "No sensitivity labels include encryption protection",
                    "Labels with encryption ensure sensitive documents remain protected even when shared externally.",
                    Severity.Medium,
                    false,
                    "Information Protection",
                    remediation: "Configure encryption settings on high-sensitivity labels to protect classified documents."
                ));
            }
            else if (encryptedLabels > 0)
            {
                findings.Findings.Add(CreateFinding(
                    "DLP-007",
                    "Labels with Protection",
                    "Sensitivity labels include protection settings",
                    $"{encryptedLabels} labels include content marking or encryption.",
                    Severity.Informational,
                    true,
                    "Information Protection"
                ));
            }

            // Check for unavailable endpoints
            if (rawData.UnavailableEndpoints.Contains("dlpPolicies"))
            {
                findings.Findings.Add(CreateFinding(
                    "DLP-008",
                    "DLP Data Unavailable",
                    "Unable to retrieve DLP policy information",
                    "DLP policy data could not be retrieved. This may be due to licensing or permissions.",
                    Severity.Informational,
                    true, // Mark as compliant since we can't assess
                    "Data Loss Prevention",
                    evidence: "Endpoint: dataLossPreventionPolicies was inaccessible",
                    remediation: "Ensure the app has appropriate permissions and the tenant has Microsoft 365 E3/E5 or equivalent licensing."
                ));
            }

            // Generate summary
            findings.Summary.Add($"Sensitivity Labels: {sensitivityLabels.Count} ({publishedLabels} published)");
            findings.Summary.Add($"DLP Policies: {dlpPolicies.Count} ({enabledDlpPolicies} enabled)");
            findings.Summary.Add($"Retention Labels: {retentionLabels.Count}");
            if (rawData.Warnings.Any())
            {
                findings.Summary.Add($"Note: {rawData.Warnings.Count} data points could not be collected (may require additional licensing)");
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error normalizing DLP findings");
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
            else if (doc.RootElement.ValueKind == JsonValueKind.Array)
            {
                foreach (var item in doc.RootElement.EnumerateArray())
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

    private class SensitivityLabelInfo
    {
        public string? Id { get; set; }
        public string? Name { get; set; }
        public string? Description { get; set; }
        public bool? IsActive { get; set; }
        public bool? IsDefault { get; set; }
        public int? Priority { get; set; }
        public List<string>? ContentMarking { get; set; }
        public object? Encryption { get; set; }
    }

    private class DlpPolicyInfo
    {
        public string? Id { get; set; }
        public string? Name { get; set; }
        public bool? IsEnabled { get; set; }
        public string? Mode { get; set; }
        public List<string>? Locations { get; set; }
        public List<string>? SensitiveTypes { get; set; }
    }

    private class RetentionLabelInfo
    {
        public string? Id { get; set; }
        public string? DisplayName { get; set; }
        public string? RetentionDuration { get; set; }
        public string? ActionAfterRetentionPeriod { get; set; }
        public bool? IsRecordLabel { get; set; }
    }

    private class SensitiveTypeInfo
    {
        public string? Id { get; set; }
        public string? Name { get; set; }
        public string? Description { get; set; }
        public string? Publisher { get; set; }
    }

    #endregion
}
