using System.Text.Json;
using Cloudativ.Assessment.Domain.Entities.Inventory;
using Cloudativ.Assessment.Domain.Enums;
using Cloudativ.Assessment.Domain.Interfaces;
using Cloudativ.Assessment.Infrastructure.Data;
using Cloudativ.Assessment.Infrastructure.Services;
using Microsoft.Extensions.Logging;

namespace Cloudativ.Assessment.Infrastructure.Inventory.Modules;

/// <summary>
/// Inventory module for Identity & Access (Entra ID) data collection.
/// Collects users, groups, roles, service principals, conditional access policies, etc.
/// </summary>
public class IdentityAccessInventoryModule : BaseInventoryModule
{
    public override InventoryDomain Domain => InventoryDomain.IdentityAccess;
    public override string DisplayName => "Identity & Access";
    public override string Description => "Collects users, groups, roles, service principals, CA policies, and authentication settings.";

    public override IReadOnlyList<string> RequiredPermissions => new[]
    {
        "User.Read.All",
        "Group.Read.All",
        "Directory.Read.All",
        "RoleManagement.Read.Directory",
        "Policy.Read.All",
        "Application.Read.All",
        "IdentityRiskyUser.Read.All"
    };

    public IdentityAccessInventoryModule(
        ApplicationDbContext dbContext,
        ILogger<IdentityAccessInventoryModule> logger)
        : base(dbContext, logger)
    {
    }

    public override async Task<InventoryCollectionResult> CollectAsync(
        IGraphClientWrapper graphClient,
        Guid tenantId,
        Guid snapshotId,
        CancellationToken ct = default)
    {
        var startTime = DateTime.UtcNow;
        var warnings = new List<string>();
        var breakdown = new Dictionary<string, int>();
        var totalItems = 0;

        try
        {
            // 1. Collect Users
            var userCount = await CollectUsersAsync(graphClient, tenantId, snapshotId, warnings, ct);
            breakdown["Users"] = userCount;
            totalItems += userCount;
            Logger.LogInformation("Collected {Count} users", userCount);

            // 2. Collect Groups
            var groupCount = await CollectGroupsAsync(graphClient, tenantId, snapshotId, warnings, ct);
            breakdown["Groups"] = groupCount;
            totalItems += groupCount;
            Logger.LogInformation("Collected {Count} groups", groupCount);

            // 3. Collect Directory Roles
            var roleCount = await CollectDirectoryRolesAsync(graphClient, tenantId, snapshotId, warnings, ct);
            breakdown["DirectoryRoles"] = roleCount;
            totalItems += roleCount;
            Logger.LogInformation("Collected {Count} directory roles", roleCount);

            // 4. Collect Service Principals
            var spCount = await CollectServicePrincipalsAsync(graphClient, tenantId, snapshotId, warnings, ct);
            breakdown["ServicePrincipals"] = spCount;
            totalItems += spCount;
            Logger.LogInformation("Collected {Count} service principals", spCount);

            // 5. Collect Conditional Access Policies
            var caCount = await CollectConditionalAccessPoliciesAsync(graphClient, tenantId, snapshotId, warnings, ct);
            breakdown["ConditionalAccessPolicies"] = caCount;
            totalItems += caCount;
            Logger.LogInformation("Collected {Count} CA policies", caCount);

            // 6. Collect Named Locations
            var locCount = await CollectNamedLocationsAsync(graphClient, tenantId, snapshotId, warnings, ct);
            breakdown["NamedLocations"] = locCount;
            totalItems += locCount;

            await DbContext.SaveChangesAsync(ct);

            return Success(totalItems, DateTime.UtcNow - startTime, breakdown, warnings);
        }
        catch (Exception ex)
        {
            Logger.LogError(ex, "Error collecting identity inventory for tenant {TenantId}", tenantId);
            return Failure(ex.Message, DateTime.UtcNow - startTime, warnings);
        }
    }

    private async Task<int> CollectUsersAsync(
        IGraphClientWrapper graphClient,
        Guid tenantId,
        Guid snapshotId,
        List<string> warnings,
        CancellationToken ct)
    {
        // Note: signInActivity requires Azure AD Premium P1/P2 license
        // We collect basic user data first, then try to enrich with signInActivity separately
        const string endpoint = "users?$select=id,userPrincipalName,displayName,mail,userType,accountEnabled," +
                               "createdDateTime,assignedLicenses,department,jobTitle," +
                               "usageLocation,country,officeLocation,companyName," +
                               "onPremisesSyncEnabled,onPremisesLastSyncDateTime,onPremisesDomainName," +
                               "onPremisesSamAccountName&$top=999";

        var users = new List<UserInventory>();
        var currentEndpoint = endpoint;

        while (!string.IsNullOrEmpty(currentEndpoint) && !ct.IsCancellationRequested)
        {
            try
            {
                var response = await graphClient.GetRawJsonAsync(currentEndpoint, ct);
                if (string.IsNullOrEmpty(response)) break;

                var doc = JsonDocument.Parse(response);

                if (doc.RootElement.TryGetProperty("value", out var valueElement))
                {
                    foreach (var userElement in valueElement.EnumerateArray())
                    {
                        var user = ParseUser(userElement, tenantId, snapshotId);
                        users.Add(user);
                    }
                }

                currentEndpoint = doc.RootElement.TryGetProperty("@odata.nextLink", out var nextLink)
                    ? nextLink.GetString()
                    : null;
            }
            catch (Exception ex)
            {
                warnings.Add($"Error fetching users page: {ex.Message}");
                break;
            }
        }

        // Enrich users with sign-in activity (requires Premium license, graceful fallback)
        await EnrichUsersWithSignInActivityAsync(graphClient, users, warnings, ct);

        // Enrich users with role assignments (for privileged user detection)
        await EnrichUsersWithRolesAsync(graphClient, users, warnings, ct);

        // Enrich users with risk status
        await EnrichUsersWithRiskAsync(graphClient, users, warnings, ct);

        // Add to database
        await DbContext.UserInventories.AddRangeAsync(users, ct);

        return users.Count;
    }

    private async Task EnrichUsersWithSignInActivityAsync(
        IGraphClientWrapper graphClient,
        List<UserInventory> users,
        List<string> warnings,
        CancellationToken ct)
    {
        // signInActivity requires Azure AD Premium P1/P2 license
        // This is a best-effort enrichment - if it fails, we continue without it
        try
        {
            const string endpoint = "users?$select=id,signInActivity&$top=999";
            var currentEndpoint = endpoint;
            var signInData = new Dictionary<string, (DateTime? lastSignIn, DateTime? lastNonInteractive)>();

            while (!string.IsNullOrEmpty(currentEndpoint) && !ct.IsCancellationRequested)
            {
                var response = await graphClient.GetRawJsonAsync(currentEndpoint, ct);
                if (string.IsNullOrEmpty(response)) break;

                var doc = JsonDocument.Parse(response);

                if (doc.RootElement.TryGetProperty("value", out var valueElement))
                {
                    foreach (var userElement in valueElement.EnumerateArray())
                    {
                        var userId = GetString(userElement, "id");
                        if (!string.IsNullOrEmpty(userId) && userElement.TryGetProperty("signInActivity", out var signInActivity))
                        {
                            signInData[userId] = (
                                GetDateTime(signInActivity, "lastSignInDateTime"),
                                GetDateTime(signInActivity, "lastNonInteractiveSignInDateTime")
                            );
                        }
                    }
                }

                currentEndpoint = doc.RootElement.TryGetProperty("@odata.nextLink", out var nextLink)
                    ? nextLink.GetString()
                    : null;
            }

            // Apply sign-in data to users
            foreach (var user in users)
            {
                if (signInData.TryGetValue(user.ObjectId, out var signIn))
                {
                    user.LastSignInDateTime = signIn.lastSignIn;
                    user.LastNonInteractiveSignInDateTime = signIn.lastNonInteractive;
                }
            }

            Logger.LogInformation("Enriched {Count} users with sign-in activity", signInData.Count);
        }
        catch (Exception ex)
        {
            // This is expected to fail on non-premium tenants
            var message = ex.Message;
            if (message.Contains("premium", StringComparison.OrdinalIgnoreCase) ||
                message.Contains("Forbidden", StringComparison.OrdinalIgnoreCase) ||
                message.Contains("license", StringComparison.OrdinalIgnoreCase))
            {
                Logger.LogWarning("Sign-in activity not available (requires Azure AD Premium): {Message}", message);
                warnings.Add("Sign-in activity data not available - requires Azure AD Premium P1/P2 license");
            }
            else
            {
                Logger.LogWarning(ex, "Error enriching users with sign-in activity");
                warnings.Add($"Error enriching users with sign-in activity: {message}");
            }
        }
    }

    private UserInventory ParseUser(JsonElement element, Guid tenantId, Guid snapshotId)
    {
        var user = new UserInventory
        {
            TenantId = tenantId,
            SnapshotId = snapshotId,
            ObjectId = GetString(element, "id") ?? string.Empty,
            UserPrincipalName = GetString(element, "userPrincipalName") ?? string.Empty,
            DisplayName = GetString(element, "displayName"),
            Mail = GetString(element, "mail"),
            UserType = GetString(element, "userType") ?? "Member",
            AccountEnabled = GetBool(element, "accountEnabled"),
            CreatedDateTime = GetDateTime(element, "createdDateTime"),
            Department = GetString(element, "department"),
            JobTitle = GetString(element, "jobTitle"),
            UsageLocation = GetString(element, "usageLocation"),
            Country = GetString(element, "country"),
            OfficeLocation = GetString(element, "officeLocation"),
            CompanyName = GetString(element, "companyName"),
            OnPremisesSyncEnabled = GetBool(element, "onPremisesSyncEnabled"),
            OnPremisesLastSyncDateTime = GetDateTime(element, "onPremisesLastSyncDateTime"),
            OnPremisesDomainName = GetString(element, "onPremisesDomainName"),
            OnPremisesSamAccountName = GetString(element, "onPremisesSamAccountName")
        };

        // Parse sign-in activity
        if (element.TryGetProperty("signInActivity", out var signInActivity))
        {
            user.LastSignInDateTime = GetDateTime(signInActivity, "lastSignInDateTime");
            user.LastNonInteractiveSignInDateTime = GetDateTime(signInActivity, "lastNonInteractiveSignInDateTime");
        }

        // Parse assigned licenses with full categorization
        if (element.TryGetProperty("assignedLicenses", out var licenses) && licenses.ValueKind == JsonValueKind.Array)
        {
            var licenseSkuIds = new List<string>();
            var allCategories = new List<LicenseCategory>();

            foreach (var license in licenses.EnumerateArray())
            {
                var skuId = GetString(license, "skuId");
                if (!string.IsNullOrEmpty(skuId))
                {
                    licenseSkuIds.Add(skuId);

                    // Get category by SKU ID
                    var category = LicenseSkuMapper.GetCategoryBySkuId(skuId);
                    if (category == LicenseCategory.Unknown)
                    {
                        // Fall back to pattern matching on common SKU IDs
                        category = GetCategoryFromSkuId(skuId);
                    }

                    if (category != LicenseCategory.Unknown)
                    {
                        allCategories.Add(category);
                    }

                    // Legacy flags (keep for backwards compatibility)
                    if (IsE5LicenseSku(skuId))
                    {
                        user.HasE5License = true;
                    }
                    if (IsE3LicenseSku(skuId))
                    {
                        user.HasE3License = true;
                    }

                    // New license type flags
                    if (category == LicenseCategory.BusinessPremium)
                    {
                        user.HasBusinessPremium = true;
                    }
                    if (category == LicenseCategory.BusinessStandard)
                    {
                        user.HasBusinessStandard = true;
                    }
                    if (category == LicenseCategory.BusinessBasic)
                    {
                        user.HasBusinessBasic = true;
                    }
                    if (category == LicenseCategory.FrontlineF1 || category == LicenseCategory.FrontlineF3)
                    {
                        user.HasFrontlineLicense = true;
                    }
                    if (category == LicenseCategory.EducationA1 || category == LicenseCategory.EducationA3 || category == LicenseCategory.EducationA5)
                    {
                        user.HasEducationLicense = true;
                    }
                    if (category == LicenseCategory.GovernmentG1 || category == LicenseCategory.GovernmentG3 || category == LicenseCategory.GovernmentG5)
                    {
                        user.HasGovernmentLicense = true;
                    }
                }
            }

            user.AssignedLicensesJson = ToJson(licenseSkuIds);
            user.LicenseCount = licenseSkuIds.Count;

            // Determine primary license (highest tier)
            if (allCategories.Any())
            {
                var uniqueCategories = allCategories.Distinct().ToList();
                user.AllLicenseCategoriesJson = ToJson(uniqueCategories);

                // Primary license is the highest-tier primary license
                var primaryLicense = LicenseSkuMapper.DeterminePrimaryLicense(uniqueCategories);
                user.PrimaryLicenseCategory = primaryLicense;
                user.PrimaryLicenseTierGroup = primaryLicense.GetTierGroup();
            }
        }

        return user;
    }

    private static LicenseCategory GetCategoryFromSkuId(string skuId)
    {
        // Known SKU IDs for major license types
        return skuId.ToLowerInvariant() switch
        {
            // E5
            "06ebc4ee-1bb5-47dd-8120-11324bc54e06" => LicenseCategory.EnterpriseE5,
            "26124093-3d78-432b-b5dc-48bf992543d5" => LicenseCategory.EnterpriseE5,
            "c7df2760-2c81-4ef7-b578-5b5392b571df" => LicenseCategory.EnterpriseE5,
            "184efa21-98c3-4e5d-95ab-d07053a96e67" => LicenseCategory.EnterpriseE5,
            "eb56d846-a2d2-4a71-938a-bfc5e37fe5c9" => LicenseCategory.EnterpriseE5,

            // E3
            "05e9a617-0261-4cee-bb44-138d3ef5d965" => LicenseCategory.EnterpriseE3,
            "6fd2c87f-b296-42f0-b197-1e91e994b900" => LicenseCategory.EnterpriseE3,

            // E1
            "18181a46-0d4e-45cd-891e-60aabd171b4e" => LicenseCategory.EnterpriseE1,

            // Business Premium
            "cbdc14ab-d96c-4c30-b9f4-6ada7cdc1d46" => LicenseCategory.BusinessPremium,

            // Business Standard
            "f245ecc8-75af-4f8e-b61f-27d8114de5f3" => LicenseCategory.BusinessStandard,

            // Business Basic
            "3b555118-da6a-4418-894f-7df1e2096870" => LicenseCategory.BusinessBasic,

            // F3
            "66b55226-6b4f-492c-910c-a3b7a3c9d993" => LicenseCategory.FrontlineF3,

            _ => LicenseCategory.Unknown
        };
    }

    private async Task EnrichUsersWithRolesAsync(
        IGraphClientWrapper graphClient,
        List<UserInventory> users,
        List<string> warnings,
        CancellationToken ct)
    {
        try
        {
            // Get all directory roles with members
            var response = await graphClient.GetRawJsonAsync(
                "directoryRoles?$expand=members($select=id)", ct);

            if (string.IsNullOrEmpty(response)) return;

            var doc = JsonDocument.Parse(response);
            var userRoles = new Dictionary<string, List<string>>();
            var globalAdminRoleTemplateId = "62e90394-69f5-4237-9190-012177145e10";
            var privilegedRoleTemplateIds = new HashSet<string>
            {
                globalAdminRoleTemplateId, // Global Administrator
                "e8611ab8-c189-46e8-94e1-60213ab1f814", // Privileged Role Administrator
                "194ae4cb-b126-40b2-bd5b-6091b380977d", // Security Administrator
                "9b895d92-2cd3-44c7-9d02-a6ac2d5ea5c3", // Application Administrator
                "7be44c8a-adaf-4e2a-84d6-ab2649e08a13", // Privileged Authentication Administrator
            };

            if (doc.RootElement.TryGetProperty("value", out var roles))
            {
                foreach (var role in roles.EnumerateArray())
                {
                    var roleTemplateId = GetString(role, "roleTemplateId");
                    var roleName = GetString(role, "displayName");

                    if (role.TryGetProperty("members", out var members))
                    {
                        foreach (var member in members.EnumerateArray())
                        {
                            var memberId = GetString(member, "id");
                            if (!string.IsNullOrEmpty(memberId))
                            {
                                if (!userRoles.ContainsKey(memberId))
                                    userRoles[memberId] = new List<string>();
                                if (!string.IsNullOrEmpty(roleName))
                                    userRoles[memberId].Add(roleName);
                            }
                        }
                    }
                }
            }

            // Update users with role information
            foreach (var user in users)
            {
                if (userRoles.TryGetValue(user.ObjectId, out var roles2))
                {
                    user.AssignedRolesJson = ToJson(roles2);
                    user.DirectRoleCount = roles2.Count;
                    user.IsPrivileged = true;
                    user.IsGlobalAdmin = roles2.Contains("Global Administrator");
                }
            }
        }
        catch (Exception ex)
        {
            warnings.Add($"Error enriching users with roles: {ex.Message}");
        }
    }

    private async Task EnrichUsersWithRiskAsync(
        IGraphClientWrapper graphClient,
        List<UserInventory> users,
        List<string> warnings,
        CancellationToken ct)
    {
        try
        {
            var response = await graphClient.GetRawJsonAsync(
                "identityProtection/riskyUsers?$select=id,userPrincipalName,riskLevel,riskState,riskDetail,riskLastUpdatedDateTime&$top=999", ct);

            if (string.IsNullOrEmpty(response)) return;

            var doc = JsonDocument.Parse(response);
            var userIdToUser = users.ToDictionary(u => u.ObjectId, u => u);

            if (doc.RootElement.TryGetProperty("value", out var riskyUsers))
            {
                foreach (var riskyUser in riskyUsers.EnumerateArray())
                {
                    var userId = GetString(riskyUser, "id");
                    if (!string.IsNullOrEmpty(userId) && userIdToUser.TryGetValue(userId, out var user))
                    {
                        user.RiskLevel = GetString(riskyUser, "riskLevel");
                        user.RiskState = GetString(riskyUser, "riskState");
                        user.RiskDetail = GetString(riskyUser, "riskDetail");
                        user.RiskLastUpdated = GetDateTime(riskyUser, "riskLastUpdatedDateTime");
                    }
                }
            }
        }
        catch (Exception ex)
        {
            warnings.Add($"Error enriching users with risk: {ex.Message}");
        }
    }

    private async Task<int> CollectGroupsAsync(
        IGraphClientWrapper graphClient,
        Guid tenantId,
        Guid snapshotId,
        List<string> warnings,
        CancellationToken ct)
    {
        // Note: Graph API only allows one $expand per query, so we get groups first
        // then enrich with member/owner counts separately
        const string endpoint = "groups?$select=id,displayName,description,mail,mailNickname,groupTypes," +
                               "securityEnabled,mailEnabled,membershipRule,membershipRuleProcessingState," +
                               "visibility,createdDateTime,renewedDateTime,expirationDateTime," +
                               "onPremisesSyncEnabled,onPremisesSamAccountName,isAssignableToRole," +
                               "classification&$top=999";

        var groups = new List<GroupInventory>();
        var currentEndpoint = endpoint;

        while (!string.IsNullOrEmpty(currentEndpoint) && !ct.IsCancellationRequested)
        {
            try
            {
                var response = await graphClient.GetRawJsonAsync(currentEndpoint, ct);
                if (string.IsNullOrEmpty(response)) break;

                var doc = JsonDocument.Parse(response);

                if (doc.RootElement.TryGetProperty("value", out var valueElement))
                {
                    foreach (var groupElement in valueElement.EnumerateArray())
                    {
                        var group = ParseGroup(groupElement, tenantId, snapshotId);
                        groups.Add(group);
                    }
                }

                currentEndpoint = doc.RootElement.TryGetProperty("@odata.nextLink", out var nextLink)
                    ? nextLink.GetString()
                    : null;
            }
            catch (Exception ex)
            {
                warnings.Add($"Error fetching groups page: {ex.Message}");
                break;
            }
        }

        // Enrich groups with member counts (using $count endpoint for efficiency)
        await EnrichGroupsWithMemberCountsAsync(graphClient, groups, warnings, ct);

        await DbContext.GroupInventories.AddRangeAsync(groups, ct);
        return groups.Count;
    }

    private async Task EnrichGroupsWithMemberCountsAsync(
        IGraphClientWrapper graphClient,
        List<GroupInventory> groups,
        List<string> warnings,
        CancellationToken ct)
    {
        // Get member counts for each group using batch requests or individual calls
        // For efficiency, we'll get counts in batches
        foreach (var group in groups)
        {
            if (ct.IsCancellationRequested) break;

            try
            {
                // Get members with userType to detect external members
                var membersResponse = await graphClient.GetRawJsonAsync(
                    $"groups/{group.ObjectId}/members?$select=id,userType&$top=999", ct);

                if (!string.IsNullOrEmpty(membersResponse))
                {
                    var membersDoc = JsonDocument.Parse(membersResponse);
                    if (membersDoc.RootElement.TryGetProperty("value", out var members))
                    {
                        group.MemberCount = members.GetArrayLength();

                        foreach (var member in members.EnumerateArray())
                        {
                            var userType = GetString(member, "userType");
                            if (userType == "Guest")
                            {
                                group.HasExternalMembers = true;
                                group.ExternalMemberCount++;
                            }
                        }
                    }
                }

                // Get owner count
                var ownersResponse = await graphClient.GetRawJsonAsync(
                    $"groups/{group.ObjectId}/owners?$select=id&$top=999", ct);

                if (!string.IsNullOrEmpty(ownersResponse))
                {
                    var ownersDoc = JsonDocument.Parse(ownersResponse);
                    if (ownersDoc.RootElement.TryGetProperty("value", out var owners))
                    {
                        group.OwnerCount = owners.GetArrayLength();
                    }
                }
            }
            catch (Exception ex)
            {
                // Log but continue - individual group enrichment failure shouldn't stop collection
                Logger.LogDebug("Could not get member/owner counts for group {GroupId}: {Message}",
                    group.ObjectId, ex.Message);
            }
        }
    }

    private GroupInventory ParseGroup(JsonElement element, Guid tenantId, Guid snapshotId)
    {
        var groupTypes = new List<string>();
        if (element.TryGetProperty("groupTypes", out var typesElement) && typesElement.ValueKind == JsonValueKind.Array)
        {
            foreach (var t in typesElement.EnumerateArray())
            {
                if (t.ValueKind == JsonValueKind.String)
                    groupTypes.Add(t.GetString()!);
            }
        }

        var group = new GroupInventory
        {
            TenantId = tenantId,
            SnapshotId = snapshotId,
            ObjectId = GetString(element, "id") ?? string.Empty,
            DisplayName = GetString(element, "displayName") ?? string.Empty,
            Description = GetString(element, "description"),
            Mail = GetString(element, "mail"),
            MailNickname = GetString(element, "mailNickname"),
            IsSecurityGroup = GetBool(element, "securityEnabled"),
            IsMailEnabled = GetBool(element, "mailEnabled"),
            IsDynamicMembership = groupTypes.Contains("DynamicMembership"),
            IsUnifiedGroup = groupTypes.Contains("Unified"),
            IsMicrosoft365Group = groupTypes.Contains("Unified"),
            MembershipRule = GetString(element, "membershipRule"),
            MembershipRuleProcessingState = GetString(element, "membershipRuleProcessingState"),
            Visibility = GetString(element, "visibility"),
            Classification = GetString(element, "classification"),
            IsRoleAssignable = GetBool(element, "isAssignableToRole"),
            CreatedDateTime = GetDateTime(element, "createdDateTime"),
            RenewedDateTime = GetDateTime(element, "renewedDateTime"),
            ExpirationDateTime = GetDateTime(element, "expirationDateTime"),
            OnPremisesSyncEnabled = GetBool(element, "onPremisesSyncEnabled"),
            OnPremisesSamAccountName = GetString(element, "onPremisesSamAccountName")
        };

        // Determine group type string
        if (group.IsMicrosoft365Group)
            group.GroupType = "Microsoft365";
        else if (group.IsSecurityGroup && group.IsMailEnabled)
            group.GroupType = "MailEnabledSecurity";
        else if (group.IsSecurityGroup)
            group.GroupType = "Security";
        else if (group.IsMailEnabled)
            group.GroupType = "Distribution";
        else
            group.GroupType = "Other";

        // Member and owner counts are enriched separately via EnrichGroupsWithMemberCountsAsync
        // because Graph API only allows one $expand per query

        return group;
    }

    private async Task<int> CollectDirectoryRolesAsync(
        IGraphClientWrapper graphClient,
        Guid tenantId,
        Guid snapshotId,
        List<string> warnings,
        CancellationToken ct)
    {
        try
        {
            // Note: @odata.type cannot be in $select, but it's automatically included in the response
            var response = await graphClient.GetRawJsonAsync(
                "directoryRoles?$expand=members($select=id,displayName,userPrincipalName)", ct);

            if (string.IsNullOrEmpty(response)) return 0;

            var doc = JsonDocument.Parse(response);
            var roles = new List<DirectoryRoleInventory>();

            if (doc.RootElement.TryGetProperty("value", out var valueElement))
            {
                foreach (var roleElement in valueElement.EnumerateArray())
                {
                    var role = ParseDirectoryRole(roleElement, tenantId, snapshotId);
                    roles.Add(role);
                }
            }

            await DbContext.DirectoryRoleInventories.AddRangeAsync(roles, ct);
            return roles.Count;
        }
        catch (Exception ex)
        {
            warnings.Add($"Error collecting directory roles: {ex.Message}");
            return 0;
        }
    }

    private DirectoryRoleInventory ParseDirectoryRole(JsonElement element, Guid tenantId, Guid snapshotId)
    {
        var role = new DirectoryRoleInventory
        {
            TenantId = tenantId,
            SnapshotId = snapshotId,
            RoleId = GetString(element, "id") ?? string.Empty,
            RoleTemplateId = GetString(element, "roleTemplateId") ?? string.Empty,
            DisplayName = GetString(element, "displayName") ?? string.Empty,
            Description = GetString(element, "description"),
            IsBuiltIn = true
        };

        // Determine if privileged role
        var privilegedRoleTemplateIds = new HashSet<string>
        {
            "62e90394-69f5-4237-9190-012177145e10", // Global Administrator
            "e8611ab8-c189-46e8-94e1-60213ab1f814", // Privileged Role Administrator
            "194ae4cb-b126-40b2-bd5b-6091b380977d", // Security Administrator
            "9b895d92-2cd3-44c7-9d02-a6ac2d5ea5c3", // Application Administrator
            "7be44c8a-adaf-4e2a-84d6-ab2649e08a13", // Privileged Authentication Administrator
            "158c047a-c907-4556-b7ef-446551a6b5f7", // Cloud Application Administrator
            "b1be1c3e-b65d-4f19-8427-f6fa0d97feb9", // Conditional Access Administrator
            "29232cdf-9323-42fd-ade2-1d097af3e4de", // Exchange Administrator
            "f28a1f50-f6e7-4571-818b-6a12f2af6b6c", // SharePoint Administrator
            "fe930be7-5e62-47db-91af-98c3a49a38b1", // User Administrator
        };

        role.IsPrivileged = privilegedRoleTemplateIds.Contains(role.RoleTemplateId);
        role.IsGlobalAdmin = role.RoleTemplateId == "62e90394-69f5-4237-9190-012177145e10";

        // Count members by type
        if (element.TryGetProperty("members", out var members))
        {
            var userMembers = new List<object>();
            var spMembers = new List<object>();

            foreach (var member in members.EnumerateArray())
            {
                var odataType = GetString(member, "@odata.type");
                if (odataType?.Contains("user") == true)
                {
                    role.UserMemberCount++;
                    userMembers.Add(new
                    {
                        id = GetString(member, "id"),
                        displayName = GetString(member, "displayName"),
                        userPrincipalName = GetString(member, "userPrincipalName")
                    });
                }
                else if (odataType?.Contains("servicePrincipal") == true)
                {
                    role.ServicePrincipalMemberCount++;
                    spMembers.Add(new
                    {
                        id = GetString(member, "id"),
                        displayName = GetString(member, "displayName")
                    });
                }
                else if (odataType?.Contains("group") == true)
                {
                    role.GroupMemberCount++;
                }
            }

            role.MemberCount = role.UserMemberCount + role.ServicePrincipalMemberCount + role.GroupMemberCount;
            role.UserMembersJson = userMembers.Any() ? ToJson(userMembers) : null;
            role.ServicePrincipalMembersJson = spMembers.Any() ? ToJson(spMembers) : null;
        }

        return role;
    }

    private async Task<int> CollectServicePrincipalsAsync(
        IGraphClientWrapper graphClient,
        Guid tenantId,
        Guid snapshotId,
        List<string> warnings,
        CancellationToken ct)
    {
        const string endpoint = "servicePrincipals?$select=id,appId,displayName,servicePrincipalType," +
                               "accountEnabled,publisherName,verifiedPublisher,appOwnerOrganizationId," +
                               "createdDateTime,signInAudience,tags,appRoleAssignmentRequired&$top=999";

        var servicePrincipals = new List<ServicePrincipalInventory>();
        var currentEndpoint = endpoint;

        while (!string.IsNullOrEmpty(currentEndpoint) && !ct.IsCancellationRequested)
        {
            try
            {
                var response = await graphClient.GetRawJsonAsync(currentEndpoint, ct);
                if (string.IsNullOrEmpty(response)) break;

                var doc = JsonDocument.Parse(response);

                if (doc.RootElement.TryGetProperty("value", out var valueElement))
                {
                    foreach (var spElement in valueElement.EnumerateArray())
                    {
                        var sp = ParseServicePrincipal(spElement, tenantId, snapshotId);
                        servicePrincipals.Add(sp);
                    }
                }

                currentEndpoint = doc.RootElement.TryGetProperty("@odata.nextLink", out var nextLink)
                    ? nextLink.GetString()
                    : null;
            }
            catch (Exception ex)
            {
                warnings.Add($"Error fetching service principals page: {ex.Message}");
                break;
            }
        }

        await DbContext.ServicePrincipalInventories.AddRangeAsync(servicePrincipals, ct);
        return servicePrincipals.Count;
    }

    private ServicePrincipalInventory ParseServicePrincipal(JsonElement element, Guid tenantId, Guid snapshotId)
    {
        var sp = new ServicePrincipalInventory
        {
            TenantId = tenantId,
            SnapshotId = snapshotId,
            ObjectId = GetString(element, "id") ?? string.Empty,
            AppId = GetString(element, "appId") ?? string.Empty,
            DisplayName = GetString(element, "displayName") ?? string.Empty,
            ServicePrincipalType = GetString(element, "servicePrincipalType") ?? string.Empty,
            AccountEnabled = GetBool(element, "accountEnabled"),
            PublisherName = GetString(element, "publisherName"),
            AppOwnerOrganizationId = GetString(element, "appOwnerOrganizationId"),
            CreatedDateTime = GetDateTime(element, "createdDateTime"),
            SignInAudience = GetString(element, "signInAudience"),
            IsAppRoleAssignmentRequired = GetBool(element, "appRoleAssignmentRequired")
        };

        // Check for verified publisher
        if (element.TryGetProperty("verifiedPublisher", out var verifiedPublisher))
        {
            sp.VerifiedPublisher = GetString(verifiedPublisher, "displayName");
        }

        // Check if Microsoft first-party app
        var msAppIds = new HashSet<string>
        {
            "00000003-0000-0000-c000-000000000000", // Microsoft Graph
            "00000002-0000-0ff1-ce00-000000000000", // SharePoint
            "00000004-0000-0ff1-ce00-000000000000", // Outlook
            "00000002-0000-0000-c000-000000000000", // Azure AD Graph
        };
        sp.IsMicrosoftFirstParty = msAppIds.Contains(sp.AppId) ||
                                   sp.AppOwnerOrganizationId == "f8cdef31-a31e-4b4a-93e4-5f571e91255a" || // Microsoft tenant
                                   sp.PublisherName?.Contains("Microsoft") == true;

        // Parse tags
        if (element.TryGetProperty("tags", out var tags) && tags.ValueKind == JsonValueKind.Array)
        {
            var tagList = new List<string>();
            foreach (var tag in tags.EnumerateArray())
            {
                if (tag.ValueKind == JsonValueKind.String)
                    tagList.Add(tag.GetString()!);
            }
            sp.TagsJson = tagList.Any() ? ToJson(tagList) : null;
        }

        return sp;
    }

    private async Task<int> CollectConditionalAccessPoliciesAsync(
        IGraphClientWrapper graphClient,
        Guid tenantId,
        Guid snapshotId,
        List<string> warnings,
        CancellationToken ct)
    {
        try
        {
            var response = await graphClient.GetRawJsonAsync("identity/conditionalAccess/policies", ct);
            if (string.IsNullOrEmpty(response)) return 0;

            var doc = JsonDocument.Parse(response);
            var policies = new List<ConditionalAccessPolicyInventory>();

            if (doc.RootElement.TryGetProperty("value", out var valueElement))
            {
                foreach (var policyElement in valueElement.EnumerateArray())
                {
                    var policy = ParseConditionalAccessPolicy(policyElement, tenantId, snapshotId);
                    policies.Add(policy);
                }
            }

            await DbContext.ConditionalAccessPolicyInventories.AddRangeAsync(policies, ct);
            return policies.Count;
        }
        catch (Exception ex)
        {
            warnings.Add($"Error collecting CA policies: {ex.Message}");
            return 0;
        }
    }

    private ConditionalAccessPolicyInventory ParseConditionalAccessPolicy(JsonElement element, Guid tenantId, Guid snapshotId)
    {
        var policy = new ConditionalAccessPolicyInventory
        {
            TenantId = tenantId,
            SnapshotId = snapshotId,
            PolicyId = GetString(element, "id") ?? string.Empty,
            DisplayName = GetString(element, "displayName") ?? string.Empty,
            State = GetString(element, "state") ?? string.Empty,
            CreatedDateTime = GetDateTime(element, "createdDateTime"),
            ModifiedDateTime = GetDateTime(element, "modifiedDateTime")
        };

        // Parse conditions
        if (element.TryGetProperty("conditions", out var conditions))
        {
            // Users
            if (conditions.TryGetProperty("users", out var users))
            {
                policy.IncludeUsersJson = users.TryGetProperty("includeUsers", out var includeUsers)
                    ? includeUsers.GetRawText()
                    : null;
                policy.ExcludeUsersJson = users.TryGetProperty("excludeUsers", out var excludeUsers)
                    ? excludeUsers.GetRawText()
                    : null;
                policy.IncludeGroupsJson = users.TryGetProperty("includeGroups", out var includeGroups)
                    ? includeGroups.GetRawText()
                    : null;
                policy.ExcludeGroupsJson = users.TryGetProperty("excludeGroups", out var excludeGroups)
                    ? excludeGroups.GetRawText()
                    : null;
                policy.IncludeRolesJson = users.TryGetProperty("includeRoles", out var includeRoles)
                    ? includeRoles.GetRawText()
                    : null;

                // Check for "All" users
                if (users.TryGetProperty("includeUsers", out var iu) && iu.ValueKind == JsonValueKind.Array)
                {
                    foreach (var u in iu.EnumerateArray())
                    {
                        if (u.ValueKind == JsonValueKind.String && u.GetString() == "All")
                        {
                            policy.IncludesAllUsers = true;
                            break;
                        }
                    }
                }

                // Count exclusions
                if (users.TryGetProperty("excludeUsers", out var eu) && eu.ValueKind == JsonValueKind.Array)
                    policy.ExcludedUserCount = eu.GetArrayLength();
                if (users.TryGetProperty("excludeGroups", out var eg) && eg.ValueKind == JsonValueKind.Array)
                    policy.ExcludedGroupCount = eg.GetArrayLength();
            }

            // Applications
            if (conditions.TryGetProperty("applications", out var apps))
            {
                policy.IncludeApplicationsJson = apps.TryGetProperty("includeApplications", out var includeApps)
                    ? includeApps.GetRawText()
                    : null;
                policy.ExcludeApplicationsJson = apps.TryGetProperty("excludeApplications", out var excludeApps)
                    ? excludeApps.GetRawText()
                    : null;

                // Check for "All" apps
                if (apps.TryGetProperty("includeApplications", out var ia) && ia.ValueKind == JsonValueKind.Array)
                {
                    foreach (var a in ia.EnumerateArray())
                    {
                        if (a.ValueKind == JsonValueKind.String && a.GetString() == "All")
                        {
                            policy.IncludesAllApps = true;
                            break;
                        }
                        if (a.ValueKind == JsonValueKind.String && a.GetString() == "Office365")
                        {
                            policy.IncludesOffice365 = true;
                        }
                    }
                }

                if (apps.TryGetProperty("excludeApplications", out var ea) && ea.ValueKind == JsonValueKind.Array)
                    policy.ExcludedAppCount = ea.GetArrayLength();
            }

            // Client app types
            if (conditions.TryGetProperty("clientAppTypes", out var clientApps))
            {
                policy.ClientAppTypesJson = clientApps.GetRawText();
                if (clientApps.ValueKind == JsonValueKind.Array)
                {
                    foreach (var appType in clientApps.EnumerateArray())
                    {
                        var type = appType.GetString();
                        if (type == "browser") policy.IncludesBrowser = true;
                        if (type == "mobileAppsAndDesktopClients") policy.IncludesMobileApps = true;
                        if (type == "exchangeActiveSync" || type == "other") policy.IncludesLegacyClients = true;
                    }
                }
            }
        }

        // Parse grant controls
        if (element.TryGetProperty("grantControls", out var grantControls))
        {
            policy.GrantControlsJson = grantControls.GetRawText();
            policy.GrantControlOperator = GetString(grantControls, "operator");

            if (grantControls.TryGetProperty("builtInControls", out var builtIn) && builtIn.ValueKind == JsonValueKind.Array)
            {
                foreach (var control in builtIn.EnumerateArray())
                {
                    var controlName = control.GetString();
                    if (controlName == "mfa") policy.RequiresMfa = true;
                    if (controlName == "compliantDevice") policy.RequiresCompliantDevice = true;
                    if (controlName == "domainJoinedDevice") policy.RequiresHybridAzureAdJoin = true;
                    if (controlName == "approvedApplication") policy.RequiresApprovedApp = true;
                    if (controlName == "compliantApplication") policy.RequiresAppProtection = true;
                    if (controlName == "passwordChange") policy.RequiresPasswordChange = true;
                    if (controlName == "block") policy.BlocksAccess = true;
                }
            }
        }

        // Check if blocks legacy auth (by blocking legacy client app types)
        policy.BlocksLegacyAuth = policy.IncludesLegacyClients && policy.BlocksAccess;

        // Parse session controls
        if (element.TryGetProperty("sessionControls", out var sessionControls))
        {
            policy.SessionControlsJson = sessionControls.GetRawText();

            if (sessionControls.TryGetProperty("signInFrequency", out var signInFreq))
            {
                policy.HasSignInFrequency = GetBool(signInFreq, "isEnabled");
                var value = GetInt(signInFreq, "value");
                var type = GetString(signInFreq, "type");
                policy.SignInFrequencyValue = $"{value} {type}";
            }

            if (sessionControls.TryGetProperty("persistentBrowser", out var persistentBrowser))
            {
                policy.HasPersistentBrowser = GetBool(persistentBrowser, "isEnabled");
            }

            if (sessionControls.TryGetProperty("cloudAppSecurity", out var cas))
            {
                policy.HasCloudAppSecurity = GetBool(cas, "isEnabled");
            }
        }

        return policy;
    }

    private async Task<int> CollectNamedLocationsAsync(
        IGraphClientWrapper graphClient,
        Guid tenantId,
        Guid snapshotId,
        List<string> warnings,
        CancellationToken ct)
    {
        try
        {
            var response = await graphClient.GetRawJsonAsync("identity/conditionalAccess/namedLocations", ct);
            if (string.IsNullOrEmpty(response)) return 0;

            var doc = JsonDocument.Parse(response);
            var locations = new List<NamedLocationInventory>();

            if (doc.RootElement.TryGetProperty("value", out var valueElement))
            {
                foreach (var locationElement in valueElement.EnumerateArray())
                {
                    var location = new NamedLocationInventory
                    {
                        TenantId = tenantId,
                        SnapshotId = snapshotId,
                        LocationId = GetString(locationElement, "id") ?? string.Empty,
                        DisplayName = GetString(locationElement, "displayName") ?? string.Empty,
                        CreatedDateTime = GetDateTime(locationElement, "createdDateTime"),
                        ModifiedDateTime = GetDateTime(locationElement, "modifiedDateTime")
                    };

                    var odataType = GetString(locationElement, "@odata.type");
                    if (odataType?.Contains("ipNamedLocation") == true)
                    {
                        location.LocationType = "IP";
                        location.IsTrusted = GetBool(locationElement, "isTrusted");
                        if (locationElement.TryGetProperty("ipRanges", out var ipRanges))
                        {
                            location.IpRangesJson = ipRanges.GetRawText();
                            location.IpRangeCount = ipRanges.GetArrayLength();
                        }
                    }
                    else if (odataType?.Contains("countryNamedLocation") == true)
                    {
                        location.LocationType = "Country";
                        location.IncludeUnknownCountriesAndRegions = GetBool(locationElement, "includeUnknownCountriesAndRegions");
                        if (locationElement.TryGetProperty("countriesAndRegions", out var countries))
                        {
                            location.CountriesAndRegionsJson = countries.GetRawText();
                            location.CountryCount = countries.GetArrayLength();
                        }
                    }

                    locations.Add(location);
                }
            }

            await DbContext.NamedLocationInventories.AddRangeAsync(locations, ct);
            return locations.Count;
        }
        catch (Exception ex)
        {
            warnings.Add($"Error collecting named locations: {ex.Message}");
            return 0;
        }
    }

    private static bool IsE5LicenseSku(string skuId)
    {
        var e5SkuIds = new HashSet<string>
        {
            "06ebc4ee-1bb5-47dd-8120-11324bc54e06", // Microsoft 365 E5
            "26124093-3d78-432b-b5dc-48bf992543d5", // Microsoft 365 E5 (no Audio Conferencing)
            "c7df2760-2c81-4ef7-b578-5b5392b571df", // Office 365 E5
            "184efa21-98c3-4e5d-95ab-d07053a96e67", // Microsoft 365 E5 Compliance
            "eb56d846-a2d2-4a71-938a-bfc5e37fe5c9", // Microsoft 365 E5 Security
            "26d45bd9-adf1-46cd-a9e1-51e9a5524128", // Microsoft 365 E5 Developer (without Windows and Audio Conferencing)
        };
        return e5SkuIds.Contains(skuId);
    }

    private static bool IsE3LicenseSku(string skuId)
    {
        var e3SkuIds = new HashSet<string>
        {
            "05e9a617-0261-4cee-bb44-138d3ef5d965", // Microsoft 365 E3
            "6fd2c87f-b296-42f0-b197-1e91e994b900", // Office 365 E3
        };
        return e3SkuIds.Contains(skuId);
    }
}
