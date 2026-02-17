using System.Text.Json;
using Cloudativ.Assessment.Domain.Entities.Inventory;
using Cloudativ.Assessment.Domain.Enums;
using Cloudativ.Assessment.Domain.Interfaces;
using Cloudativ.Assessment.Infrastructure.Data;
using Microsoft.Extensions.Logging;

namespace Cloudativ.Assessment.Infrastructure.Inventory.Modules;

/// <summary>
/// Inventory module for SharePoint, OneDrive, and Teams data collection.
/// Collects sites, teams, sharing settings, and collaboration configurations.
/// </summary>
public class SharePointOneDriveTeamsInventoryModule : BaseInventoryModule
{
    public override InventoryDomain Domain => InventoryDomain.SharePointOneDriveTeams;
    public override string DisplayName => "SharePoint, OneDrive & Teams";
    public override string Description => "Collects SharePoint sites, Teams, sharing settings, and collaboration configurations.";

    public override IReadOnlyList<string> RequiredPermissions => new[]
    {
        "Sites.Read.All",
        "Team.ReadBasic.All",
        "TeamSettings.Read.All",
        "Channel.ReadBasic.All",
        "Group.Read.All",
        "User.Read.All"
    };

    public SharePointOneDriveTeamsInventoryModule(
        ApplicationDbContext dbContext,
        ILogger<SharePointOneDriveTeamsInventoryModule> logger)
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
            // 1. Collect SharePoint Sites
            var siteCount = await CollectSharePointSitesAsync(graphClient, tenantId, snapshotId, warnings, ct);
            breakdown["SharePointSites"] = siteCount;
            totalItems += siteCount;
            Logger.LogInformation("Collected {Count} SharePoint sites", siteCount);

            // 2. Collect SharePoint Settings (tenant-level)
            var settingsCount = await CollectSharePointSettingsAsync(graphClient, tenantId, snapshotId, siteCount, warnings, ct);
            breakdown["SharePointSettings"] = settingsCount;
            totalItems += settingsCount;
            Logger.LogInformation("Collected SharePoint settings");

            // 3. Collect Teams
            var teamsCount = await CollectTeamsAsync(graphClient, tenantId, snapshotId, warnings, ct);
            breakdown["Teams"] = teamsCount;
            totalItems += teamsCount;
            Logger.LogInformation("Collected {Count} Teams", teamsCount);

            // 4. Collect Teams Settings (tenant-level policies)
            var teamsSettingsCount = await CollectTeamsSettingsAsync(graphClient, tenantId, snapshotId, teamsCount, warnings, ct);
            breakdown["TeamsSettings"] = teamsSettingsCount;
            totalItems += teamsSettingsCount;
            Logger.LogInformation("Collected Teams settings");

            await DbContext.SaveChangesAsync(ct);

            return Success(totalItems, DateTime.UtcNow - startTime, breakdown, warnings);
        }
        catch (Exception ex)
        {
            Logger.LogError(ex, "Error collecting SharePoint/Teams inventory for tenant {TenantId}", tenantId);
            return Failure(ex.Message, DateTime.UtcNow - startTime, warnings);
        }
    }

    private async Task<int> CollectSharePointSitesAsync(
        IGraphClientWrapper graphClient,
        Guid tenantId,
        Guid snapshotId,
        List<string> warnings,
        CancellationToken ct)
    {
        const string endpoint = "sites?$select=id,webUrl,displayName,description,createdDateTime,lastModifiedDateTime," +
                               "siteCollection,root&$top=999&search=*";

        var sites = new List<SharePointSiteInventory>();
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
                    foreach (var siteElement in valueElement.EnumerateArray())
                    {
                        var site = ParseSharePointSite(siteElement, tenantId, snapshotId);
                        if (site != null)
                        {
                            sites.Add(site);
                        }
                    }
                }

                currentEndpoint = doc.RootElement.TryGetProperty("@odata.nextLink", out var nextLink)
                    ? nextLink.GetString()
                    : null;
            }
            catch (Exception ex)
            {
                warnings.Add($"Error fetching SharePoint sites page: {ex.Message}");
                break;
            }
        }

        // Enrich sites with additional details
        await EnrichSitesWithDetailsAsync(graphClient, sites, warnings, ct);

        await DbContext.SharePointSiteInventories.AddRangeAsync(sites, ct);
        return sites.Count;
    }

    private SharePointSiteInventory? ParseSharePointSite(JsonElement element, Guid tenantId, Guid snapshotId)
    {
        var siteId = GetString(element, "id");
        var webUrl = GetString(element, "webUrl");

        // Skip personal OneDrive sites and system sites
        if (string.IsNullOrEmpty(siteId) || string.IsNullOrEmpty(webUrl))
            return null;

        // Skip personal sites (OneDrive) - they have /personal/ in the URL
        if (webUrl.Contains("/personal/", StringComparison.OrdinalIgnoreCase))
            return null;

        var site = new SharePointSiteInventory
        {
            TenantId = tenantId,
            SnapshotId = snapshotId,
            SiteId = siteId,
            WebUrl = webUrl,
            Title = GetString(element, "displayName"),
            Description = GetString(element, "description"),
            CreatedDateTime = GetDateTime(element, "createdDateTime"),
            LastModifiedDateTime = GetDateTime(element, "lastModifiedDateTime")
        };

        // Determine site type from URL
        if (webUrl.Contains("/sites/", StringComparison.OrdinalIgnoreCase))
        {
            site.IsTeamSite = true;
        }
        else if (webUrl.Contains("/teams/", StringComparison.OrdinalIgnoreCase))
        {
            site.IsTeamSite = true;
            site.IsGroupConnected = true;
        }

        // Check for root site
        if (element.TryGetProperty("root", out _))
        {
            site.Template = "RootSite";
        }

        // Check for site collection info
        if (element.TryGetProperty("siteCollection", out var siteCollection))
        {
            var hostname = GetString(siteCollection, "hostname");
            if (!string.IsNullOrEmpty(hostname))
            {
                site.Template = hostname.Contains("-my.sharepoint.com") ? "OneDrive" : "SiteCollection";
            }
        }

        return site;
    }

    private async Task EnrichSitesWithDetailsAsync(
        IGraphClientWrapper graphClient,
        List<SharePointSiteInventory> sites,
        List<string> warnings,
        CancellationToken ct)
    {
        foreach (var site in sites.Take(100)) // Limit to avoid too many API calls
        {
            if (ct.IsCancellationRequested) break;

            try
            {
                // Get site drive for storage info
                var driveResponse = await graphClient.GetRawJsonAsync($"sites/{site.SiteId}/drive?$select=quota", ct);
                if (!string.IsNullOrEmpty(driveResponse))
                {
                    var driveDoc = JsonDocument.Parse(driveResponse);
                    if (driveDoc.RootElement.TryGetProperty("quota", out var quota))
                    {
                        site.StorageUsedBytes = GetLong(quota, "used");
                        site.StorageQuotaBytes = GetLong(quota, "total");
                        if (site.StorageQuotaBytes > 0)
                        {
                            site.StoragePercentUsed = (double)site.StorageUsedBytes / site.StorageQuotaBytes * 100;
                        }
                    }
                }

                // Get site lists count
                var listsResponse = await graphClient.GetRawJsonAsync($"sites/{site.SiteId}/lists?$select=id&$top=999", ct);
                if (!string.IsNullOrEmpty(listsResponse))
                {
                    var listsDoc = JsonDocument.Parse(listsResponse);
                    if (listsDoc.RootElement.TryGetProperty("value", out var lists))
                    {
                        site.ListCount = lists.GetArrayLength();
                    }
                }
            }
            catch (Exception ex)
            {
                Logger.LogDebug("Could not enrich site {SiteId}: {Message}", site.SiteId, ex.Message);
            }
        }
    }

    private async Task<int> CollectSharePointSettingsAsync(
        IGraphClientWrapper graphClient,
        Guid tenantId,
        Guid snapshotId,
        int totalSiteCount,
        List<string> warnings,
        CancellationToken ct)
    {
        try
        {
            var settings = new SharePointSettingsInventory
            {
                TenantId = tenantId,
                SnapshotId = snapshotId,
                TotalSiteCount = totalSiteCount
            };

            // Get organization info for tenant-level settings
            var orgResponse = await graphClient.GetRawJsonAsync("organization?$select=id,displayName", ct);
            if (!string.IsNullOrEmpty(orgResponse))
            {
                // Organization retrieved successfully
            }

            // Try to get SharePoint admin settings if available
            try
            {
                var adminResponse = await graphClient.GetRawJsonAsync("admin/sharepoint/settings", ct);
                if (!string.IsNullOrEmpty(adminResponse))
                {
                    var adminDoc = JsonDocument.Parse(adminResponse);

                    settings.SharingCapability = GetString(adminDoc.RootElement, "sharingCapability") ?? "Unknown";
                    settings.DefaultSharingLinkType = GetString(adminDoc.RootElement, "defaultSharingLinkType") ?? "Unknown";
                    settings.DefaultLinkPermission = GetString(adminDoc.RootElement, "defaultLinkPermission") ?? "Unknown";
                    settings.ExternalUserExpirationRequired = GetBool(adminDoc.RootElement, "externalUserExpirationRequired");
                    settings.ExternalUserExpirationDays = GetNullableInt(adminDoc.RootElement, "externalUserExpireInDays");
                }
            }
            catch
            {
                // SharePoint admin API may not be available
                settings.SharingCapability = "Unknown";
                settings.DefaultSharingLinkType = "Unknown";
                settings.DefaultLinkPermission = "Unknown";
                warnings.Add("SharePoint admin settings not accessible - requires SharePoint Admin permissions");
            }

            await DbContext.SharePointSettingsInventories.AddAsync(settings, ct);
            return 1;
        }
        catch (Exception ex)
        {
            warnings.Add($"Error collecting SharePoint settings: {ex.Message}");
            return 0;
        }
    }

    private async Task<int> CollectTeamsAsync(
        IGraphClientWrapper graphClient,
        Guid tenantId,
        Guid snapshotId,
        List<string> warnings,
        CancellationToken ct)
    {
        // Get all groups that are Teams-enabled
        const string endpoint = "groups?$filter=resourceProvisioningOptions/Any(x:x eq 'Team')&" +
                               "$select=id,displayName,description,visibility,createdDateTime,mail,mailNickname," +
                               "classification,membershipRule&$top=999";

        var teams = new List<TeamsInventory>();
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
                        var team = ParseTeam(groupElement, tenantId, snapshotId);
                        teams.Add(team);
                    }
                }

                currentEndpoint = doc.RootElement.TryGetProperty("@odata.nextLink", out var nextLink)
                    ? nextLink.GetString()
                    : null;
            }
            catch (Exception ex)
            {
                warnings.Add($"Error fetching Teams page: {ex.Message}");
                break;
            }
        }

        // Enrich teams with additional details
        await EnrichTeamsWithDetailsAsync(graphClient, teams, warnings, ct);

        await DbContext.TeamsInventories.AddRangeAsync(teams, ct);
        return teams.Count;
    }

    private TeamsInventory ParseTeam(JsonElement element, Guid tenantId, Guid snapshotId)
    {
        var team = new TeamsInventory
        {
            TenantId = tenantId,
            SnapshotId = snapshotId,
            TeamId = GetString(element, "id") ?? string.Empty,
            DisplayName = GetString(element, "displayName") ?? string.Empty,
            Description = GetString(element, "description"),
            Visibility = GetString(element, "visibility") ?? "Private",
            CreatedDateTime = GetDateTime(element, "createdDateTime"),
            Classification = GetString(element, "classification"),
            GroupId = GetString(element, "id"),
            MailNickname = GetString(element, "mailNickname")
        };

        return team;
    }

    private async Task EnrichTeamsWithDetailsAsync(
        IGraphClientWrapper graphClient,
        List<TeamsInventory> teams,
        List<string> warnings,
        CancellationToken ct)
    {
        foreach (var team in teams)
        {
            if (ct.IsCancellationRequested) break;

            try
            {
                // Get team settings
                var teamResponse = await graphClient.GetRawJsonAsync($"teams/{team.TeamId}", ct);
                if (!string.IsNullOrEmpty(teamResponse))
                {
                    var teamDoc = JsonDocument.Parse(teamResponse);

                    team.IsArchived = GetBool(teamDoc.RootElement, "isArchived");

                    // Member settings
                    if (teamDoc.RootElement.TryGetProperty("memberSettings", out var memberSettings))
                    {
                        team.AllowCreateUpdateChannels = GetBool(memberSettings, "allowCreateUpdateChannels");
                        team.AllowDeleteChannels = GetBool(memberSettings, "allowDeleteChannels");
                        team.AllowAddRemoveApps = GetBool(memberSettings, "allowAddRemoveApps");
                        team.AllowCreateUpdateRemoveTabs = GetBool(memberSettings, "allowCreateUpdateRemoveTabs");
                        team.AllowCreateUpdateRemoveConnectors = GetBool(memberSettings, "allowCreateUpdateRemoveConnectors");
                    }

                    // Guest settings
                    if (teamDoc.RootElement.TryGetProperty("guestSettings", out var guestSettings))
                    {
                        team.AllowGuestCreateUpdateChannels = GetBool(guestSettings, "allowCreateUpdateChannels");
                        team.AllowGuestDeleteChannels = GetBool(guestSettings, "allowDeleteChannels");
                    }

                    // Messaging settings
                    if (teamDoc.RootElement.TryGetProperty("messagingSettings", out var messagingSettings))
                    {
                        team.AllowUserEditMessages = GetBool(messagingSettings, "allowUserEditMessages");
                        team.AllowUserDeleteMessages = GetBool(messagingSettings, "allowUserDeleteMessages");
                        team.AllowOwnerDeleteMessages = GetBool(messagingSettings, "allowOwnerDeleteMessages");
                        team.AllowTeamMentions = GetBool(messagingSettings, "allowTeamMentions");
                        team.AllowChannelMentions = GetBool(messagingSettings, "allowChannelMentions");
                    }

                    // Fun settings
                    if (teamDoc.RootElement.TryGetProperty("funSettings", out var funSettings))
                    {
                        team.AllowGiphy = GetBool(funSettings, "allowGiphy");
                        team.AllowStickersAndMemes = GetBool(funSettings, "allowStickersAndMemes");
                        team.AllowCustomMemes = GetBool(funSettings, "allowCustomMemes");
                    }
                }

                // Get channels
                var channelsResponse = await graphClient.GetRawJsonAsync($"teams/{team.TeamId}/channels?$select=id,displayName,membershipType", ct);
                if (!string.IsNullOrEmpty(channelsResponse))
                {
                    var channelsDoc = JsonDocument.Parse(channelsResponse);
                    if (channelsDoc.RootElement.TryGetProperty("value", out var channels))
                    {
                        team.TotalChannelCount = channels.GetArrayLength();

                        foreach (var channel in channels.EnumerateArray())
                        {
                            var membershipType = GetString(channel, "membershipType");
                            switch (membershipType)
                            {
                                case "standard":
                                    team.StandardChannelCount++;
                                    break;
                                case "private":
                                    team.PrivateChannelCount++;
                                    break;
                                case "shared":
                                    team.SharedChannelCount++;
                                    break;
                            }
                        }
                    }
                }

                // Get members
                var membersResponse = await graphClient.GetRawJsonAsync($"groups/{team.GroupId}/members?$select=id,userType&$top=999", ct);
                if (!string.IsNullOrEmpty(membersResponse))
                {
                    var membersDoc = JsonDocument.Parse(membersResponse);
                    if (membersDoc.RootElement.TryGetProperty("value", out var members))
                    {
                        team.MemberCount = members.GetArrayLength();

                        foreach (var member in members.EnumerateArray())
                        {
                            var userType = GetString(member, "userType");
                            if (userType == "Guest")
                            {
                                team.GuestCount++;
                                team.HasExternalMembers = true;
                            }
                        }
                    }
                }

                // Get owners
                var ownersResponse = await graphClient.GetRawJsonAsync($"groups/{team.GroupId}/owners?$select=id,displayName&$top=999", ct);
                if (!string.IsNullOrEmpty(ownersResponse))
                {
                    var ownersDoc = JsonDocument.Parse(ownersResponse);
                    if (ownersDoc.RootElement.TryGetProperty("value", out var owners))
                    {
                        team.OwnerCount = owners.GetArrayLength();

                        var ownersList = new List<object>();
                        foreach (var owner in owners.EnumerateArray())
                        {
                            ownersList.Add(new
                            {
                                id = GetString(owner, "id"),
                                displayName = GetString(owner, "displayName")
                            });
                        }
                        team.OwnersJson = ownersList.Count > 0 ? ToJson(ownersList) : null;
                    }
                }
            }
            catch (Exception ex)
            {
                Logger.LogDebug("Could not enrich team {TeamId}: {Message}", team.TeamId, ex.Message);
            }
        }
    }

    private async Task<int> CollectTeamsSettingsAsync(
        IGraphClientWrapper graphClient,
        Guid tenantId,
        Guid snapshotId,
        int totalTeamsCount,
        List<string> warnings,
        CancellationToken ct)
    {
        try
        {
            var settings = new TeamsSettingsInventory
            {
                TenantId = tenantId,
                SnapshotId = snapshotId,
                TotalTeamsCount = totalTeamsCount
            };

            // Try to get Teams tenant settings
            try
            {
                // External access settings via teamwork endpoint
                var teamworkResponse = await graphClient.GetRawJsonAsync("teamwork", ct);
                if (!string.IsNullOrEmpty(teamworkResponse))
                {
                    // Teamwork endpoint available
                }
            }
            catch
            {
                warnings.Add("Teams tenant settings not fully accessible");
            }

            // Count teams with guests and archived teams from collected data
            var teamsInDb = DbContext.TeamsInventories
                .Where(t => t.TenantId == tenantId && t.SnapshotId == snapshotId)
                .ToList();

            settings.TeamsWithGuests = teamsInDb.Count(t => t.HasExternalMembers);
            settings.ArchivedTeamsCount = teamsInDb.Count(t => t.IsArchived);
            settings.ActiveTeamsCount = teamsInDb.Count(t => !t.IsArchived);
            settings.PrivateChannelCount = teamsInDb.Sum(t => t.PrivateChannelCount);
            settings.SharedChannelCount = teamsInDb.Sum(t => t.SharedChannelCount);

            // Set default values for policies that require admin access
            settings.AllowGuestAccess = true; // Default assumption
            settings.AllowThirdPartyApps = true;
            settings.AllowSideloading = false;

            await DbContext.TeamsSettingsInventories.AddAsync(settings, ct);
            return 1;
        }
        catch (Exception ex)
        {
            warnings.Add($"Error collecting Teams settings: {ex.Message}");
            return 0;
        }
    }

    private static long GetLong(JsonElement element, string propertyName)
    {
        if (element.TryGetProperty(propertyName, out var value))
        {
            return value.ValueKind == JsonValueKind.Number ? value.GetInt64() : 0;
        }
        return 0;
    }

    private static int? GetNullableInt(JsonElement element, string propertyName)
    {
        if (element.TryGetProperty(propertyName, out var value))
        {
            if (value.ValueKind == JsonValueKind.Number)
                return value.GetInt32();
            if (value.ValueKind == JsonValueKind.Null)
                return null;
        }
        return null;
    }
}
