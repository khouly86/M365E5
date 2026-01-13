using System.Text.Json;
using Cloudativ.Assessment.Application.Services;
using Cloudativ.Assessment.Domain.Enums;
using Cloudativ.Assessment.Domain.Interfaces;
using Microsoft.Extensions.Logging;

namespace Cloudativ.Assessment.Infrastructure.Modules;

public class IamAssessmentModule : BaseAssessmentModule
{
    private readonly IScoringService _scoringService;

    public IamAssessmentModule(ILogger<IamAssessmentModule> logger, IScoringService scoringService)
        : base(logger)
    {
        _scoringService = scoringService;
    }

    public override AssessmentDomain Domain => AssessmentDomain.IdentityAndAccess;
    public override string DisplayName => "Identity & Access Management";
    public override string Description => "Assesses user accounts, admin roles, MFA status, conditional access policies, and authentication settings.";

    public override IReadOnlyList<string> RequiredPermissions => new[]
    {
        "User.Read.All",
        "Directory.Read.All",
        "RoleManagement.Read.Directory",
        "Policy.Read.All",
        "AuditLog.Read.All",
        "IdentityRiskyUser.Read.All"
    };

    public override async Task<CollectionResult> CollectAsync(IGraphClientWrapper graphClient, CancellationToken cancellationToken = default)
    {
        var rawData = new Dictionary<string, object?>();
        var warnings = new List<string>();
        var unavailableEndpoints = new List<string>();

        try
        {
            // 1. Collect Users
            _logger.LogInformation("Collecting users...");
            try
            {
                var usersJson = await graphClient.GetRawJsonAsync(
                    "users?$select=id,displayName,userPrincipalName,accountEnabled,createdDateTime,signInActivity,userType,assignedLicenses&$top=999",
                    cancellationToken);
                rawData["users"] = usersJson;
            }
            catch (Exception ex)
            {
                warnings.Add($"Failed to collect users: {ex.Message}");
                unavailableEndpoints.Add("users");
            }

            // 2. Collect Directory Roles
            _logger.LogInformation("Collecting directory roles...");
            try
            {
                var rolesJson = await graphClient.GetRawJsonAsync("directoryRoles?$expand=members", cancellationToken);
                rawData["directoryRoles"] = rolesJson;
            }
            catch (Exception ex)
            {
                warnings.Add($"Failed to collect directory roles: {ex.Message}");
                unavailableEndpoints.Add("directoryRoles");
            }

            // 3. Collect Role Definitions
            _logger.LogInformation("Collecting role definitions...");
            try
            {
                var roleDefinitionsJson = await graphClient.GetRawJsonAsync("roleManagement/directory/roleDefinitions", cancellationToken);
                rawData["roleDefinitions"] = roleDefinitionsJson;
            }
            catch (Exception ex)
            {
                warnings.Add($"Failed to collect role definitions: {ex.Message}");
            }

            // 4. Collect Conditional Access Policies
            _logger.LogInformation("Collecting conditional access policies...");
            try
            {
                var caPoliciesJson = await graphClient.GetRawJsonAsync("identity/conditionalAccess/policies", cancellationToken);
                rawData["conditionalAccessPolicies"] = caPoliciesJson;
            }
            catch (Exception ex)
            {
                warnings.Add($"Failed to collect conditional access policies: {ex.Message}");
                unavailableEndpoints.Add("conditionalAccessPolicies");
            }

            // 5. Collect Authentication Methods Policy
            _logger.LogInformation("Collecting authentication methods policy...");
            try
            {
                var authMethodsJson = await graphClient.GetRawJsonAsync("policies/authenticationMethodsPolicy", cancellationToken);
                rawData["authenticationMethodsPolicy"] = authMethodsJson;
            }
            catch (Exception ex)
            {
                warnings.Add($"Failed to collect authentication methods policy: {ex.Message}");
            }

            // 6. Collect Password Policies (via Domain)
            _logger.LogInformation("Collecting domains for password policy...");
            try
            {
                var domainsJson = await graphClient.GetRawJsonAsync("domains", cancellationToken);
                rawData["domains"] = domainsJson;
            }
            catch (Exception ex)
            {
                warnings.Add($"Failed to collect domains: {ex.Message}");
            }

            // 7. Collect Named Locations
            _logger.LogInformation("Collecting named locations...");
            try
            {
                var namedLocationsJson = await graphClient.GetRawJsonAsync("identity/conditionalAccess/namedLocations", cancellationToken);
                rawData["namedLocations"] = namedLocationsJson;
            }
            catch (Exception ex)
            {
                warnings.Add($"Failed to collect named locations: {ex.Message}");
            }

            // 8. Collect Risky Users
            _logger.LogInformation("Collecting risky users...");
            try
            {
                var riskyUsersJson = await graphClient.GetRawJsonAsync("identityProtection/riskyUsers?$filter=riskState eq 'atRisk'", cancellationToken);
                rawData["riskyUsers"] = riskyUsersJson;
            }
            catch (Exception ex)
            {
                warnings.Add($"Failed to collect risky users (may require P2 license): {ex.Message}");
                unavailableEndpoints.Add("riskyUsers");
            }

            // 9. Collect Sign-In Risk Policies
            _logger.LogInformation("Collecting sign-in risk policies...");
            try
            {
                var signInRiskPoliciesJson = await graphClient.GetRawJsonAsync("identity/conditionalAccess/policies?$filter=contains(displayName, 'risk')", cancellationToken);
                rawData["signInRiskPolicies"] = signInRiskPoliciesJson;
            }
            catch (Exception ex)
            {
                warnings.Add($"Failed to collect sign-in risk policies: {ex.Message}");
            }

            // 10. Collect MFA Registration Details
            _logger.LogInformation("Collecting MFA registration details...");
            try
            {
                var mfaDetailsJson = await graphClient.GetRawJsonAsync("reports/credentialUserRegistrationDetails", cancellationToken);
                rawData["mfaRegistrationDetails"] = mfaDetailsJson;
            }
            catch (Exception ex)
            {
                warnings.Add($"Failed to collect MFA registration details: {ex.Message}");
            }

            // 11. Collect SSPR Settings
            _logger.LogInformation("Collecting SSPR settings...");
            try
            {
                var ssprJson = await graphClient.GetRawJsonAsync("policies/authorizationPolicy", cancellationToken);
                rawData["authorizationPolicy"] = ssprJson;
            }
            catch (Exception ex)
            {
                warnings.Add($"Failed to collect authorization policy: {ex.Message}");
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
            _logger.LogError(ex, "Failed to collect IAM data");
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
            var users = ParseJsonCollection<UserInfo>(rawData.RawData.GetValueOrDefault("users") as string);
            var directoryRoles = ParseJsonCollection<DirectoryRoleInfo>(rawData.RawData.GetValueOrDefault("directoryRoles") as string);
            var conditionalAccessPolicies = ParseJsonCollection<CaPolicyInfo>(rawData.RawData.GetValueOrDefault("conditionalAccessPolicies") as string);
            var riskyUsers = ParseJsonCollection<RiskyUserInfo>(rawData.RawData.GetValueOrDefault("riskyUsers") as string);

            // Calculate metrics
            var totalUsers = users.Count;
            var enabledUsers = users.Count(u => u.AccountEnabled == true);
            var guestUsers = users.Count(u => u.UserType == "Guest");
            var adminUsers = GetAdminUsers(directoryRoles);
            var globalAdmins = GetGlobalAdmins(directoryRoles);
            var enabledCaPolicies = conditionalAccessPolicies.Count(p => p.State == "enabled");

            findings.Metrics["totalUsers"] = totalUsers;
            findings.Metrics["enabledUsers"] = enabledUsers;
            findings.Metrics["guestUsers"] = guestUsers;
            findings.Metrics["totalAdmins"] = adminUsers.Count;
            findings.Metrics["globalAdmins"] = globalAdmins.Count;
            findings.Metrics["conditionalAccessPolicies"] = conditionalAccessPolicies.Count;
            findings.Metrics["enabledCaPolicies"] = enabledCaPolicies;
            findings.Metrics["riskyUsersCount"] = riskyUsers.Count;

            // Generate findings

            // Check 1: Global Admin Count
            if (globalAdmins.Count > 5)
            {
                findings.Findings.Add(CreateFinding(
                    "IAM-001",
                    "Excessive Global Administrators",
                    "Too many Global Administrator accounts detected",
                    $"Found {globalAdmins.Count} Global Administrator accounts. Microsoft recommends having 2-4 Global Admins.",
                    Severity.High,
                    false,
                    "Privileged Access",
                    JsonSerializer.Serialize(globalAdmins.Select(u => u.DisplayName)),
                    "Reduce the number of Global Administrators to 2-4. Use least-privilege roles instead.",
                    "https://learn.microsoft.com/en-us/azure/active-directory/roles/best-practices",
                    globalAdmins.Select(u => u.UserPrincipalName ?? u.DisplayName ?? "Unknown").ToList()
                ));
            }
            else
            {
                findings.Findings.Add(CreateFinding(
                    "IAM-001",
                    "Global Administrator Count",
                    "Global Administrator count is within recommended limits",
                    $"Found {globalAdmins.Count} Global Administrator accounts, which is within the recommended 2-4.",
                    Severity.Informational,
                    true,
                    "Privileged Access"
                ));
            }

            // Check 2: Conditional Access Policies
            if (conditionalAccessPolicies.Count == 0)
            {
                findings.Findings.Add(CreateFinding(
                    "IAM-002",
                    "No Conditional Access Policies",
                    "No Conditional Access policies configured",
                    "Conditional Access policies are critical for Zero Trust security. None were found.",
                    Severity.Critical,
                    false,
                    "Conditional Access",
                    remediation: "Create Conditional Access policies to enforce MFA, block risky sign-ins, and control access based on conditions.",
                    references: "https://learn.microsoft.com/en-us/azure/active-directory/conditional-access/overview"
                ));
            }
            else if (enabledCaPolicies == 0)
            {
                findings.Findings.Add(CreateFinding(
                    "IAM-002",
                    "No Enabled Conditional Access Policies",
                    "All Conditional Access policies are disabled",
                    $"Found {conditionalAccessPolicies.Count} Conditional Access policies but none are enabled.",
                    Severity.High,
                    false,
                    "Conditional Access",
                    remediation: "Enable Conditional Access policies after testing in report-only mode."
                ));
            }
            else
            {
                findings.Findings.Add(CreateFinding(
                    "IAM-002",
                    "Conditional Access Policies",
                    "Conditional Access policies are configured and enabled",
                    $"Found {enabledCaPolicies} enabled Conditional Access policies out of {conditionalAccessPolicies.Count} total.",
                    Severity.Informational,
                    true,
                    "Conditional Access"
                ));
            }

            // Check 3: MFA for Admins
            var mfaRequiredForAdmins = conditionalAccessPolicies.Any(p =>
                p.State == "enabled" &&
                p.GrantControls?.BuiltInControls?.Contains("mfa") == true &&
                (p.Conditions?.Users?.IncludeRoles?.Any() == true ||
                 p.Conditions?.Users?.IncludeUsers?.Contains("All") == true));

            if (!mfaRequiredForAdmins)
            {
                findings.Findings.Add(CreateFinding(
                    "IAM-003",
                    "MFA Not Required for Admins",
                    "No Conditional Access policy requires MFA for administrators",
                    "Privileged accounts should always require multi-factor authentication.",
                    Severity.Critical,
                    false,
                    "Multi-Factor Authentication",
                    remediation: "Create a Conditional Access policy requiring MFA for all admin roles.",
                    references: "https://learn.microsoft.com/en-us/azure/active-directory/conditional-access/howto-conditional-access-policy-admin-mfa"
                ));
            }
            else
            {
                findings.Findings.Add(CreateFinding(
                    "IAM-003",
                    "MFA Required for Admins",
                    "MFA is required for administrators via Conditional Access",
                    "A Conditional Access policy requires MFA for privileged accounts.",
                    Severity.Informational,
                    true,
                    "Multi-Factor Authentication"
                ));
            }

            // Check 4: Risky Users
            if (riskyUsers.Count > 0)
            {
                findings.Findings.Add(CreateFinding(
                    "IAM-004",
                    "Risky Users Detected",
                    $"{riskyUsers.Count} users flagged as risky",
                    "Users have been flagged as risky by Azure AD Identity Protection. These should be investigated.",
                    riskyUsers.Count > 10 ? Severity.Critical : Severity.High,
                    false,
                    "Identity Protection",
                    JsonSerializer.Serialize(riskyUsers.Take(20).Select(u => new { u.UserDisplayName, u.RiskLevel })),
                    "Investigate and remediate risky users. Consider requiring password reset or blocking access.",
                    "https://learn.microsoft.com/en-us/azure/active-directory/identity-protection/howto-identity-protection-remediate-unblock",
                    riskyUsers.Select(u => u.UserDisplayName ?? "Unknown").ToList()
                ));
            }
            else
            {
                findings.Findings.Add(CreateFinding(
                    "IAM-004",
                    "No Risky Users",
                    "No users currently flagged as risky",
                    "Azure AD Identity Protection has not flagged any users as risky.",
                    Severity.Informational,
                    true,
                    "Identity Protection"
                ));
            }

            // Check 5: Guest User Count
            var guestPercentage = totalUsers > 0 ? (guestUsers * 100.0 / totalUsers) : 0;
            if (guestPercentage > 20)
            {
                findings.Findings.Add(CreateFinding(
                    "IAM-005",
                    "High Guest User Percentage",
                    $"Guest users represent {guestPercentage:F1}% of all users",
                    $"Found {guestUsers} guest users out of {totalUsers} total users ({guestPercentage:F1}%).",
                    Severity.Medium,
                    false,
                    "External Identities",
                    remediation: "Review guest users and remove stale or unnecessary accounts. Implement guest access reviews.",
                    references: "https://learn.microsoft.com/en-us/azure/active-directory/governance/access-reviews-overview"
                ));
            }
            else
            {
                findings.Findings.Add(CreateFinding(
                    "IAM-005",
                    "Guest User Percentage",
                    "Guest user count is within acceptable limits",
                    $"Found {guestUsers} guest users out of {totalUsers} total users ({guestPercentage:F1}%).",
                    Severity.Informational,
                    true,
                    "External Identities"
                ));
            }

            // Check 6: Block Legacy Authentication
            var blocksLegacyAuth = conditionalAccessPolicies.Any(p =>
                p.State == "enabled" &&
                p.Conditions?.ClientAppTypes?.Contains("other") == true &&
                p.GrantControls?.BuiltInControls?.Contains("block") == true);

            if (!blocksLegacyAuth)
            {
                findings.Findings.Add(CreateFinding(
                    "IAM-006",
                    "Legacy Authentication Not Blocked",
                    "No policy blocking legacy authentication protocols",
                    "Legacy authentication protocols (IMAP, POP3, SMTP) bypass MFA and should be blocked.",
                    Severity.High,
                    false,
                    "Conditional Access",
                    remediation: "Create a Conditional Access policy to block legacy authentication for all users.",
                    references: "https://learn.microsoft.com/en-us/azure/active-directory/conditional-access/block-legacy-authentication"
                ));
            }
            else
            {
                findings.Findings.Add(CreateFinding(
                    "IAM-006",
                    "Legacy Authentication Blocked",
                    "Legacy authentication is blocked via Conditional Access",
                    "A Conditional Access policy blocks legacy authentication protocols.",
                    Severity.Informational,
                    true,
                    "Conditional Access"
                ));
            }

            // Check 7: Disabled Users with Admin Roles
            var disabledAdmins = adminUsers.Where(u => u.AccountEnabled == false).ToList();
            if (disabledAdmins.Any())
            {
                findings.Findings.Add(CreateFinding(
                    "IAM-007",
                    "Disabled Users with Admin Roles",
                    $"{disabledAdmins.Count} disabled users still have admin roles assigned",
                    "Admin roles should be removed from disabled accounts to maintain least privilege.",
                    Severity.Medium,
                    false,
                    "Privileged Access",
                    JsonSerializer.Serialize(disabledAdmins.Select(u => u.DisplayName)),
                    "Remove admin roles from disabled user accounts.",
                    affectedResources: disabledAdmins.Select(u => u.UserPrincipalName ?? "Unknown").ToList()
                ));
            }

            // Check 8: Password Policies
            var authorizationPolicyJson = rawData.RawData.GetValueOrDefault("authorizationPolicy") as string;
            if (!string.IsNullOrEmpty(authorizationPolicyJson))
            {
                try
                {
                    using var doc = JsonDocument.Parse(authorizationPolicyJson);
                    var allowPasswordReset = doc.RootElement.TryGetProperty("allowedToUseSSPR", out var sspr) && sspr.GetBoolean();

                    if (!allowPasswordReset)
                    {
                        findings.Findings.Add(CreateFinding(
                            "IAM-008",
                            "Self-Service Password Reset Disabled",
                            "Self-Service Password Reset (SSPR) is not enabled",
                            "SSPR reduces helpdesk burden and improves security by allowing users to securely reset passwords.",
                            Severity.Medium,
                            false,
                            "Password Management",
                            remediation: "Enable Self-Service Password Reset for all or selected users.",
                            references: "https://learn.microsoft.com/en-us/azure/active-directory/authentication/howto-sspr-deployment"
                        ));
                    }
                    else
                    {
                        findings.Findings.Add(CreateFinding(
                            "IAM-008",
                            "Self-Service Password Reset Enabled",
                            "Self-Service Password Reset (SSPR) is enabled",
                            "Users can securely reset their passwords without helpdesk assistance.",
                            Severity.Informational,
                            true,
                            "Password Management"
                        ));
                    }
                }
                catch { }
            }

            // Generate summary
            findings.Summary.Add($"Total Users: {totalUsers} ({enabledUsers} enabled, {guestUsers} guests)");
            findings.Summary.Add($"Admin Users: {adminUsers.Count} ({globalAdmins.Count} Global Admins)");
            findings.Summary.Add($"Conditional Access Policies: {conditionalAccessPolicies.Count} ({enabledCaPolicies} enabled)");
            findings.Summary.Add($"Risky Users: {riskyUsers.Count}");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error normalizing IAM findings");
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

    private List<UserInfo> GetAdminUsers(List<DirectoryRoleInfo> roles)
    {
        var adminUsers = new List<UserInfo>();
        var seenIds = new HashSet<string>();

        foreach (var role in roles)
        {
            if (role.Members != null)
            {
                foreach (var member in role.Members)
                {
                    if (member.OdataType == "#microsoft.graph.user" && !seenIds.Contains(member.Id ?? ""))
                    {
                        seenIds.Add(member.Id ?? "");
                        adminUsers.Add(new UserInfo
                        {
                            Id = member.Id,
                            DisplayName = member.DisplayName,
                            UserPrincipalName = member.UserPrincipalName,
                            AccountEnabled = true // Assume enabled for role members
                        });
                    }
                }
            }
        }

        return adminUsers;
    }

    private List<UserInfo> GetGlobalAdmins(List<DirectoryRoleInfo> roles)
    {
        var globalAdminRole = roles.FirstOrDefault(r =>
            r.DisplayName?.Contains("Global Administrator", StringComparison.OrdinalIgnoreCase) == true ||
            r.RoleTemplateId == "62e90394-69f5-4237-9190-012177145e10");

        if (globalAdminRole?.Members == null)
            return new List<UserInfo>();

        return globalAdminRole.Members
            .Where(m => m.OdataType == "#microsoft.graph.user")
            .Select(m => new UserInfo
            {
                Id = m.Id,
                DisplayName = m.DisplayName,
                UserPrincipalName = m.UserPrincipalName,
                AccountEnabled = true
            })
            .ToList();
    }

    private class UserInfo
    {
        public string? Id { get; set; }
        public string? DisplayName { get; set; }
        public string? UserPrincipalName { get; set; }
        public bool? AccountEnabled { get; set; }
        public string? UserType { get; set; }
    }

    private class DirectoryRoleInfo
    {
        public string? Id { get; set; }
        public string? DisplayName { get; set; }
        public string? RoleTemplateId { get; set; }
        public List<RoleMemberInfo>? Members { get; set; }
    }

    private class RoleMemberInfo
    {
        public string? Id { get; set; }
        public string? DisplayName { get; set; }
        public string? UserPrincipalName { get; set; }
        public string? OdataType { get; set; }
    }

    private class CaPolicyInfo
    {
        public string? Id { get; set; }
        public string? DisplayName { get; set; }
        public string? State { get; set; }
        public CaConditions? Conditions { get; set; }
        public CaGrantControls? GrantControls { get; set; }
    }

    private class CaConditions
    {
        public CaUsers? Users { get; set; }
        public List<string>? ClientAppTypes { get; set; }
    }

    private class CaUsers
    {
        public List<string>? IncludeUsers { get; set; }
        public List<string>? IncludeRoles { get; set; }
    }

    private class CaGrantControls
    {
        public List<string>? BuiltInControls { get; set; }
    }

    private class RiskyUserInfo
    {
        public string? Id { get; set; }
        public string? UserDisplayName { get; set; }
        public string? RiskLevel { get; set; }
        public string? RiskState { get; set; }
    }

    #endregion
}
