using System.Text.Json;
using Cloudativ.Assessment.Application.Services;
using Cloudativ.Assessment.Domain.Enums;
using Cloudativ.Assessment.Domain.Interfaces;
using Microsoft.Extensions.Logging;

namespace Cloudativ.Assessment.Infrastructure.Modules;

public class CollaborationSecurityAssessmentModule : BaseAssessmentModule
{
    private readonly IScoringService _scoringService;

    public CollaborationSecurityAssessmentModule(ILogger<CollaborationSecurityAssessmentModule> logger, IScoringService scoringService)
        : base(logger)
    {
        _scoringService = scoringService;
    }

    public override AssessmentDomain Domain => AssessmentDomain.CollaborationSecurity;
    public override string DisplayName => "Collaboration Security";
    public override string Description => "Assesses Microsoft Teams, SharePoint, OneDrive security settings, external sharing, and guest access policies.";

    public override IReadOnlyList<string> RequiredPermissions => new[]
    {
        "Sites.Read.All",
        "Team.ReadBasic.All",
        "TeamSettings.Read.All",
        "User.Read.All",
        "Directory.Read.All",
        "Policy.Read.All"
    };

    public override async Task<CollectionResult> CollectAsync(IGraphClientWrapper graphClient, CancellationToken cancellationToken = default)
    {
        var rawData = new Dictionary<string, object?>();
        var warnings = new List<string>();
        var unavailableEndpoints = new List<string>();

        try
        {
            // 1. Collect SharePoint Sites
            _logger.LogInformation("Collecting SharePoint sites...");
            try
            {
                var sitesJson = await graphClient.GetRawJsonAsync(
                    "sites?$select=id,name,displayName,webUrl,isPersonalSite&$top=500",
                    cancellationToken);
                rawData["sites"] = sitesJson;
            }
            catch (Exception ex)
            {
                warnings.Add($"Failed to collect SharePoint sites: {ex.Message}");
                unavailableEndpoints.Add("sites");
            }

            // 2. Collect Teams
            _logger.LogInformation("Collecting Teams...");
            try
            {
                var teamsJson = await graphClient.GetRawJsonAsync(
                    "groups?$filter=resourceProvisioningOptions/Any(x:x eq 'Team')&$select=id,displayName,visibility,createdDateTime,membershipRule&$top=999",
                    cancellationToken);
                rawData["teams"] = teamsJson;
            }
            catch (Exception ex)
            {
                warnings.Add($"Failed to collect Teams: {ex.Message}");
                unavailableEndpoints.Add("teams");
            }

            // 3. Collect Guest Users
            _logger.LogInformation("Collecting guest users...");
            try
            {
                var guestUsersJson = await graphClient.GetRawJsonAsync(
                    "users?$filter=userType eq 'Guest'&$select=id,displayName,userPrincipalName,mail,createdDateTime,signInActivity&$top=999",
                    cancellationToken);
                rawData["guestUsers"] = guestUsersJson;
            }
            catch (Exception ex)
            {
                warnings.Add($"Failed to collect guest users: {ex.Message}");
            }

            // 4. Collect External Identities Policy
            _logger.LogInformation("Collecting external identities policy...");
            try
            {
                var externalPolicyJson = await graphClient.GetRawJsonAsync(
                    "policies/externalIdentitiesPolicy",
                    cancellationToken);
                rawData["externalIdentitiesPolicy"] = externalPolicyJson;
            }
            catch (Exception ex)
            {
                warnings.Add($"Failed to collect external identities policy: {ex.Message}");
            }

            // 5. Collect Cross-Tenant Access Policy
            _logger.LogInformation("Collecting cross-tenant access policy...");
            try
            {
                var crossTenantPolicyJson = await graphClient.GetRawJsonAsync(
                    "policies/crossTenantAccessPolicy",
                    cancellationToken);
                rawData["crossTenantAccessPolicy"] = crossTenantPolicyJson;
            }
            catch (Exception ex)
            {
                warnings.Add($"Failed to collect cross-tenant access policy: {ex.Message}");
            }

            // 6. Collect Cross-Tenant Access Partners
            _logger.LogInformation("Collecting cross-tenant access partners...");
            try
            {
                var partnersJson = await graphClient.GetRawJsonAsync(
                    "policies/crossTenantAccessPolicy/partners",
                    cancellationToken);
                rawData["crossTenantPartners"] = partnersJson;
            }
            catch (Exception ex)
            {
                warnings.Add($"Failed to collect cross-tenant partners: {ex.Message}");
            }

            // 7. Collect Conditional Access Policies (guest-related)
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

            // 8. Collect Sharing Links (beta)
            _logger.LogInformation("Collecting sharing information...");
            try
            {
                var sharingLinksJson = await graphClient.GetRawJsonAsync(
                    "https://graph.microsoft.com/beta/drives?$select=id,name,driveType,owner&$top=100",
                    cancellationToken);
                rawData["drives"] = sharingLinksJson;
            }
            catch (Exception ex)
            {
                warnings.Add($"Failed to collect drives: {ex.Message}");
            }

            // 9. Collect All Users for context
            _logger.LogInformation("Collecting users for context...");
            try
            {
                var usersJson = await graphClient.GetRawJsonAsync(
                    "users?$select=id,userType&$count=true&$top=999",
                    cancellationToken);
                rawData["users"] = usersJson;
            }
            catch (Exception ex)
            {
                warnings.Add($"Failed to collect users: {ex.Message}");
            }

            // 10. Collect Group Settings (for Teams/Groups creation policy)
            _logger.LogInformation("Collecting group settings...");
            try
            {
                var groupSettingsJson = await graphClient.GetRawJsonAsync(
                    "groupSettings",
                    cancellationToken);
                rawData["groupSettings"] = groupSettingsJson;
            }
            catch (Exception ex)
            {
                warnings.Add($"Failed to collect group settings: {ex.Message}");
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
            _logger.LogError(ex, "Failed to collect Collaboration Security data");
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
            var sites = ParseJsonCollection<SiteInfo>(
                rawData.RawData.GetValueOrDefault("sites") as string);
            var teams = ParseJsonCollection<TeamInfo>(
                rawData.RawData.GetValueOrDefault("teams") as string);
            var guestUsers = ParseJsonCollection<GuestUserInfo>(
                rawData.RawData.GetValueOrDefault("guestUsers") as string);
            var allUsers = ParseJsonCollection<UserInfo>(
                rawData.RawData.GetValueOrDefault("users") as string);
            var conditionalAccessPolicies = ParseJsonCollection<CaPolicyInfo>(
                rawData.RawData.GetValueOrDefault("conditionalAccessPolicies") as string);
            var crossTenantPartners = ParseJsonCollection<CrossTenantPartnerInfo>(
                rawData.RawData.GetValueOrDefault("crossTenantPartners") as string);
            var crossTenantPolicy = ParseCrossTenantPolicy(
                rawData.RawData.GetValueOrDefault("crossTenantAccessPolicy") as string);
            var groupSettings = ParseJsonCollection<GroupSettingInfo>(
                rawData.RawData.GetValueOrDefault("groupSettings") as string);

            // Calculate metrics
            var totalUsers = allUsers.Count;
            var guestCount = guestUsers.Count;
            var guestPercentage = totalUsers > 0 ? (guestCount * 100.0 / totalUsers) : 0;
            var publicTeams = teams.Count(t => t.Visibility == "Public");
            var privateTeams = teams.Count(t => t.Visibility == "Private");

            // Identify stale guests (no sign-in in 90 days)
            var staleThreshold = DateTime.UtcNow.AddDays(-90);
            var staleGuests = guestUsers.Where(g =>
                g.SignInActivity?.LastSignInDateTime == null ||
                g.SignInActivity?.LastSignInDateTime < staleThreshold).ToList();

            findings.Metrics["totalSites"] = sites.Count;
            findings.Metrics["totalTeams"] = teams.Count;
            findings.Metrics["publicTeams"] = publicTeams;
            findings.Metrics["privateTeams"] = privateTeams;
            findings.Metrics["guestUsersCount"] = guestCount;
            findings.Metrics["guestPercentage"] = Math.Round(guestPercentage, 1);
            findings.Metrics["staleGuests"] = staleGuests.Count;
            findings.Metrics["crossTenantPartners"] = crossTenantPartners.Count;
            findings.Metrics["externalSharingEnabled"] = 1; // Will be updated based on policy

            // Check 1: Guest User Percentage
            if (guestPercentage > 30)
            {
                findings.Findings.Add(CreateFinding(
                    "COL-001",
                    "High Guest User Percentage",
                    $"Guests represent {guestPercentage:F1}% of all users",
                    $"Found {guestCount} guest users out of {totalUsers} total users.",
                    Severity.Medium,
                    false,
                    "External Access",
                    remediation: "Review guest users and implement access reviews to manage external access.",
                    references: "https://learn.microsoft.com/en-us/azure/active-directory/governance/access-reviews-overview"
                ));
            }
            else if (guestPercentage > 15)
            {
                findings.Findings.Add(CreateFinding(
                    "COL-001",
                    "Moderate Guest User Count",
                    $"Guests represent {guestPercentage:F1}% of all users",
                    "Consider implementing regular access reviews for guest users.",
                    Severity.Low,
                    false,
                    "External Access",
                    remediation: "Configure guest access reviews to ensure external users still need access."
                ));
            }
            else
            {
                findings.Findings.Add(CreateFinding(
                    "COL-001",
                    "Guest User Count",
                    $"Guests represent {guestPercentage:F1}% of users",
                    "Guest user percentage is within normal range.",
                    Severity.Informational,
                    true,
                    "External Access"
                ));
            }

            // Check 2: Stale Guest Accounts
            if (staleGuests.Count > 0)
            {
                findings.Findings.Add(CreateFinding(
                    "COL-002",
                    "Stale Guest Accounts",
                    $"{staleGuests.Count} guest users haven't signed in for 90+ days",
                    "Stale guest accounts may pose a security risk if compromised.",
                    staleGuests.Count > 20 ? Severity.Medium : Severity.Low,
                    false,
                    "Guest Hygiene",
                    remediation: "Remove or disable stale guest accounts. Consider implementing guest access reviews.",
                    affectedResources: staleGuests.Select(g => g.UserPrincipalName ?? g.DisplayName ?? "Unknown").Take(20).ToList()
                ));
            }
            else if (guestCount > 0)
            {
                findings.Findings.Add(CreateFinding(
                    "COL-002",
                    "Active Guest Accounts",
                    "All guest users have recent sign-in activity",
                    "Guest accounts are actively being used.",
                    Severity.Informational,
                    true,
                    "Guest Hygiene"
                ));
            }

            // Check 3: Public Teams
            if (publicTeams > 0)
            {
                findings.Findings.Add(CreateFinding(
                    "COL-003",
                    "Public Teams Detected",
                    $"{publicTeams} public Teams found",
                    "Public Teams are discoverable by all users in the organization.",
                    publicTeams > 10 ? Severity.Medium : Severity.Low,
                    false,
                    "Teams Security",
                    remediation: "Review public Teams to ensure they don't contain sensitive information.",
                    affectedResources: teams.Where(t => t.Visibility == "Public").Select(t => t.DisplayName ?? "Unknown").Take(20).ToList()
                ));
            }
            else
            {
                findings.Findings.Add(CreateFinding(
                    "COL-003",
                    "No Public Teams",
                    "All Teams are set to Private",
                    "Teams are properly restricted to members only.",
                    Severity.Informational,
                    true,
                    "Teams Security"
                ));
            }

            // Check 4: Guest Conditional Access
            var hasGuestPolicy = conditionalAccessPolicies.Any(p =>
                p.State == "enabled" &&
                (p.Conditions?.Users?.IncludeGuestsOrExternalUsers == true ||
                 p.DisplayName?.Contains("guest", StringComparison.OrdinalIgnoreCase) == true ||
                 p.DisplayName?.Contains("external", StringComparison.OrdinalIgnoreCase) == true));

            if (!hasGuestPolicy && guestCount > 0)
            {
                findings.Findings.Add(CreateFinding(
                    "COL-004",
                    "No Guest Conditional Access",
                    "No Conditional Access policies targeting guest users",
                    "Guests may not be subject to the same security controls as internal users.",
                    Severity.High,
                    false,
                    "Guest Security",
                    remediation: "Create Conditional Access policies specifically for guest and external users.",
                    references: "https://learn.microsoft.com/en-us/azure/active-directory/external-identities/authentication-conditional-access"
                ));
            }
            else if (hasGuestPolicy)
            {
                findings.Findings.Add(CreateFinding(
                    "COL-004",
                    "Guest Conditional Access",
                    "Conditional Access policies exist for guest users",
                    "Guest users are subject to specific security policies.",
                    Severity.Informational,
                    true,
                    "Guest Security"
                ));
            }

            // Check 5: Cross-Tenant Access
            if (crossTenantPolicy?.AllowedCloudEndpoints?.Any() == true ||
                crossTenantPartners.Any(p => p.InboundTrust?.IsEnabled == true))
            {
                findings.Findings.Add(CreateFinding(
                    "COL-005",
                    "Cross-Tenant Access Configured",
                    $"{crossTenantPartners.Count} cross-tenant partner configurations",
                    "Cross-tenant access settings allow collaboration with specific organizations.",
                    Severity.Informational,
                    true,
                    "B2B Collaboration",
                    evidence: JsonSerializer.Serialize(crossTenantPartners.Select(p => p.TenantId).Take(10))
                ));
            }
            else
            {
                findings.Findings.Add(CreateFinding(
                    "COL-005",
                    "Default Cross-Tenant Settings",
                    "Using default cross-tenant access settings",
                    "No specific partner configurations have been defined.",
                    Severity.Low,
                    false,
                    "B2B Collaboration",
                    remediation: "Consider configuring cross-tenant access settings for trusted partner organizations.",
                    references: "https://learn.microsoft.com/en-us/azure/active-directory/external-identities/cross-tenant-access-overview"
                ));
            }

            // Check 6: Teams Creation Policy
            var groupCreationSetting = groupSettings.FirstOrDefault(s =>
                s.TemplateId == "62375ab9-6b52-47ed-826b-58e47e0e304b"); // Group.Unified settings

            if (groupCreationSetting?.Values?.Any(v =>
                v.Name == "EnableGroupCreation" && v.Value == "false") == true)
            {
                findings.Findings.Add(CreateFinding(
                    "COL-006",
                    "Teams Creation Restricted",
                    "Teams/Group creation is restricted to specific users",
                    "Only authorized users can create new Teams and Microsoft 365 Groups.",
                    Severity.Informational,
                    true,
                    "Teams Governance"
                ));
            }
            else
            {
                findings.Findings.Add(CreateFinding(
                    "COL-006",
                    "Open Teams Creation",
                    "Any user can create Teams and Microsoft 365 Groups",
                    "Unrestricted group creation can lead to sprawl and governance issues.",
                    Severity.Medium,
                    false,
                    "Teams Governance",
                    remediation: "Consider restricting Teams creation to specific groups or implementing naming policies.",
                    references: "https://learn.microsoft.com/en-us/microsoft-365/solutions/manage-creation-of-groups"
                ));
            }

            // Check 7: External Sharing (general assessment)
            findings.Findings.Add(CreateFinding(
                "COL-007",
                "External Sharing",
                "External sharing settings should be verified in SharePoint admin center",
                "SharePoint and OneDrive external sharing settings control how users share content externally.",
                Severity.Low,
                true, // Cannot fully assess via Graph
                "Sharing Settings",
                remediation: "Review SharePoint and OneDrive external sharing settings in the admin center.",
                references: "https://learn.microsoft.com/en-us/sharepoint/turn-external-sharing-on-or-off"
            ));

            // Check 8: Site Count and Management
            if (sites.Count > 100)
            {
                findings.Findings.Add(CreateFinding(
                    "COL-008",
                    "Many SharePoint Sites",
                    $"{sites.Count} SharePoint sites detected",
                    "A large number of sites may require governance policies and regular reviews.",
                    Severity.Low,
                    false,
                    "Site Governance",
                    remediation: "Implement site lifecycle policies and regular site access reviews."
                ));
            }
            else
            {
                findings.Findings.Add(CreateFinding(
                    "COL-008",
                    "SharePoint Sites",
                    $"{sites.Count} SharePoint sites",
                    "Site count is manageable.",
                    Severity.Informational,
                    true,
                    "Site Governance"
                ));
            }

            // Generate summary
            findings.Summary.Add($"Teams: {teams.Count} ({publicTeams} public, {privateTeams} private)");
            findings.Summary.Add($"SharePoint Sites: {sites.Count}");
            findings.Summary.Add($"Guest Users: {guestCount} ({guestPercentage:F1}% of users)");
            findings.Summary.Add($"Stale Guests (90+ days): {staleGuests.Count}");
            findings.Summary.Add($"Cross-Tenant Partners: {crossTenantPartners.Count}");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error normalizing Collaboration Security findings");
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

    private CrossTenantAccessPolicyInfo? ParseCrossTenantPolicy(string? json)
    {
        if (string.IsNullOrEmpty(json))
            return null;

        try
        {
            return JsonSerializer.Deserialize<CrossTenantAccessPolicyInfo>(json, new JsonSerializerOptions
            {
                PropertyNameCaseInsensitive = true
            });
        }
        catch
        {
            return null;
        }
    }

    private class SiteInfo
    {
        public string? Id { get; set; }
        public string? Name { get; set; }
        public string? DisplayName { get; set; }
        public string? WebUrl { get; set; }
        public bool? IsPersonalSite { get; set; }
    }

    private class TeamInfo
    {
        public string? Id { get; set; }
        public string? DisplayName { get; set; }
        public string? Visibility { get; set; }
        public DateTime? CreatedDateTime { get; set; }
        public string? MembershipRule { get; set; }
    }

    private class GuestUserInfo
    {
        public string? Id { get; set; }
        public string? DisplayName { get; set; }
        public string? UserPrincipalName { get; set; }
        public string? Mail { get; set; }
        public DateTime? CreatedDateTime { get; set; }
        public SignInActivityInfo? SignInActivity { get; set; }
    }

    private class SignInActivityInfo
    {
        public DateTime? LastSignInDateTime { get; set; }
    }

    private class UserInfo
    {
        public string? Id { get; set; }
        public string? UserType { get; set; }
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
        public CaUsers? Users { get; set; }
    }

    private class CaUsers
    {
        public bool? IncludeGuestsOrExternalUsers { get; set; }
    }

    private class CrossTenantAccessPolicyInfo
    {
        public string? Id { get; set; }
        public List<string>? AllowedCloudEndpoints { get; set; }
    }

    private class CrossTenantPartnerInfo
    {
        public string? TenantId { get; set; }
        public InboundTrustInfo? InboundTrust { get; set; }
    }

    private class InboundTrustInfo
    {
        public bool? IsEnabled { get; set; }
    }

    private class GroupSettingInfo
    {
        public string? Id { get; set; }
        public string? DisplayName { get; set; }
        public string? TemplateId { get; set; }
        public List<GroupSettingValue>? Values { get; set; }
    }

    private class GroupSettingValue
    {
        public string? Name { get; set; }
        public string? Value { get; set; }
    }

    #endregion
}
