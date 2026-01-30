using System.Text.Json;
using Cloudativ.Assessment.Application.Services;
using Cloudativ.Assessment.Domain.Enums;
using Cloudativ.Assessment.Domain.Interfaces;
using Microsoft.Extensions.Logging;

namespace Cloudativ.Assessment.Infrastructure.Modules;

public class PrivilegedAccessAssessmentModule : BaseAssessmentModule
{
    private readonly IScoringService _scoringService;

    public PrivilegedAccessAssessmentModule(ILogger<PrivilegedAccessAssessmentModule> logger, IScoringService scoringService)
        : base(logger)
    {
        _scoringService = scoringService;
    }

    public override AssessmentDomain Domain => AssessmentDomain.PrivilegedAccess;
    public override string DisplayName => "Privileged Access Management";
    public override string Description => "Assesses PIM configuration, role assignments, just-in-time access, and administrative privileges.";

    public override IReadOnlyList<string> RequiredPermissions => new[]
    {
        "RoleManagement.Read.Directory",
        "RoleManagement.Read.All",
        "PrivilegedAccess.Read.AzureAD",
        "Directory.Read.All"
    };

    public override async Task<CollectionResult> CollectAsync(IGraphClientWrapper graphClient, CancellationToken cancellationToken = default)
    {
        var rawData = new Dictionary<string, object?>();
        var warnings = new List<string>();
        var unavailableEndpoints = new List<string>();

        try
        {
            // 1. Collect PIM Role Assignments (Eligible)
            _logger.LogInformation("Collecting PIM eligible role assignments...");
            try
            {
                var eligibleAssignmentsJson = await graphClient.GetRawJsonAsync(
                    "roleManagement/directory/roleEligibilityScheduleInstances",
                    cancellationToken);
                rawData["eligibleAssignments"] = eligibleAssignmentsJson;
            }
            catch (Exception ex)
            {
                warnings.Add($"Failed to collect eligible role assignments (may require P2 license): {ex.Message}");
                unavailableEndpoints.Add("eligibleAssignments");
            }

            // 2. Collect PIM Role Assignments (Active)
            _logger.LogInformation("Collecting PIM active role assignments...");
            try
            {
                var activeAssignmentsJson = await graphClient.GetRawJsonAsync(
                    "roleManagement/directory/roleAssignmentScheduleInstances",
                    cancellationToken);
                rawData["activeAssignments"] = activeAssignmentsJson;
            }
            catch (Exception ex)
            {
                warnings.Add($"Failed to collect active role assignments: {ex.Message}");
                unavailableEndpoints.Add("activeAssignments");
            }

            // 3. Collect Directory Role Definitions
            _logger.LogInformation("Collecting role definitions...");
            try
            {
                var roleDefinitionsJson = await graphClient.GetRawJsonAsync(
                    "roleManagement/directory/roleDefinitions",
                    cancellationToken);
                rawData["roleDefinitions"] = roleDefinitionsJson;
            }
            catch (Exception ex)
            {
                warnings.Add($"Failed to collect role definitions: {ex.Message}");
            }

            // 4. Collect Directory Roles with Members
            _logger.LogInformation("Collecting directory roles...");
            try
            {
                var directoryRolesJson = await graphClient.GetRawJsonAsync(
                    "directoryRoles?$expand=members",
                    cancellationToken);
                rawData["directoryRoles"] = directoryRolesJson;
            }
            catch (Exception ex)
            {
                warnings.Add($"Failed to collect directory roles: {ex.Message}");
            }

            // 5. Collect PIM Settings for Roles
            _logger.LogInformation("Collecting PIM role settings...");
            try
            {
                var roleSettingsJson = await graphClient.GetRawJsonAsync(
                    "policies/roleManagementPolicies?$filter=scopeId eq '/' and scopeType eq 'DirectoryRole'",
                    cancellationToken);
                rawData["roleManagementPolicies"] = roleSettingsJson;
            }
            catch (Exception ex)
            {
                warnings.Add($"Failed to collect role management policies: {ex.Message}");
            }

            // 6. Collect Privileged Access Groups
            _logger.LogInformation("Collecting privileged access groups...");
            try
            {
                var privilegedGroupsJson = await graphClient.GetRawJsonAsync(
                    "groups?$filter=isAssignableToRole eq true&$expand=members",
                    cancellationToken);
                rawData["privilegedGroups"] = privilegedGroupsJson;
            }
            catch (Exception ex)
            {
                warnings.Add($"Failed to collect role-assignable groups: {ex.Message}");
            }

            // 7. Collect Admin Consent Requests
            _logger.LogInformation("Collecting admin consent requests...");
            try
            {
                var consentRequestsJson = await graphClient.GetRawJsonAsync(
                    "identityGovernance/appConsent/appConsentRequests?$filter=status eq 'InProgress'",
                    cancellationToken);
                rawData["adminConsentRequests"] = consentRequestsJson;
            }
            catch (Exception ex)
            {
                warnings.Add($"Failed to collect admin consent requests: {ex.Message}");
            }

            // 8. Collect Users for cross-referencing
            _logger.LogInformation("Collecting users for reference...");
            try
            {
                var usersJson = await graphClient.GetRawJsonAsync(
                    "users?$select=id,displayName,userPrincipalName,accountEnabled&$top=999",
                    cancellationToken);
                rawData["users"] = usersJson;
            }
            catch (Exception ex)
            {
                warnings.Add($"Failed to collect users: {ex.Message}");
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
            _logger.LogError(ex, "Failed to collect Privileged Access data");
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
            var eligibleAssignments = ParseJsonCollection<RoleAssignmentInfo>(
                rawData.RawData.GetValueOrDefault("eligibleAssignments") as string);
            var activeAssignments = ParseJsonCollection<RoleAssignmentInfo>(
                rawData.RawData.GetValueOrDefault("activeAssignments") as string);
            var directoryRoles = ParseJsonCollection<DirectoryRoleInfo>(
                rawData.RawData.GetValueOrDefault("directoryRoles") as string);
            var roleDefinitions = ParseJsonCollection<RoleDefinitionInfo>(
                rawData.RawData.GetValueOrDefault("roleDefinitions") as string);
            var privilegedGroups = ParseJsonCollection<GroupInfo>(
                rawData.RawData.GetValueOrDefault("privilegedGroups") as string);

            // Identify high-privilege roles
            var highPrivilegeRoleIds = new HashSet<string>
            {
                "62e90394-69f5-4237-9190-012177145e10", // Global Administrator
                "e8611ab8-c189-46e8-94e1-60213ab1f814", // Privileged Role Administrator
                "194ae4cb-b126-40b2-bd5b-6091b380977d", // Security Administrator
                "9b895d92-2cd3-44c7-9d02-a6ac2d5ea5c3", // Application Administrator
                "158c047a-c907-4556-b7ef-446551a6b5f7", // Cloud Application Administrator
                "b1be1c3e-b65d-4f19-8427-f6fa0d97feb9", // Conditional Access Administrator
                "29232cdf-9323-42fd-ade2-1d097af3e4de", // Exchange Administrator
                "fe930be7-5e62-47db-91af-98c3a49a38b1", // User Administrator
                "fdd7a751-b60b-444a-984c-02652fe8fa1c"  // Groups Administrator
            };

            // Calculate metrics
            var permanentAdmins = GetPermanentAssignments(directoryRoles, highPrivilegeRoleIds);
            var eligibleAdmins = eligibleAssignments.Count(a => highPrivilegeRoleIds.Contains(a.RoleDefinitionId ?? ""));
            var globalAdminCount = directoryRoles
                .FirstOrDefault(r => r.RoleTemplateId == "62e90394-69f5-4237-9190-012177145e10")
                ?.Members?.Count ?? 0;

            findings.Metrics["totalPrivilegedRoles"] = directoryRoles.Count;
            findings.Metrics["permanentAdmins"] = permanentAdmins.Count;
            findings.Metrics["eligibleAssignments"] = eligibleAssignments.Count;
            findings.Metrics["activeJitAssignments"] = activeAssignments.Count;
            findings.Metrics["globalAdminCount"] = globalAdminCount;
            findings.Metrics["roleAssignableGroups"] = privilegedGroups.Count;

            // Check 1: Excessive Global Administrators
            if (globalAdminCount > 5)
            {
                findings.Findings.Add(CreateFinding(
                    "PAM-001",
                    "Excessive Global Administrators",
                    $"{globalAdminCount} Global Administrator accounts detected",
                    "Microsoft recommends having 2-4 Global Administrators. Excessive admins increase attack surface.",
                    Severity.High,
                    false,
                    "Global Admin Management",
                    remediation: "Reduce Global Administrators to 2-4. Use least-privilege roles for specific tasks.",
                    references: "https://learn.microsoft.com/en-us/azure/active-directory/roles/best-practices"
                ));
            }
            else if (globalAdminCount >= 2 && globalAdminCount <= 5)
            {
                findings.Findings.Add(CreateFinding(
                    "PAM-001",
                    "Global Administrator Count",
                    "Global Administrator count is within recommended limits",
                    $"Found {globalAdminCount} Global Administrators (recommended: 2-4).",
                    Severity.Informational,
                    true,
                    "Global Admin Management"
                ));
            }
            else if (globalAdminCount < 2)
            {
                findings.Findings.Add(CreateFinding(
                    "PAM-001",
                    "Insufficient Global Administrators",
                    "Less than 2 Global Administrators configured",
                    "Having fewer than 2 Global Administrators risks account lockout scenarios.",
                    Severity.Medium,
                    false,
                    "Global Admin Management",
                    remediation: "Ensure at least 2 Global Administrator accounts exist for emergency access."
                ));
            }

            // Check 2: PIM Configuration
            bool pimEnabled = eligibleAssignments.Any() || !rawData.UnavailableEndpoints.Contains("eligibleAssignments");
            if (rawData.UnavailableEndpoints.Contains("eligibleAssignments"))
            {
                findings.Findings.Add(CreateFinding(
                    "PAM-002",
                    "PIM Not Configured",
                    "Privileged Identity Management is not enabled or inaccessible",
                    "PIM provides just-in-time access and reduces standing privileges. It may require Azure AD P2 license.",
                    Severity.High,
                    false,
                    "Just-In-Time Access",
                    remediation: "Enable Azure AD Privileged Identity Management to implement just-in-time access for privileged roles.",
                    references: "https://learn.microsoft.com/en-us/azure/active-directory/privileged-identity-management/pim-configure"
                ));
            }
            else if (eligibleAssignments.Count == 0 && permanentAdmins.Count > 0)
            {
                findings.Findings.Add(CreateFinding(
                    "PAM-002",
                    "No Eligible Role Assignments",
                    "No eligible (JIT) role assignments found",
                    $"Found {permanentAdmins.Count} permanent admin assignments but no eligible/JIT assignments configured.",
                    Severity.High,
                    false,
                    "Just-In-Time Access",
                    remediation: "Convert permanent role assignments to eligible assignments requiring activation."
                ));
            }
            else
            {
                findings.Findings.Add(CreateFinding(
                    "PAM-002",
                    "PIM Configured",
                    "Privileged Identity Management is configured with eligible assignments",
                    $"Found {eligibleAssignments.Count} eligible role assignments.",
                    Severity.Informational,
                    true,
                    "Just-In-Time Access"
                ));
            }

            // Check 3: Standing Privileged Access
            var permanentHighPrivilege = permanentAdmins.Where(a =>
                highPrivilegeRoleIds.Contains(a.RoleTemplateId ?? "")).ToList();

            if (permanentHighPrivilege.Count > 2)
            {
                findings.Findings.Add(CreateFinding(
                    "PAM-003",
                    "Excessive Standing Privileges",
                    $"{permanentHighPrivilege.Count} permanent high-privilege role assignments",
                    "Standing privileged access should be minimized. Use eligible assignments instead.",
                    Severity.High,
                    false,
                    "Standing Access",
                    evidence: JsonSerializer.Serialize(permanentHighPrivilege.Take(10).Select(r => r.DisplayName)),
                    remediation: "Convert permanent assignments to eligible assignments. Keep only 2 break-glass accounts as permanent.",
                    affectedResources: permanentHighPrivilege.Select(r => r.DisplayName ?? "Unknown").ToList()
                ));
            }
            else
            {
                findings.Findings.Add(CreateFinding(
                    "PAM-003",
                    "Standing Privileges Minimized",
                    "Standing privileged access is appropriately limited",
                    $"Only {permanentHighPrivilege.Count} permanent high-privilege assignments found.",
                    Severity.Informational,
                    true,
                    "Standing Access"
                ));
            }

            // Check 4: Role-Assignable Groups
            if (privilegedGroups.Count > 0)
            {
                var largePrivilegedGroups = privilegedGroups.Where(g => (g.Members?.Count ?? 0) > 10).ToList();
                if (largePrivilegedGroups.Any())
                {
                    findings.Findings.Add(CreateFinding(
                        "PAM-004",
                        "Large Role-Assignable Groups",
                        "Role-assignable groups with many members detected",
                        $"Found {largePrivilegedGroups.Count} role-assignable groups with more than 10 members.",
                        Severity.Medium,
                        false,
                        "Group-Based Access",
                        remediation: "Review membership of large role-assignable groups and remove unnecessary members.",
                        affectedResources: largePrivilegedGroups.Select(g => g.DisplayName ?? "Unknown").ToList()
                    ));
                }
                else
                {
                    findings.Findings.Add(CreateFinding(
                        "PAM-004",
                        "Role-Assignable Groups",
                        "Role-assignable groups are appropriately sized",
                        $"Found {privilegedGroups.Count} role-assignable groups with reasonable membership.",
                        Severity.Informational,
                        true,
                        "Group-Based Access"
                    ));
                }
            }

            // Check 5: Break-Glass Account
            var globalAdminRole = directoryRoles.FirstOrDefault(r =>
                r.RoleTemplateId == "62e90394-69f5-4237-9190-012177145e10");
            var breakGlassAccounts = globalAdminRole?.Members?
                .Where(m => m.DisplayName?.Contains("break", StringComparison.OrdinalIgnoreCase) == true ||
                           m.DisplayName?.Contains("emergency", StringComparison.OrdinalIgnoreCase) == true ||
                           m.DisplayName?.Contains("glass", StringComparison.OrdinalIgnoreCase) == true)
                .ToList() ?? new List<RoleMemberInfo>();

            if (breakGlassAccounts.Count == 0 && globalAdminCount < 2)
            {
                findings.Findings.Add(CreateFinding(
                    "PAM-005",
                    "No Break-Glass Account",
                    "No emergency access (break-glass) accounts detected",
                    "Break-glass accounts provide emergency access when normal authentication fails.",
                    Severity.High,
                    false,
                    "Emergency Access",
                    remediation: "Create 2 cloud-only break-glass accounts excluded from Conditional Access policies.",
                    references: "https://learn.microsoft.com/en-us/azure/active-directory/roles/security-emergency-access"
                ));
            }
            else
            {
                findings.Findings.Add(CreateFinding(
                    "PAM-005",
                    "Emergency Access",
                    "Emergency access accounts appear to be configured",
                    breakGlassAccounts.Count > 0
                        ? $"Found {breakGlassAccounts.Count} potential break-glass accounts."
                        : "Multiple Global Administrators configured for redundancy.",
                    Severity.Informational,
                    true,
                    "Emergency Access"
                ));
            }

            // Check 6: Service Principals with High Privileges
            var servicePrincipalsInRoles = directoryRoles
                .SelectMany(r => r.Members ?? new List<RoleMemberInfo>())
                .Where(m => m.OdataType == "#microsoft.graph.servicePrincipal")
                .ToList();

            if (servicePrincipalsInRoles.Any())
            {
                findings.Findings.Add(CreateFinding(
                    "PAM-006",
                    "Service Principals in Admin Roles",
                    $"{servicePrincipalsInRoles.Count} service principals have administrative roles",
                    "Service principals with admin roles should be carefully reviewed and monitored.",
                    Severity.Medium,
                    false,
                    "Application Access",
                    remediation: "Review service principal role assignments and ensure least privilege access.",
                    affectedResources: servicePrincipalsInRoles.Select(sp => sp.DisplayName ?? "Unknown SP").Take(20).ToList()
                ));
            }
            else
            {
                findings.Findings.Add(CreateFinding(
                    "PAM-006",
                    "Service Principal Roles",
                    "No service principals in administrative roles",
                    "No service principals were found assigned to directory roles.",
                    Severity.Informational,
                    true,
                    "Application Access"
                ));
            }

            // Check 7: Custom Roles
            var customRoles = roleDefinitions.Where(r => r.IsBuiltIn == false).ToList();
            if (customRoles.Count > 10)
            {
                findings.Findings.Add(CreateFinding(
                    "PAM-007",
                    "Many Custom Roles",
                    $"{customRoles.Count} custom roles defined",
                    "Large numbers of custom roles can be difficult to manage and audit.",
                    Severity.Low,
                    false,
                    "Role Management",
                    remediation: "Review custom roles and consolidate where possible."
                ));
            }
            else
            {
                findings.Findings.Add(CreateFinding(
                    "PAM-007",
                    "Custom Roles",
                    customRoles.Count > 0 ? $"{customRoles.Count} custom roles defined" : "No custom roles defined",
                    "Custom role count is manageable.",
                    Severity.Informational,
                    true,
                    "Role Management"
                ));
            }

            // Generate summary
            findings.Summary.Add($"Global Administrators: {globalAdminCount}");
            findings.Summary.Add($"Permanent Admin Assignments: {permanentAdmins.Count}");
            findings.Summary.Add($"Eligible (JIT) Assignments: {eligibleAssignments.Count}");
            findings.Summary.Add($"Role-Assignable Groups: {privilegedGroups.Count}");
            if (rawData.Warnings.Any())
            {
                findings.Summary.Add($"Note: {rawData.Warnings.Count} data points could not be collected");
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error normalizing Privileged Access findings");
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

    private List<DirectoryRoleInfo> GetPermanentAssignments(List<DirectoryRoleInfo> roles, HashSet<string> highPrivilegeRoleIds)
    {
        return roles
            .Where(r => highPrivilegeRoleIds.Contains(r.RoleTemplateId ?? "") && r.Members?.Any() == true)
            .ToList();
    }

    private class RoleAssignmentInfo
    {
        public string? Id { get; set; }
        public string? RoleDefinitionId { get; set; }
        public string? PrincipalId { get; set; }
        public string? DirectoryScopeId { get; set; }
        public string? AssignmentType { get; set; }
        public DateTime? StartDateTime { get; set; }
        public DateTime? EndDateTime { get; set; }
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

    private class RoleDefinitionInfo
    {
        public string? Id { get; set; }
        public string? DisplayName { get; set; }
        public string? Description { get; set; }
        public bool? IsBuiltIn { get; set; }
        public bool? IsEnabled { get; set; }
    }

    private class GroupInfo
    {
        public string? Id { get; set; }
        public string? DisplayName { get; set; }
        public bool? IsAssignableToRole { get; set; }
        public List<RoleMemberInfo>? Members { get; set; }
    }

    #endregion
}
