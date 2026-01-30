using System.Text.Json;
using Cloudativ.Assessment.Application.Services;
using Cloudativ.Assessment.Domain.Enums;
using Cloudativ.Assessment.Domain.Interfaces;
using Microsoft.Extensions.Logging;

namespace Cloudativ.Assessment.Infrastructure.Modules;

public class AppGovernanceAssessmentModule : BaseAssessmentModule
{
    private readonly IScoringService _scoringService;

    public AppGovernanceAssessmentModule(ILogger<AppGovernanceAssessmentModule> logger, IScoringService scoringService)
        : base(logger)
    {
        _scoringService = scoringService;
    }

    public override AssessmentDomain Domain => AssessmentDomain.AppGovernance;
    public override string DisplayName => "App Governance & Consent";
    public override string Description => "Assesses OAuth apps, consent policies, app permissions, enterprise applications, and application security.";

    public override IReadOnlyList<string> RequiredPermissions => new[]
    {
        "Application.Read.All",
        "Directory.Read.All",
        "Policy.Read.All",
        "AppRoleAssignment.ReadWrite.All"
    };

    public override async Task<CollectionResult> CollectAsync(IGraphClientWrapper graphClient, CancellationToken cancellationToken = default)
    {
        var rawData = new Dictionary<string, object?>();
        var warnings = new List<string>();
        var unavailableEndpoints = new List<string>();

        try
        {
            // 1. Collect Enterprise Applications (Service Principals)
            _logger.LogInformation("Collecting enterprise applications...");
            try
            {
                var appsJson = await graphClient.GetRawJsonAsync(
                    "servicePrincipals?$select=id,displayName,appId,accountEnabled,servicePrincipalType,tags,appRoleAssignmentRequired,oauth2PermissionScopes&$top=999",
                    cancellationToken);
                rawData["enterpriseApps"] = appsJson;
            }
            catch (Exception ex)
            {
                warnings.Add($"Failed to collect enterprise applications: {ex.Message}");
                unavailableEndpoints.Add("enterpriseApps");
            }

            // 2. Collect App Registrations
            _logger.LogInformation("Collecting app registrations...");
            try
            {
                var appRegistrationsJson = await graphClient.GetRawJsonAsync(
                    "applications?$select=id,displayName,appId,createdDateTime,signInAudience,requiredResourceAccess,passwordCredentials,keyCredentials&$top=999",
                    cancellationToken);
                rawData["appRegistrations"] = appRegistrationsJson;
            }
            catch (Exception ex)
            {
                warnings.Add($"Failed to collect app registrations: {ex.Message}");
            }

            // 3. Collect OAuth2 Permission Grants (User Consents)
            _logger.LogInformation("Collecting OAuth2 permission grants...");
            try
            {
                var oauthGrantsJson = await graphClient.GetRawJsonAsync(
                    "oauth2PermissionGrants?$top=999",
                    cancellationToken);
                rawData["oauth2Grants"] = oauthGrantsJson;
            }
            catch (Exception ex)
            {
                warnings.Add($"Failed to collect OAuth2 grants: {ex.Message}");
                unavailableEndpoints.Add("oauth2Grants");
            }

            // 4. Collect Admin Consent Requests
            _logger.LogInformation("Collecting admin consent requests...");
            try
            {
                var consentRequestsJson = await graphClient.GetRawJsonAsync(
                    "identityGovernance/appConsent/appConsentRequests",
                    cancellationToken);
                rawData["consentRequests"] = consentRequestsJson;
            }
            catch (Exception ex)
            {
                warnings.Add($"Failed to collect consent requests: {ex.Message}");
            }

            // 5. Collect Permission Grant Policies
            _logger.LogInformation("Collecting permission grant policies...");
            try
            {
                var grantPoliciesJson = await graphClient.GetRawJsonAsync(
                    "policies/permissionGrantPolicies",
                    cancellationToken);
                rawData["permissionGrantPolicies"] = grantPoliciesJson;
            }
            catch (Exception ex)
            {
                warnings.Add($"Failed to collect permission grant policies: {ex.Message}");
            }

            // 6. Collect Authorization Policy (for consent settings)
            _logger.LogInformation("Collecting authorization policy...");
            try
            {
                var authPolicyJson = await graphClient.GetRawJsonAsync(
                    "policies/authorizationPolicy",
                    cancellationToken);
                rawData["authorizationPolicy"] = authPolicyJson;
            }
            catch (Exception ex)
            {
                warnings.Add($"Failed to collect authorization policy: {ex.Message}");
            }

            // 7. Collect App Role Assignments
            _logger.LogInformation("Collecting app role assignments...");
            try
            {
                // Get top apps with role assignments
                var roleAssignmentsJson = await graphClient.GetRawJsonAsync(
                    "servicePrincipals?$select=id,displayName,appRoleAssignedTo&$expand=appRoleAssignedTo&$top=100",
                    cancellationToken);
                rawData["appRoleAssignments"] = roleAssignmentsJson;
            }
            catch (Exception ex)
            {
                warnings.Add($"Failed to collect app role assignments: {ex.Message}");
            }

            // 8. Collect Credential Policies
            _logger.LogInformation("Collecting credential policies...");
            try
            {
                var credPoliciesJson = await graphClient.GetRawJsonAsync(
                    "policies/appManagementPolicies",
                    cancellationToken);
                rawData["appManagementPolicies"] = credPoliciesJson;
            }
            catch (Exception ex)
            {
                warnings.Add($"Failed to collect app management policies: {ex.Message}");
            }

            // 9. Collect Service Principal Sign-Ins
            _logger.LogInformation("Collecting service principal sign-ins...");
            try
            {
                var spSignInsJson = await graphClient.GetRawJsonAsync(
                    "auditLogs/signIns?$filter=signInEventTypes/any(t:t eq 'servicePrincipal')&$top=50",
                    cancellationToken);
                rawData["servicePrincipalSignIns"] = spSignInsJson;
            }
            catch (Exception ex)
            {
                warnings.Add($"Failed to collect service principal sign-ins: {ex.Message}");
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
            _logger.LogError(ex, "Failed to collect App Governance data");
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
            var enterpriseApps = ParseJsonCollection<ServicePrincipalInfo>(
                rawData.RawData.GetValueOrDefault("enterpriseApps") as string);
            var appRegistrations = ParseJsonCollection<AppRegistrationInfo>(
                rawData.RawData.GetValueOrDefault("appRegistrations") as string);
            var oauth2Grants = ParseJsonCollection<OAuth2GrantInfo>(
                rawData.RawData.GetValueOrDefault("oauth2Grants") as string);
            var consentRequests = ParseJsonCollection<ConsentRequestInfo>(
                rawData.RawData.GetValueOrDefault("consentRequests") as string);
            var permissionGrantPolicies = ParseJsonCollection<PermissionGrantPolicyInfo>(
                rawData.RawData.GetValueOrDefault("permissionGrantPolicies") as string);
            var authPolicy = ParseAuthorizationPolicy(rawData.RawData.GetValueOrDefault("authorizationPolicy") as string);

            // High-risk permission scopes to look for
            var highRiskScopes = new HashSet<string>(StringComparer.OrdinalIgnoreCase)
            {
                "Mail.ReadWrite", "Mail.ReadWrite.All", "Mail.Send",
                "Files.ReadWrite.All", "Sites.ReadWrite.All",
                "User.ReadWrite.All", "Directory.ReadWrite.All",
                "RoleManagement.ReadWrite.Directory",
                "Application.ReadWrite.All", "AppRoleAssignment.ReadWrite.All",
                "MailboxSettings.ReadWrite", "Calendars.ReadWrite"
            };

            // Calculate metrics
            var enabledApps = enterpriseApps.Count(a => a.AccountEnabled == true);
            var disabledApps = enterpriseApps.Count(a => a.AccountEnabled == false);
            var userConsentedApps = oauth2Grants.Where(g => g.ConsentType == "Principal").Select(g => g.ClientId).Distinct().Count();
            var adminConsentedApps = oauth2Grants.Where(g => g.ConsentType == "AllPrincipals").Select(g => g.ClientId).Distinct().Count();
            var pendingConsentRequests = consentRequests.Count(r => r.Status == "InProgress");

            // Identify high-risk apps with their permissions
            var highRiskAppDetails = oauth2Grants
                .Where(g => g.Scope?.Split(' ').Any(s => highRiskScopes.Contains(s)) == true)
                .GroupBy(g => g.ClientId)
                .Select(g =>
                {
                    var app = enterpriseApps.FirstOrDefault(a => a.AppId == g.Key);
                    var grantedScopes = g.SelectMany(grant => grant.Scope?.Split(' ') ?? Array.Empty<string>())
                        .Where(s => highRiskScopes.Contains(s))
                        .Distinct()
                        .ToList();
                    return new
                    {
                        AppId = g.Key,
                        AppName = app?.DisplayName ?? "Unknown App",
                        HighRiskPermissions = grantedScopes,
                        ConsentTypes = g.Select(x => x.ConsentType).Distinct().ToList()
                    };
                })
                .ToList();

            var highRiskApps = highRiskAppDetails.Select(a => a.AppId).ToList();

            // Apps with expiring credentials
            var appsWithExpiringCredentials = appRegistrations
                .Where(a => a.PasswordCredentials?.Any(c =>
                    c.EndDateTime.HasValue &&
                    c.EndDateTime < DateTime.UtcNow.AddDays(30) &&
                    c.EndDateTime > DateTime.UtcNow) == true ||
                    a.KeyCredentials?.Any(c =>
                    c.EndDateTime.HasValue &&
                    c.EndDateTime < DateTime.UtcNow.AddDays(30) &&
                    c.EndDateTime > DateTime.UtcNow) == true)
                .ToList();

            // Apps with expired credentials
            var appsWithExpiredCredentials = appRegistrations
                .Where(a => a.PasswordCredentials?.Any(c =>
                    c.EndDateTime.HasValue && c.EndDateTime < DateTime.UtcNow) == true ||
                    a.KeyCredentials?.Any(c =>
                    c.EndDateTime.HasValue && c.EndDateTime < DateTime.UtcNow) == true)
                .ToList();

            findings.Metrics["enterpriseAppsCount"] = enterpriseApps.Count;
            findings.Metrics["enabledApps"] = enabledApps;
            findings.Metrics["appRegistrationsCount"] = appRegistrations.Count;
            findings.Metrics["oauthAppsCount"] = oauth2Grants.Select(g => g.ClientId).Distinct().Count();
            findings.Metrics["userConsentedApps"] = userConsentedApps;
            findings.Metrics["adminConsentedApps"] = adminConsentedApps;
            findings.Metrics["highRiskAppsCount"] = highRiskApps.Count;
            findings.Metrics["pendingConsentRequests"] = pendingConsentRequests;
            findings.Metrics["adminConsentRequired"] = authPolicy?.AllowUserConsentForApps == false ? 1 : 0;

            // Check 1: User Consent Settings
            if (authPolicy?.AllowUserConsentForApps == true)
            {
                findings.Findings.Add(CreateFinding(
                    "APP-001",
                    "User Consent Enabled",
                    "Users can consent to apps without admin approval",
                    "Users can grant apps access to organizational data, potentially introducing risky applications.",
                    Severity.High,
                    false,
                    "Consent Policy",
                    remediation: "Configure user consent to require admin approval or limit to verified publishers.",
                    references: "https://learn.microsoft.com/en-us/azure/active-directory/manage-apps/configure-user-consent"
                ));
            }
            else
            {
                findings.Findings.Add(CreateFinding(
                    "APP-001",
                    "Admin Consent Required",
                    "Admin approval is required for app consent",
                    "Users cannot grant permissions to apps without administrator approval.",
                    Severity.Informational,
                    true,
                    "Consent Policy"
                ));
            }

            // Check 2: High-Risk Permission Grants
            if (highRiskApps.Count > 0)
            {
                // Create detailed evidence with app names and permissions
                var evidenceData = highRiskAppDetails.Take(20).Select(a => new
                {
                    a.AppName,
                    a.AppId,
                    Permissions = string.Join(", ", a.HighRiskPermissions),
                    ConsentType = a.ConsentTypes.Contains("AllPrincipals") ? "Admin Consent" : "User Consent"
                }).ToList();

                // Create human-readable affected resources list
                var affectedResourcesList = highRiskAppDetails.Take(20)
                    .Select(a => $"{a.AppName} ({string.Join(", ", a.HighRiskPermissions)})")
                    .ToList();

                findings.Findings.Add(CreateFinding(
                    "APP-002",
                    "High-Risk App Permissions",
                    $"{highRiskApps.Count} apps have high-risk permissions granted",
                    "These apps have permissions that allow significant access to organizational data including mail, files, and directory operations.",
                    Severity.High,
                    false,
                    "Permission Management",
                    evidence: JsonSerializer.Serialize(evidenceData, new JsonSerializerOptions { WriteIndented = true }),
                    remediation: "Review apps with high-risk permissions and revoke unnecessary access. Consider implementing app consent policies.",
                    affectedResources: affectedResourcesList,
                    references: "https://learn.microsoft.com/en-us/azure/active-directory/manage-apps/manage-application-permissions"
                ));
            }
            else
            {
                findings.Findings.Add(CreateFinding(
                    "APP-002",
                    "No High-Risk Permissions",
                    "No apps with high-risk permissions detected",
                    "No applications have been granted highly privileged permissions.",
                    Severity.Informational,
                    true,
                    "Permission Management"
                ));
            }

            // Check 3: User-Consented Apps
            if (userConsentedApps > 20)
            {
                findings.Findings.Add(CreateFinding(
                    "APP-003",
                    "Many User-Consented Apps",
                    $"{userConsentedApps} apps have user-level consent",
                    "A large number of user-consented apps may indicate shadow IT or consent fatigue.",
                    Severity.Medium,
                    false,
                    "User Consent",
                    remediation: "Review user-consented apps and implement consent workflow for admin review."
                ));
            }
            else
            {
                findings.Findings.Add(CreateFinding(
                    "APP-003",
                    "User-Consented Apps",
                    $"{userConsentedApps} apps have user-level consent",
                    "User app consent is within manageable levels.",
                    Severity.Informational,
                    true,
                    "User Consent"
                ));
            }

            // Check 4: Pending Consent Requests
            if (pendingConsentRequests > 0)
            {
                findings.Findings.Add(CreateFinding(
                    "APP-004",
                    "Pending Consent Requests",
                    $"{pendingConsentRequests} consent requests awaiting review",
                    "Admin consent requests should be reviewed promptly.",
                    Severity.Low,
                    false,
                    "Consent Workflow",
                    remediation: "Review pending consent requests and approve or deny based on business need."
                ));
            }
            else
            {
                findings.Findings.Add(CreateFinding(
                    "APP-004",
                    "No Pending Requests",
                    "No pending consent requests",
                    "All consent requests have been processed.",
                    Severity.Informational,
                    true,
                    "Consent Workflow"
                ));
            }

            // Check 5: Expiring Credentials
            if (appsWithExpiringCredentials.Count > 0)
            {
                findings.Findings.Add(CreateFinding(
                    "APP-005",
                    "Expiring App Credentials",
                    $"{appsWithExpiringCredentials.Count} apps have credentials expiring within 30 days",
                    "App credentials should be rotated before expiration to avoid service disruptions.",
                    Severity.Medium,
                    false,
                    "Credential Management",
                    remediation: "Rotate credentials for apps with expiring secrets or certificates.",
                    affectedResources: appsWithExpiringCredentials.Select(a => a.DisplayName ?? "Unknown").ToList()
                ));
            }

            // Check 6: Expired Credentials
            if (appsWithExpiredCredentials.Count > 0)
            {
                findings.Findings.Add(CreateFinding(
                    "APP-006",
                    "Expired App Credentials",
                    $"{appsWithExpiredCredentials.Count} apps have expired credentials",
                    "Apps with expired credentials may not be functioning or may be abandoned.",
                    Severity.Medium,
                    false,
                    "Credential Management",
                    remediation: "Review apps with expired credentials and either update or remove them.",
                    affectedResources: appsWithExpiredCredentials.Select(a => a.DisplayName ?? "Unknown").ToList()
                ));
            }

            // Check 7: Multi-Tenant Apps
            var multiTenantApps = appRegistrations.Count(a =>
                a.SignInAudience?.Contains("AzureADMultipleOrgs", StringComparison.OrdinalIgnoreCase) == true ||
                a.SignInAudience?.Contains("PersonalMicrosoftAccount", StringComparison.OrdinalIgnoreCase) == true);

            if (multiTenantApps > 0)
            {
                findings.Findings.Add(CreateFinding(
                    "APP-007",
                    "Multi-Tenant Apps",
                    $"{multiTenantApps} multi-tenant app registrations found",
                    "Multi-tenant apps can be accessed from other organizations. Ensure this is intentional.",
                    Severity.Low,
                    false,
                    "App Configuration",
                    remediation: "Review multi-tenant apps to ensure external access is intended."
                ));
            }
            else
            {
                findings.Findings.Add(CreateFinding(
                    "APP-007",
                    "Single-Tenant Apps",
                    "All app registrations are single-tenant",
                    "Apps are restricted to this organization only.",
                    Severity.Informational,
                    true,
                    "App Configuration"
                ));
            }

            // Check 8: Disabled Apps with Permissions
            var disabledAppsWithGrants = enterpriseApps
                .Where(a => a.AccountEnabled == false &&
                       oauth2Grants.Any(g => g.ClientId == a.AppId))
                .ToList();

            if (disabledAppsWithGrants.Count > 0)
            {
                findings.Findings.Add(CreateFinding(
                    "APP-008",
                    "Disabled Apps with Permissions",
                    $"{disabledAppsWithGrants.Count} disabled apps still have permission grants",
                    "Disabled apps with permissions should have their grants revoked.",
                    Severity.Low,
                    false,
                    "App Cleanup",
                    remediation: "Revoke permissions from disabled applications.",
                    affectedResources: disabledAppsWithGrants.Select(a => a.DisplayName ?? "Unknown").ToList()
                ));
            }

            // Generate summary
            findings.Summary.Add($"Enterprise Apps: {enterpriseApps.Count} ({enabledApps} enabled)");
            findings.Summary.Add($"App Registrations: {appRegistrations.Count}");
            findings.Summary.Add($"OAuth Consents: {userConsentedApps} user, {adminConsentedApps} admin");
            findings.Summary.Add($"High-Risk Apps: {highRiskApps.Count}");
            if (rawData.Warnings.Any())
            {
                findings.Summary.Add($"Note: {rawData.Warnings.Count} data points could not be collected");
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error normalizing App Governance findings");
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

    private AuthorizationPolicyInfo? ParseAuthorizationPolicy(string? json)
    {
        if (string.IsNullOrEmpty(json))
            return null;

        try
        {
            return JsonSerializer.Deserialize<AuthorizationPolicyInfo>(json, new JsonSerializerOptions
            {
                PropertyNameCaseInsensitive = true
            });
        }
        catch
        {
            return null;
        }
    }

    private class ServicePrincipalInfo
    {
        public string? Id { get; set; }
        public string? DisplayName { get; set; }
        public string? AppId { get; set; }
        public bool? AccountEnabled { get; set; }
        public string? ServicePrincipalType { get; set; }
        public List<string>? Tags { get; set; }
    }

    private class AppRegistrationInfo
    {
        public string? Id { get; set; }
        public string? DisplayName { get; set; }
        public string? AppId { get; set; }
        public DateTime? CreatedDateTime { get; set; }
        public string? SignInAudience { get; set; }
        public List<CredentialInfo>? PasswordCredentials { get; set; }
        public List<CredentialInfo>? KeyCredentials { get; set; }
    }

    private class CredentialInfo
    {
        public string? KeyId { get; set; }
        public string? DisplayName { get; set; }
        public DateTime? StartDateTime { get; set; }
        public DateTime? EndDateTime { get; set; }
    }

    private class OAuth2GrantInfo
    {
        public string? Id { get; set; }
        public string? ClientId { get; set; }
        public string? ConsentType { get; set; }
        public string? PrincipalId { get; set; }
        public string? ResourceId { get; set; }
        public string? Scope { get; set; }
    }

    private class ConsentRequestInfo
    {
        public string? Id { get; set; }
        public string? AppDisplayName { get; set; }
        public string? Status { get; set; }
    }

    private class PermissionGrantPolicyInfo
    {
        public string? Id { get; set; }
        public string? DisplayName { get; set; }
        public string? Description { get; set; }
    }

    private class AuthorizationPolicyInfo
    {
        public bool? AllowUserConsentForApps { get; set; }
        public bool? AllowedToUseSSPR { get; set; }
        public bool? AllowedToSignUpEmailBasedSubscriptions { get; set; }
    }

    #endregion
}
