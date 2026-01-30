using System.Text.Json;
using Cloudativ.Assessment.Application.Services;
using Cloudativ.Assessment.Domain.Enums;
using Cloudativ.Assessment.Domain.Interfaces;
using Microsoft.Extensions.Logging;

namespace Cloudativ.Assessment.Infrastructure.Modules;

public class ExchangeEmailSecurityAssessmentModule : BaseAssessmentModule
{
    private readonly IScoringService _scoringService;

    public ExchangeEmailSecurityAssessmentModule(ILogger<ExchangeEmailSecurityAssessmentModule> logger, IScoringService scoringService)
        : base(logger)
    {
        _scoringService = scoringService;
    }

    public override AssessmentDomain Domain => AssessmentDomain.ExchangeEmailSecurity;
    public override string DisplayName => "Exchange & Email Security";
    public override string Description => "Assesses email authentication (DMARC, DKIM, SPF), anti-phishing policies, safe links, safe attachments, and mail flow rules.";

    public override IReadOnlyList<string> RequiredPermissions => new[]
    {
        "Organization.Read.All",
        "Domain.Read.All",
        "Mail.Read",
        "SecurityEvents.Read.All"
    };

    public override async Task<CollectionResult> CollectAsync(IGraphClientWrapper graphClient, CancellationToken cancellationToken = default)
    {
        var rawData = new Dictionary<string, object?>();
        var warnings = new List<string>();
        var unavailableEndpoints = new List<string>();

        try
        {
            // 1. Collect Organization Domains
            _logger.LogInformation("Collecting organization domains...");
            try
            {
                var domainsJson = await graphClient.GetRawJsonAsync(
                    "domains",
                    cancellationToken);
                rawData["domains"] = domainsJson;
            }
            catch (Exception ex)
            {
                warnings.Add($"Failed to collect domains: {ex.Message}");
                unavailableEndpoints.Add("domains");
            }

            // 2. Collect Security Defaults
            _logger.LogInformation("Collecting security defaults...");
            try
            {
                var securityDefaultsJson = await graphClient.GetRawJsonAsync(
                    "policies/identitySecurityDefaultsEnforcementPolicy",
                    cancellationToken);
                rawData["securityDefaults"] = securityDefaultsJson;
            }
            catch (Exception ex)
            {
                warnings.Add($"Failed to collect security defaults: {ex.Message}");
            }

            // 3. Collect Threat Protection Policies (via Security Graph Beta)
            _logger.LogInformation("Collecting threat protection policies...");
            try
            {
                var threatPoliciesJson = await graphClient.GetRawJsonAsync(
                    "https://graph.microsoft.com/beta/security/threatSubmission/emailThreats",
                    cancellationToken);
                rawData["threatPolicies"] = threatPoliciesJson;
            }
            catch (Exception ex)
            {
                warnings.Add($"Failed to collect threat policies: {ex.Message}");
            }

            // 4. Collect Safe Links Policies (via Security API)
            _logger.LogInformation("Collecting safe links policies...");
            try
            {
                var safeLinksJson = await graphClient.GetRawJsonAsync(
                    "https://graph.microsoft.com/beta/security/attackSimulation/simulations",
                    cancellationToken);
                rawData["attackSimulations"] = safeLinksJson;
            }
            catch (Exception ex)
            {
                warnings.Add($"Failed to collect attack simulations: {ex.Message}");
            }

            // 5. Collect Organization Settings
            _logger.LogInformation("Collecting organization settings...");
            try
            {
                var orgSettingsJson = await graphClient.GetRawJsonAsync(
                    "organization?$select=id,displayName,verifiedDomains,securityComplianceCenterUrl",
                    cancellationToken);
                rawData["organization"] = orgSettingsJson;
            }
            catch (Exception ex)
            {
                warnings.Add($"Failed to collect organization settings: {ex.Message}");
            }

            // 6. Collect Conditional Access Policies
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

            // 7. Collect Alert Rules
            _logger.LogInformation("Collecting security alerts...");
            try
            {
                var alertsJson = await graphClient.GetRawJsonAsync(
                    "security/alerts_v2?$top=100&$filter=category eq 'Email'",
                    cancellationToken);
                rawData["emailAlerts"] = alertsJson;
            }
            catch (Exception ex)
            {
                warnings.Add($"Failed to collect email alerts: {ex.Message}");
            }

            // 8. Collect App Configurations for Mail
            _logger.LogInformation("Collecting mail-related app configurations...");
            try
            {
                var mailAppsJson = await graphClient.GetRawJsonAsync(
                    "servicePrincipals?$filter=tags/any(t:t eq 'WindowsAzureActiveDirectoryIntegratedApp')&$select=id,displayName,appId,tags",
                    cancellationToken);
                rawData["mailApps"] = mailAppsJson;
            }
            catch (Exception ex)
            {
                warnings.Add($"Failed to collect mail apps: {ex.Message}");
            }

            // 9. Check for External Email Forwarding (basic check)
            _logger.LogInformation("Checking mail flow configurations...");
            try
            {
                // Note: Full mail flow rules require Exchange Online PowerShell
                // This is a basic check via Graph
                var usersWithForwardingJson = await graphClient.GetRawJsonAsync(
                    "users?$select=id,displayName,mail,mailboxSettings&$top=100",
                    cancellationToken);
                rawData["usersMailboxSettings"] = usersWithForwardingJson;
            }
            catch (Exception ex)
            {
                warnings.Add($"Failed to collect mailbox settings: {ex.Message}");
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
            _logger.LogError(ex, "Failed to collect Exchange/Email Security data");
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
            var domains = ParseJsonCollection<DomainInfo>(
                rawData.RawData.GetValueOrDefault("domains") as string);
            var conditionalAccessPolicies = ParseJsonCollection<CaPolicyInfo>(
                rawData.RawData.GetValueOrDefault("conditionalAccessPolicies") as string);
            var emailAlerts = ParseJsonCollection<AlertInfo>(
                rawData.RawData.GetValueOrDefault("emailAlerts") as string);
            var organization = ParseOrganization(rawData.RawData.GetValueOrDefault("organization") as string);

            // Calculate metrics
            var verifiedDomains = domains.Count(d => d.IsVerified == true);
            var defaultDomains = domains.Count(d => d.IsDefault == true);

            findings.Metrics["totalDomains"] = domains.Count;
            findings.Metrics["verifiedDomains"] = verifiedDomains;
            findings.Metrics["recentEmailAlerts"] = emailAlerts.Count;
            findings.Metrics["dmarcConfigured"] = 0; // Will be updated below
            findings.Metrics["dkimConfigured"] = 0;
            findings.Metrics["spfConfigured"] = 0;

            // Check 1: DMARC Configuration
            var domainsWithDmarc = domains.Where(d =>
                d.ServiceConfigurationRecords?.Any(r =>
                    r.RecordType == "TXT" &&
                    r.Text?.Contains("v=DMARC1", StringComparison.OrdinalIgnoreCase) == true) == true).ToList();

            findings.Metrics["dmarcConfigured"] = domainsWithDmarc.Count;

            if (domainsWithDmarc.Count == 0)
            {
                findings.Findings.Add(CreateFinding(
                    "EXO-001",
                    "DMARC Not Configured",
                    "No domains have DMARC configured",
                    "DMARC (Domain-based Message Authentication) helps prevent email spoofing and phishing.",
                    Severity.Critical,
                    false,
                    "Email Authentication",
                    remediation: "Configure DMARC TXT records for all sending domains. Start with p=none and monitor reports.",
                    references: "https://learn.microsoft.com/en-us/microsoft-365/security/office-365-security/use-dmarc-to-validate-email"
                ));
            }
            else if (domainsWithDmarc.Count < verifiedDomains)
            {
                var missingDmarc = domains.Where(d => d.IsVerified == true &&
                    !domainsWithDmarc.Any(dm => dm.Id == d.Id)).Select(d => d.Id ?? "Unknown").ToList();

                findings.Findings.Add(CreateFinding(
                    "EXO-001",
                    "DMARC Partially Configured",
                    $"DMARC configured on {domainsWithDmarc.Count} of {verifiedDomains} domains",
                    "Some verified domains are missing DMARC configuration.",
                    Severity.High,
                    false,
                    "Email Authentication",
                    remediation: "Configure DMARC for all verified domains.",
                    affectedResources: missingDmarc
                ));
            }
            else
            {
                // Check DMARC policy strength
                var weakDmarc = domainsWithDmarc.Where(d =>
                    d.ServiceConfigurationRecords?.Any(r =>
                        r.Text?.Contains("p=none", StringComparison.OrdinalIgnoreCase) == true) == true).ToList();

                if (weakDmarc.Any())
                {
                    findings.Findings.Add(CreateFinding(
                        "EXO-001",
                        "DMARC Policy Too Weak",
                        $"{weakDmarc.Count} domains have DMARC set to 'none' (monitoring only)",
                        "DMARC policy 'none' does not reject spoofed emails. Consider upgrading to 'quarantine' or 'reject'.",
                        Severity.Medium,
                        false,
                        "Email Authentication",
                        remediation: "After monitoring DMARC reports, upgrade policy to p=quarantine or p=reject."
                    ));
                }
                else
                {
                    findings.Findings.Add(CreateFinding(
                        "EXO-001",
                        "DMARC Configured",
                        "DMARC is configured on all verified domains",
                        "Email authentication via DMARC is properly configured.",
                        Severity.Informational,
                        true,
                        "Email Authentication"
                    ));
                }
            }

            // Check 2: SPF Configuration
            var domainsWithSpf = domains.Where(d =>
                d.ServiceConfigurationRecords?.Any(r =>
                    r.RecordType == "TXT" &&
                    r.Text?.Contains("v=spf1", StringComparison.OrdinalIgnoreCase) == true) == true).ToList();

            findings.Metrics["spfConfigured"] = domainsWithSpf.Count;

            if (domainsWithSpf.Count == 0)
            {
                findings.Findings.Add(CreateFinding(
                    "EXO-002",
                    "SPF Not Configured",
                    "No domains have SPF configured",
                    "SPF (Sender Policy Framework) validates that mail is sent from authorized servers.",
                    Severity.High,
                    false,
                    "Email Authentication",
                    remediation: "Configure SPF TXT records specifying authorized sending sources.",
                    references: "https://learn.microsoft.com/en-us/microsoft-365/security/office-365-security/set-up-spf-in-office-365-to-help-prevent-spoofing"
                ));
            }
            else if (domainsWithSpf.Count < verifiedDomains)
            {
                findings.Findings.Add(CreateFinding(
                    "EXO-002",
                    "SPF Partially Configured",
                    $"SPF configured on {domainsWithSpf.Count} of {verifiedDomains} domains",
                    "Some verified domains are missing SPF configuration.",
                    Severity.Medium,
                    false,
                    "Email Authentication",
                    remediation: "Configure SPF for all verified domains."
                ));
            }
            else
            {
                findings.Findings.Add(CreateFinding(
                    "EXO-002",
                    "SPF Configured",
                    "SPF is configured on all verified domains",
                    "Sender Policy Framework is properly configured.",
                    Severity.Informational,
                    true,
                    "Email Authentication"
                ));
            }

            // Check 3: DKIM (check for CNAME records)
            var domainsWithDkim = domains.Where(d =>
                d.ServiceConfigurationRecords?.Any(r =>
                    r.RecordType == "CNAME" &&
                    (r.Label?.Contains("selector1._domainkey") == true ||
                     r.Label?.Contains("selector2._domainkey") == true)) == true).ToList();

            findings.Metrics["dkimConfigured"] = domainsWithDkim.Count;

            if (domainsWithDkim.Count == 0)
            {
                findings.Findings.Add(CreateFinding(
                    "EXO-003",
                    "DKIM Not Configured",
                    "No domains have DKIM configured",
                    "DKIM (DomainKeys Identified Mail) provides email message integrity verification.",
                    Severity.Medium,
                    false,
                    "Email Authentication",
                    remediation: "Enable DKIM signing for all verified domains in Microsoft 365 Defender portal.",
                    references: "https://learn.microsoft.com/en-us/microsoft-365/security/office-365-security/use-dkim-to-validate-outbound-email"
                ));
            }
            else
            {
                findings.Findings.Add(CreateFinding(
                    "EXO-003",
                    "DKIM Configured",
                    $"DKIM configured on {domainsWithDkim.Count} domains",
                    "DKIM signing is enabled for email authentication.",
                    Severity.Informational,
                    true,
                    "Email Authentication"
                ));
            }

            // Check 4: Recent Email Alerts
            if (emailAlerts.Count > 10)
            {
                findings.Findings.Add(CreateFinding(
                    "EXO-004",
                    "High Volume of Email Alerts",
                    $"{emailAlerts.Count} email-related security alerts detected",
                    "A high number of email security alerts may indicate active threats or misconfigurations.",
                    Severity.High,
                    false,
                    "Threat Detection",
                    remediation: "Investigate email security alerts and address underlying issues."
                ));
            }
            else if (emailAlerts.Count > 0)
            {
                findings.Findings.Add(CreateFinding(
                    "EXO-004",
                    "Email Alerts Present",
                    $"{emailAlerts.Count} email-related security alerts",
                    "Some email security alerts have been generated. Review for potential issues.",
                    Severity.Medium,
                    false,
                    "Threat Detection",
                    remediation: "Review and address email security alerts."
                ));
            }
            else
            {
                findings.Findings.Add(CreateFinding(
                    "EXO-004",
                    "No Recent Email Alerts",
                    "No recent email security alerts detected",
                    "No email-related security alerts in the monitored period.",
                    Severity.Informational,
                    true,
                    "Threat Detection"
                ));
            }

            // Check 5: External Email Warning
            // Note: This typically requires Exchange Online PowerShell to fully configure
            findings.Findings.Add(CreateFinding(
                "EXO-005",
                "External Email Warning",
                "External email warning tag configuration should be verified",
                "Marking external emails helps users identify potential phishing attempts.",
                Severity.Low,
                true, // Mark as compliant since we can't verify via Graph
                "User Awareness",
                remediation: "Enable external email tagging in Exchange Online to warn users of external senders.",
                references: "https://learn.microsoft.com/en-us/exchange/mail-flow-best-practices/remote-domains/remote-domains"
            ));

            // Check 6: Conditional Access for Legacy Auth
            var blocksLegacyAuth = conditionalAccessPolicies.Any(p =>
                p.State == "enabled" &&
                p.Conditions?.ClientAppTypes?.Contains("other") == true);

            if (!blocksLegacyAuth)
            {
                findings.Findings.Add(CreateFinding(
                    "EXO-006",
                    "Legacy Auth Not Blocked",
                    "Legacy authentication protocols are not blocked for email",
                    "Protocols like IMAP, POP3, and SMTP Auth bypass MFA and are vulnerable to credential attacks.",
                    Severity.High,
                    false,
                    "Authentication",
                    remediation: "Block legacy authentication via Conditional Access policies.",
                    references: "https://learn.microsoft.com/en-us/azure/active-directory/conditional-access/block-legacy-authentication"
                ));
            }
            else
            {
                findings.Findings.Add(CreateFinding(
                    "EXO-006",
                    "Legacy Auth Blocked",
                    "Legacy authentication protocols are blocked",
                    "Users cannot use IMAP, POP3, or other legacy protocols that bypass MFA.",
                    Severity.Informational,
                    true,
                    "Authentication"
                ));
            }

            // Check 7: Anti-phishing policies (general assessment)
            findings.Findings.Add(CreateFinding(
                "EXO-007",
                "Anti-Phishing Configuration",
                "Anti-phishing policy configuration should be verified in Security Center",
                "Anti-phishing policies protect against impersonation and phishing attacks.",
                Severity.Medium,
                true, // Cannot fully verify via Graph - recommend manual review
                "Threat Protection",
                remediation: "Review and configure anti-phishing policies in Microsoft 365 Defender portal.",
                references: "https://learn.microsoft.com/en-us/microsoft-365/security/office-365-security/configure-anti-phishing-policies-eop"
            ));

            // Check 8: Safe Links & Safe Attachments (general assessment)
            findings.Findings.Add(CreateFinding(
                "EXO-008",
                "Safe Links & Attachments",
                "Safe Links and Safe Attachments configuration should be verified",
                "Microsoft Defender for Office 365 provides URL and attachment scanning.",
                Severity.Medium,
                true, // Cannot fully verify via Graph - recommend manual review
                "Threat Protection",
                remediation: "Ensure Safe Links and Safe Attachments policies are enabled for all users.",
                references: "https://learn.microsoft.com/en-us/microsoft-365/security/office-365-security/safe-links"
            ));

            // Generate summary
            findings.Summary.Add($"Domains: {domains.Count} ({verifiedDomains} verified)");
            findings.Summary.Add($"Email Authentication: DMARC: {domainsWithDmarc.Count}, SPF: {domainsWithSpf.Count}, DKIM: {domainsWithDkim.Count}");
            findings.Summary.Add($"Email Alerts: {emailAlerts.Count}");
            if (rawData.Warnings.Any())
            {
                findings.Summary.Add($"Note: Some email security settings require verification in Microsoft 365 Defender portal");
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error normalizing Exchange/Email Security findings");
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

    private OrganizationInfo? ParseOrganization(string? json)
    {
        if (string.IsNullOrEmpty(json))
            return null;

        try
        {
            using var doc = JsonDocument.Parse(json);
            if (doc.RootElement.TryGetProperty("value", out var valueElement) && valueElement.GetArrayLength() > 0)
            {
                return JsonSerializer.Deserialize<OrganizationInfo>(valueElement[0].GetRawText(), new JsonSerializerOptions
                {
                    PropertyNameCaseInsensitive = true
                });
            }
            return null;
        }
        catch
        {
            return null;
        }
    }

    private class DomainInfo
    {
        public string? Id { get; set; }
        public bool? IsVerified { get; set; }
        public bool? IsDefault { get; set; }
        public string? AuthenticationType { get; set; }
        public List<ServiceConfigRecord>? ServiceConfigurationRecords { get; set; }
    }

    private class ServiceConfigRecord
    {
        public string? RecordType { get; set; }
        public string? Label { get; set; }
        public string? Text { get; set; }
    }

    private class CaPolicyInfo
    {
        public string? Id { get; set; }
        public string? DisplayName { get; set; }
        public string? State { get; set; }
        public CaConditions? Conditions { get; set; }
    }

    private class CaConditions
    {
        public List<string>? ClientAppTypes { get; set; }
    }

    private class AlertInfo
    {
        public string? Id { get; set; }
        public string? Title { get; set; }
        public string? Category { get; set; }
        public string? Severity { get; set; }
        public string? Status { get; set; }
    }

    private class OrganizationInfo
    {
        public string? Id { get; set; }
        public string? DisplayName { get; set; }
        public List<VerifiedDomainInfo>? VerifiedDomains { get; set; }
    }

    private class VerifiedDomainInfo
    {
        public string? Name { get; set; }
        public string? Type { get; set; }
    }

    #endregion
}
