using System.Text.Json;
using Cloudativ.Assessment.Domain.Entities.Inventory;
using Cloudativ.Assessment.Domain.Enums;
using Cloudativ.Assessment.Domain.Interfaces;
using Cloudativ.Assessment.Infrastructure.Data;
using Cloudativ.Assessment.Infrastructure.Services;
using Microsoft.Extensions.Logging;

namespace Cloudativ.Assessment.Infrastructure.Inventory.Modules;

/// <summary>
/// Inventory module for Tenant & Org Baseline data collection.
/// Collects tenant info, domains, subscriptions, service health, and org-wide settings.
/// </summary>
public class TenantBaselineInventoryModule : BaseInventoryModule
{
    public override InventoryDomain Domain => InventoryDomain.TenantBaseline;
    public override string DisplayName => "Tenant & Org Baseline";
    public override string Description => "Collects tenant info, domains, subscriptions, service health, and org-wide settings.";

    public override IReadOnlyList<string> RequiredPermissions => new[]
    {
        "Organization.Read.All",
        "Directory.Read.All"
    };

    public TenantBaselineInventoryModule(
        ApplicationDbContext dbContext,
        ILogger<TenantBaselineInventoryModule> logger)
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
            // 1. Collect Organization Info
            var orgCollected = await CollectOrganizationAsync(graphClient, tenantId, snapshotId, warnings, ct);
            breakdown["Organization"] = orgCollected ? 1 : 0;
            totalItems += orgCollected ? 1 : 0;

            // 2. Collect Subscribed SKUs (Licenses)
            var skuCount = await CollectSubscribedSkusAsync(graphClient, tenantId, snapshotId, warnings, ct);
            breakdown["Subscriptions"] = skuCount;
            totalItems += skuCount;
            Logger.LogInformation("Collected {Count} subscribed SKUs", skuCount);

            // 3. Collect Service Health (optional)
            var healthCount = await CollectServiceHealthAsync(graphClient, tenantId, snapshotId, warnings, ct);
            breakdown["ServiceHealth"] = healthCount;

            await DbContext.SaveChangesAsync(ct);

            return Success(totalItems, DateTime.UtcNow - startTime, breakdown, warnings);
        }
        catch (Exception ex)
        {
            Logger.LogError(ex, "Error collecting tenant baseline for tenant {TenantId}", tenantId);
            return Failure(ex.Message, DateTime.UtcNow - startTime, warnings);
        }
    }

    private async Task<bool> CollectOrganizationAsync(
        IGraphClientWrapper graphClient,
        Guid tenantId,
        Guid snapshotId,
        List<string> warnings,
        CancellationToken ct)
    {
        try
        {
            var response = await graphClient.GetRawJsonAsync("organization", ct);
            if (string.IsNullOrEmpty(response)) return false;

            var doc = JsonDocument.Parse(response);

            if (doc.RootElement.TryGetProperty("value", out var orgs) && orgs.ValueKind == JsonValueKind.Array)
            {
                foreach (var org in orgs.EnumerateArray())
                {
                    var tenantInfo = new TenantInfo
                    {
                        TenantId = tenantId,
                        SnapshotId = snapshotId,
                        AzureTenantId = GetString(org, "id") ?? string.Empty,
                        DisplayName = GetString(org, "displayName") ?? string.Empty,
                        PreferredDataLocation = GetString(org, "preferredDataLocation"),
                        DefaultUsageLocation = GetString(org, "defaultUsageLocation")
                    };

                    // Parse verified domains
                    if (org.TryGetProperty("verifiedDomains", out var domains) && domains.ValueKind == JsonValueKind.Array)
                    {
                        var domainList = new List<object>();
                        foreach (var domain in domains.EnumerateArray())
                        {
                            var domainName = GetString(domain, "name");
                            var isDefault = GetBool(domain, "isDefault");
                            var isInitial = GetBool(domain, "isInitial");
                            var type = GetString(domain, "type");

                            domainList.Add(new
                            {
                                name = domainName,
                                isDefault,
                                isInitial,
                                type
                            });

                            if (isDefault)
                            {
                                tenantInfo.PrimaryDomain = domainName;
                            }
                        }

                        tenantInfo.VerifiedDomainsJson = ToJson(domainList);
                        tenantInfo.VerifiedDomainCount = domainList.Count;
                    }

                    // Parse technical notification emails
                    if (org.TryGetProperty("technicalNotificationMails", out var techMails) && techMails.ValueKind == JsonValueKind.Array)
                    {
                        var emails = new List<string>();
                        foreach (var email in techMails.EnumerateArray())
                        {
                            if (email.ValueKind == JsonValueKind.String)
                                emails.Add(email.GetString()!);
                        }
                        tenantInfo.TechnicalNotificationMails = ToJson(emails);
                    }

                    // Check Multi-Geo
                    if (org.TryGetProperty("assignedPlans", out var plans) && plans.ValueKind == JsonValueKind.Array)
                    {
                        foreach (var plan in plans.EnumerateArray())
                        {
                            var servicePlanId = GetString(plan, "servicePlanId");
                            // Multi-Geo Capabilities service plan ID
                            if (servicePlanId == "b9b5f21b-8f69-40e6-9b99-e3c764bc8c4c")
                            {
                                tenantInfo.IsMultiGeoEnabled = true;
                                break;
                            }
                        }
                    }

                    await DbContext.TenantInfos.AddAsync(tenantInfo, ct);
                    return true;
                }
            }

            return false;
        }
        catch (Exception ex)
        {
            warnings.Add($"Error collecting organization info: {ex.Message}");
            return false;
        }
    }

    private async Task<int> CollectSubscribedSkusAsync(
        IGraphClientWrapper graphClient,
        Guid tenantId,
        Guid snapshotId,
        List<string> warnings,
        CancellationToken ct)
    {
        try
        {
            var response = await graphClient.GetRawJsonAsync("subscribedSkus", ct);
            if (string.IsNullOrEmpty(response)) return 0;

            var doc = JsonDocument.Parse(response);
            var subscriptions = new List<LicenseSubscription>();

            if (doc.RootElement.TryGetProperty("value", out var skus) && skus.ValueKind == JsonValueKind.Array)
            {
                foreach (var sku in skus.EnumerateArray())
                {
                    var subscription = new LicenseSubscription
                    {
                        TenantId = tenantId,
                        SnapshotId = snapshotId,
                        SkuId = GetString(sku, "skuId") ?? string.Empty,
                        SkuPartNumber = GetString(sku, "skuPartNumber") ?? string.Empty,
                        AppliesTo = GetString(sku, "appliesTo"),
                        CapabilityStatus = GetString(sku, "capabilityStatus")
                    };

                    // Parse prepaid units
                    if (sku.TryGetProperty("prepaidUnits", out var prepaid))
                    {
                        subscription.PrepaidUnits = GetInt(prepaid, "enabled");
                        subscription.SuspendedUnits = GetInt(prepaid, "suspended");
                        subscription.WarningUnits = GetInt(prepaid, "warning");
                    }

                    subscription.ConsumedUnits = GetInt(sku, "consumedUnits");
                    subscription.AvailableUnits = subscription.PrepaidUnits - subscription.ConsumedUnits;

                    // Parse service plans
                    if (sku.TryGetProperty("servicePlans", out var plans) && plans.ValueKind == JsonValueKind.Array)
                    {
                        var planList = new List<object>();
                        foreach (var plan in plans.EnumerateArray())
                        {
                            planList.Add(new
                            {
                                servicePlanId = GetString(plan, "servicePlanId"),
                                servicePlanName = GetString(plan, "servicePlanName"),
                                provisioningStatus = GetString(plan, "provisioningStatus"),
                                appliesTo = GetString(plan, "appliesTo")
                            });
                        }
                        subscription.ServicePlansJson = ToJson(planList);
                    }

                    // Set display name based on SKU part number
                    subscription.DisplayName = GetLicenseDisplayName(subscription.SkuPartNumber);

                    // Detect trial
                    subscription.IsTrial = subscription.CapabilityStatus == "Trial" ||
                                          subscription.SkuPartNumber.Contains("TRIAL", StringComparison.OrdinalIgnoreCase);

                    // License categorization
                    subscription.LicenseCategory = LicenseSkuMapper.GetBestCategory(
                        subscription.SkuPartNumber,
                        subscription.SkuId);
                    subscription.IsPrimaryLicense = subscription.LicenseCategory.IsPrimaryLicense();
                    subscription.TierGroup = subscription.LicenseCategory.GetTierGroup();
                    subscription.EstimatedMonthlyPricePerUser = LicenseSkuMapper.GetEstimatedMonthlyPrice(subscription.LicenseCategory);
                    subscription.IncludedFeaturesJson = ToJson(LicenseSkuMapper.GetIncludedFeatures(subscription.LicenseCategory));

                    subscriptions.Add(subscription);
                }
            }

            await DbContext.LicenseSubscriptions.AddRangeAsync(subscriptions, ct);
            return subscriptions.Count;
        }
        catch (Exception ex)
        {
            warnings.Add($"Error collecting subscribed SKUs: {ex.Message}");
            return 0;
        }
    }

    private async Task<int> CollectServiceHealthAsync(
        IGraphClientWrapper graphClient,
        Guid tenantId,
        Guid snapshotId,
        List<string> warnings,
        CancellationToken ct)
    {
        try
        {
            // Note: Service health requires ServiceHealth.Read.All permission
            var response = await graphClient.GetRawJsonAsync(
                "admin/serviceAnnouncement/healthOverviews?$expand=issues($filter=isResolved eq false)", ct);

            if (string.IsNullOrEmpty(response)) return 0;

            var doc = JsonDocument.Parse(response);
            var activeIssues = 0;

            // Update TenantInfo with service health
            var tenantInfo = DbContext.TenantInfos.Local.FirstOrDefault(t => t.TenantId == tenantId && t.SnapshotId == snapshotId);
            if (tenantInfo != null && doc.RootElement.TryGetProperty("value", out var services))
            {
                var healthSummary = new List<object>();
                foreach (var service in services.EnumerateArray())
                {
                    var serviceName = GetString(service, "service");
                    var status = GetString(service, "status");

                    if (service.TryGetProperty("issues", out var issues) && issues.ValueKind == JsonValueKind.Array)
                    {
                        activeIssues += issues.GetArrayLength();
                    }

                    healthSummary.Add(new
                    {
                        service = serviceName,
                        status,
                        issueCount = service.TryGetProperty("issues", out var iss) ? iss.GetArrayLength() : 0
                    });
                }

                tenantInfo.ServiceHealthJson = ToJson(healthSummary);
                tenantInfo.ActiveServiceIssues = activeIssues;
            }

            return activeIssues;
        }
        catch (Exception ex)
        {
            warnings.Add($"Error collecting service health: {ex.Message}");
            return 0;
        }
    }

    private static string GetLicenseDisplayName(string skuPartNumber)
    {
        var licenseNames = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase)
        {
            { "SPE_E5", "Microsoft 365 E5" },
            { "SPE_E3", "Microsoft 365 E3" },
            { "SPE_F1", "Microsoft 365 F1" },
            { "ENTERPRISEPACK", "Office 365 E3" },
            { "ENTERPRISEPREMIUM", "Office 365 E5" },
            { "EXCHANGEENTERPRISE", "Exchange Online (Plan 2)" },
            { "EXCHANGESTANDARD", "Exchange Online (Plan 1)" },
            { "TEAMS_EXPLORATORY", "Microsoft Teams Exploratory" },
            { "POWER_BI_STANDARD", "Power BI (free)" },
            { "POWER_BI_PRO", "Power BI Pro" },
            { "PROJECTPREMIUM", "Project Plan 5" },
            { "PROJECTPROFESSIONAL", "Project Plan 3" },
            { "VISIOCLIENT", "Visio Plan 2" },
            { "FLOW_FREE", "Power Automate Free" },
            { "POWERAPPS_VIRAL", "Power Apps Plan 2 Trial" },
            { "AAD_PREMIUM", "Azure AD Premium P1" },
            { "AAD_PREMIUM_P2", "Azure AD Premium P2" },
            { "EMS", "Enterprise Mobility + Security E3" },
            { "EMSPREMIUM", "Enterprise Mobility + Security E5" },
            { "ATP_ENTERPRISE", "Microsoft Defender for Office 365 (Plan 1)" },
            { "THREAT_INTELLIGENCE", "Microsoft Defender for Office 365 (Plan 2)" },
            { "WIN_DEF_ATP", "Microsoft Defender for Endpoint" },
            { "IDENTITY_THREAT_PROTECTION", "Microsoft 365 E5 Security" },
            { "INFORMATION_PROTECTION_COMPLIANCE", "Microsoft 365 E5 Compliance" },
            { "M365_F1", "Microsoft 365 F1" },
            { "M365_F3", "Microsoft 365 F3" },
            { "MICROSOFT_BUSINESS_CENTER", "Microsoft Business Center" },
            { "O365_BUSINESS_ESSENTIALS", "Microsoft 365 Business Basic" },
            { "O365_BUSINESS_PREMIUM", "Microsoft 365 Business Standard" },
            { "SMB_BUSINESS_PREMIUM", "Microsoft 365 Business Premium" },
        };

        return licenseNames.TryGetValue(skuPartNumber, out var name) ? name : skuPartNumber;
    }
}
