using System.Text.Json;
using Cloudativ.Assessment.Application.DTOs;
using Cloudativ.Assessment.Application.Interfaces;
using Cloudativ.Assessment.Domain.Entities;
using Cloudativ.Assessment.Domain.Enums;
using Cloudativ.Assessment.Domain.Interfaces;
using Microsoft.Extensions.Logging;

namespace Cloudativ.Assessment.Infrastructure.Services;

public class DashboardService : IDashboardService
{
    private readonly IUnitOfWork _unitOfWork;
    private readonly ILogger<DashboardService> _logger;

    public DashboardService(IUnitOfWork unitOfWork, ILogger<DashboardService> logger)
    {
        _unitOfWork = unitOfWork;
        _logger = logger;
    }

    public async Task<DashboardSummaryDto> GetSummaryAsync(CancellationToken cancellationToken = default)
    {
        var tenants = await _unitOfWork.Tenants.GetAllAsync(cancellationToken);
        var totalTenants = tenants.Count;
        var activeTenants = tenants.Count(t => t.OnboardingStatus == OnboardingStatus.Active ||
                                               t.OnboardingStatus == OnboardingStatus.Validated);
        var pendingOnboarding = tenants.Count(t => t.OnboardingStatus == OnboardingStatus.Pending ||
                                                   t.OnboardingStatus == OnboardingStatus.InProgress);

        var runs = await _unitOfWork.AssessmentRuns.GetAllAsync(cancellationToken);
        var completedRuns = runs.Where(r => r.Status == AssessmentStatus.Completed).ToList();
        var thisMonth = DateTime.UtcNow.AddMonths(-1);
        var runsThisMonth = completedRuns.Count(r => r.CompletedAt >= thisMonth);

        var averageScore = completedRuns.Any() && completedRuns.Any(r => r.OverallScore.HasValue)
            ? completedRuns.Where(r => r.OverallScore.HasValue).Average(r => r.OverallScore!.Value)
            : 0;

        // Get findings counts
        var criticalFindings = 0;
        var highFindings = 0;

        foreach (var run in completedRuns.OrderByDescending(r => r.CompletedAt).Take(10))
        {
            var findings = await _unitOfWork.Findings.GetByRunIdAsync(run.Id, cancellationToken);
            criticalFindings += findings.Count(f => f.Severity == Severity.Critical && !f.IsCompliant);
            highFindings += findings.Count(f => f.Severity == Severity.High && !f.IsCompliant);
        }

        // Get tenant scores
        var tenantScores = new List<TenantScoreDto>();
        foreach (var tenant in tenants)
        {
            var latestRun = await _unitOfWork.AssessmentRuns.GetLatestByTenantAsync(tenant.Id, cancellationToken);
            if (latestRun?.OverallScore.HasValue == true)
            {
                tenantScores.Add(new TenantScoreDto
                {
                    TenantId = tenant.Id,
                    TenantName = tenant.Name,
                    Score = latestRun.OverallScore.Value,
                    Grade = GetGrade(latestRun.OverallScore.Value),
                    LastAssessment = latestRun.CompletedAt
                });
            }
        }

        // Recent assessments
        var recentAssessments = completedRuns
            .OrderByDescending(r => r.CompletedAt)
            .Take(10)
            .Select(r =>
            {
                var tenant = tenants.FirstOrDefault(t => t.Id == r.TenantId);
                return new RecentAssessmentDto
                {
                    RunId = r.Id,
                    TenantId = r.TenantId,
                    TenantName = tenant?.Name ?? "Unknown",
                    CompletedAt = r.CompletedAt ?? r.StartedAt,
                    Score = r.OverallScore ?? 0,
                    Status = r.Status
                };
            })
            .ToList();

        return new DashboardSummaryDto
        {
            TotalTenants = totalTenants,
            ActiveTenants = activeTenants,
            PendingOnboarding = pendingOnboarding,
            TotalAssessments = runs.Count,
            AssessmentsThisMonth = runsThisMonth,
            AverageScore = averageScore,
            CriticalFindingsCount = criticalFindings,
            HighFindingsCount = highFindings,
            TopTenants = tenantScores.OrderByDescending(t => t.Score).Take(5).ToList(),
            LowestTenants = tenantScores.OrderBy(t => t.Score).Take(5).ToList(),
            RecentAssessments = recentAssessments
        };
    }

    public async Task<TenantDashboardDto> GetTenantDashboardAsync(Guid tenantId, CancellationToken cancellationToken = default)
    {
        var tenant = await _unitOfWork.Tenants.GetByIdAsync(tenantId, cancellationToken);
        if (tenant == null)
            throw new InvalidOperationException($"Tenant with ID '{tenantId}' not found");

        var runs = await _unitOfWork.AssessmentRuns.GetByTenantAsync(tenantId, 20, cancellationToken);
        var latestRun = runs.FirstOrDefault();
        var totalRuns = await _unitOfWork.AssessmentRuns.CountAsync(r => r.TenantId == tenantId, cancellationToken);

        var tenantDto = new TenantDto
        {
            Id = tenant.Id,
            Name = tenant.Name,
            Domain = tenant.Domain,
            AzureTenantId = tenant.AzureTenantId,
            OnboardingStatus = tenant.OnboardingStatus,
            Industry = tenant.Industry,
            ContactEmail = tenant.ContactEmail,
            CreatedAt = tenant.CreatedAt,
            LastAssessmentScore = latestRun?.OverallScore,
            LastAssessmentDate = latestRun?.CompletedAt ?? latestRun?.StartedAt,
            TotalAssessments = totalRuns
        };

        var recentAssessments = runs.Select(r => new AssessmentRunDto
        {
            Id = r.Id,
            TenantId = r.TenantId,
            TenantName = tenant.Name,
            StartedAt = r.StartedAt,
            CompletedAt = r.CompletedAt,
            Status = r.Status,
            InitiatedBy = r.InitiatedBy,
            OverallScore = r.OverallScore,
            DomainScores = !string.IsNullOrEmpty(r.SummaryScoresJson)
                ? JsonSerializer.Deserialize<Dictionary<string, DomainScoreSummary>>(r.SummaryScoresJson) ?? new()
                : new()
        }).ToList();

        var scoreTrend = runs
            .Where(r => r.OverallScore.HasValue && r.CompletedAt.HasValue)
            .OrderBy(r => r.CompletedAt)
            .Select(r => new ScoreTrendPoint
            {
                Date = r.CompletedAt!.Value,
                Score = r.OverallScore!.Value,
                Label = r.CompletedAt!.Value.ToString("MMM dd")
            })
            .ToList();

        var findingsSummary = new FindingsSummaryDto();
        var domainBreakdown = new Dictionary<AssessmentDomain, DomainScoreSummary>();

        if (latestRun != null)
        {
            var findings = await _unitOfWork.Findings.GetByRunIdAsync(latestRun.Id, cancellationToken);

            findingsSummary = new FindingsSummaryDto
            {
                TotalFindings = findings.Count,
                CriticalCount = findings.Count(f => f.Severity == Severity.Critical && !f.IsCompliant),
                HighCount = findings.Count(f => f.Severity == Severity.High && !f.IsCompliant),
                MediumCount = findings.Count(f => f.Severity == Severity.Medium && !f.IsCompliant),
                LowCount = findings.Count(f => f.Severity == Severity.Low && !f.IsCompliant),
                InformationalCount = findings.Count(f => f.Severity == Severity.Informational),
                CompliantCount = findings.Count(f => f.IsCompliant),
                NonCompliantCount = findings.Count(f => !f.IsCompliant),
                FindingsByDomain = findings.GroupBy(f => f.Domain)
                    .ToDictionary(g => g.Key, g => g.Count()),
                TopCriticalFindings = findings
                    .Where(f => (f.Severity == Severity.Critical || f.Severity == Severity.High) && !f.IsCompliant)
                    .OrderBy(f => f.Severity)
                    .Take(10)
                    .Select(f => new FindingDto
                    {
                        Id = f.Id,
                        AssessmentRunId = f.AssessmentRunId,
                        Domain = f.Domain,
                        DomainDisplayName = f.Domain.GetDisplayName(),
                        Severity = f.Severity,
                        Title = f.Title,
                        Description = f.Description,
                        Category = f.Category,
                        IsCompliant = f.IsCompliant
                    })
                    .ToList()
            };

            if (!string.IsNullOrEmpty(latestRun.SummaryScoresJson))
            {
                var scores = JsonSerializer.Deserialize<Dictionary<string, DomainScoreSummary>>(latestRun.SummaryScoresJson);
                if (scores != null)
                {
                    foreach (var kvp in scores)
                    {
                        if (Enum.TryParse<AssessmentDomain>(kvp.Key, out var domain))
                        {
                            domainBreakdown[domain] = kvp.Value;
                        }
                    }
                }
            }
        }

        return new TenantDashboardDto
        {
            Tenant = tenantDto,
            LatestAssessment = latestRun != null ? recentAssessments.FirstOrDefault() : null,
            RecentAssessments = recentAssessments,
            ScoreTrend = scoreTrend,
            FindingsSummary = findingsSummary,
            DomainBreakdown = domainBreakdown
        };
    }

    public async Task<List<ScoreTrendPoint>> GetScoreTrendAsync(Guid tenantId, int monthsBack = 12, CancellationToken cancellationToken = default)
    {
        var cutoff = DateTime.UtcNow.AddMonths(-monthsBack);
        var runs = await _unitOfWork.AssessmentRuns.GetByTenantAsync(tenantId, 100, cancellationToken);

        return runs
            .Where(r => r.OverallScore.HasValue && r.CompletedAt.HasValue && r.CompletedAt >= cutoff)
            .OrderBy(r => r.CompletedAt)
            .Select(r => new ScoreTrendPoint
            {
                Date = r.CompletedAt!.Value,
                Score = r.OverallScore!.Value,
                Label = r.CompletedAt!.Value.ToString("MMM dd")
            })
            .ToList();
    }

    public async Task<FindingsSummaryDto> GetFindingsSummaryAsync(Guid runId, CancellationToken cancellationToken = default)
    {
        var findings = await _unitOfWork.Findings.GetByRunIdAsync(runId, cancellationToken);

        return new FindingsSummaryDto
        {
            TotalFindings = findings.Count,
            CriticalCount = findings.Count(f => f.Severity == Severity.Critical && !f.IsCompliant),
            HighCount = findings.Count(f => f.Severity == Severity.High && !f.IsCompliant),
            MediumCount = findings.Count(f => f.Severity == Severity.Medium && !f.IsCompliant),
            LowCount = findings.Count(f => f.Severity == Severity.Low && !f.IsCompliant),
            InformationalCount = findings.Count(f => f.Severity == Severity.Informational),
            CompliantCount = findings.Count(f => f.IsCompliant),
            NonCompliantCount = findings.Count(f => !f.IsCompliant),
            FindingsByDomain = findings.GroupBy(f => f.Domain)
                .ToDictionary(g => g.Key, g => g.Count())
        };
    }

    private static string GetGrade(int score) => score switch
    {
        >= 90 => "A",
        >= 80 => "B",
        >= 70 => "C",
        >= 60 => "D",
        _ => "F"
    };

    public async Task<ResourceStatisticsDto?> GetResourceStatisticsAsync(Guid tenantId, CancellationToken cancellationToken = default)
    {
        var tenant = await _unitOfWork.Tenants.GetByIdAsync(tenantId, cancellationToken);
        if (tenant == null)
            return null;

        var latestRun = await _unitOfWork.AssessmentRuns.GetLatestByTenantAsync(tenantId, cancellationToken);
        if (latestRun == null)
        {
            return new ResourceStatisticsDto
            {
                TenantId = tenantId,
                TenantName = tenant.Name
            };
        }

        // Get raw snapshots from the latest assessment
        var snapshots = await _unitOfWork.RawSnapshots.FindAsync(
            s => s.AssessmentRunId == latestRun.Id,
            cancellationToken);

        var stats = new ResourceStatisticsDto
        {
            TenantId = tenantId,
            TenantName = tenant.Name,
            LastAssessmentDate = latestRun.CompletedAt ?? latestRun.StartedAt
        };

        // Parse metrics from each domain's raw data
        foreach (var snapshot in snapshots)
        {
            stats = ExtractMetricsFromSnapshot(stats, snapshot);
        }

        return stats;
    }

    public async Task<ResourceStatisticsDto> GetAggregatedResourceStatisticsAsync(CancellationToken cancellationToken = default)
    {
        var tenants = await _unitOfWork.Tenants.GetAllAsync(cancellationToken);
        var activeTenants = tenants.Where(t => t.OnboardingStatus == OnboardingStatus.Active ||
                                                t.OnboardingStatus == OnboardingStatus.Validated).ToList();

        var aggregated = new ResourceStatisticsDto
        {
            TenantName = "All Tenants"
        };

        int totalUsers = 0, enabledUsers = 0, guestUsers = 0, adminUsers = 0, globalAdmins = 0, riskyUsers = 0;
        int totalGroups = 0, securityGroups = 0, m365Groups = 0;
        int enterpriseApps = 0, appRegistrations = 0, highRiskApps = 0;
        int totalDevices = 0, managedDevices = 0, compliantDevices = 0;
        int caPolicies = 0, enabledCaPolicies = 0, dlpPolicies = 0;
        int mailboxes = 0, sharedMailboxes = 0;
        int spSites = 0, teams = 0;
        int totalLicenses = 0, assignedLicenses = 0;
        DateTime? lastAssessmentDate = null;

        foreach (var tenant in activeTenants)
        {
            var tenantStats = await GetResourceStatisticsAsync(tenant.Id, cancellationToken);
            if (tenantStats != null)
            {
                totalUsers += tenantStats.TotalUsers;
                enabledUsers += tenantStats.EnabledUsers;
                guestUsers += tenantStats.GuestUsers;
                adminUsers += tenantStats.AdminUsers;
                globalAdmins += tenantStats.GlobalAdmins;
                riskyUsers += tenantStats.RiskyUsers;
                totalGroups += tenantStats.TotalGroups;
                securityGroups += tenantStats.SecurityGroups;
                m365Groups += tenantStats.Microsoft365Groups;
                enterpriseApps += tenantStats.EnterpriseApps;
                appRegistrations += tenantStats.AppRegistrations;
                highRiskApps += tenantStats.HighRiskApps;
                totalDevices += tenantStats.TotalDevices;
                managedDevices += tenantStats.ManagedDevices;
                compliantDevices += tenantStats.CompliantDevices;
                caPolicies += tenantStats.ConditionalAccessPolicies;
                enabledCaPolicies += tenantStats.EnabledCaPolicies;
                dlpPolicies += tenantStats.DlpPolicies;
                mailboxes += tenantStats.Mailboxes;
                sharedMailboxes += tenantStats.SharedMailboxes;
                spSites += tenantStats.SharePointSites;
                teams += tenantStats.TeamsCount;
                totalLicenses += tenantStats.TotalLicenses;
                assignedLicenses += tenantStats.AssignedLicenses;

                if (tenantStats.LastAssessmentDate.HasValue &&
                    (!lastAssessmentDate.HasValue || tenantStats.LastAssessmentDate > lastAssessmentDate))
                {
                    lastAssessmentDate = tenantStats.LastAssessmentDate;
                }
            }
        }

        return new ResourceStatisticsDto
        {
            TenantName = "All Tenants",
            LastAssessmentDate = lastAssessmentDate,
            TotalUsers = totalUsers,
            EnabledUsers = enabledUsers,
            GuestUsers = guestUsers,
            AdminUsers = adminUsers,
            GlobalAdmins = globalAdmins,
            RiskyUsers = riskyUsers,
            TotalGroups = totalGroups,
            SecurityGroups = securityGroups,
            Microsoft365Groups = m365Groups,
            EnterpriseApps = enterpriseApps,
            AppRegistrations = appRegistrations,
            HighRiskApps = highRiskApps,
            TotalDevices = totalDevices,
            ManagedDevices = managedDevices,
            CompliantDevices = compliantDevices,
            ConditionalAccessPolicies = caPolicies,
            EnabledCaPolicies = enabledCaPolicies,
            DlpPolicies = dlpPolicies,
            Mailboxes = mailboxes,
            SharedMailboxes = sharedMailboxes,
            SharePointSites = spSites,
            TeamsCount = teams,
            TotalLicenses = totalLicenses,
            AssignedLicenses = assignedLicenses
        };
    }

    private ResourceStatisticsDto ExtractMetricsFromSnapshot(ResourceStatisticsDto stats, RawSnapshot snapshot)
    {
        if (string.IsNullOrEmpty(snapshot.PayloadJson))
            return stats;

        try
        {
            // The PayloadJson contains a dictionary where keys are data types (users, directoryRoles, etc.)
            // and values are the JSON strings from the Graph API
            using var doc = JsonDocument.Parse(snapshot.PayloadJson);
            var root = doc.RootElement;

            // Iterate through the dictionary properties
            foreach (var property in root.EnumerateObject())
            {
                var dataType = property.Name.ToLowerInvariant();
                var valueJson = property.Value.GetString();

                if (string.IsNullOrEmpty(valueJson))
                    continue;

                try
                {
                    using var dataDoc = JsonDocument.Parse(valueJson);
                    var dataRoot = dataDoc.RootElement;

                    stats = ExtractFromDataType(stats, dataType, dataRoot);
                }
                catch (Exception ex)
                {
                    _logger.LogDebug(ex, "Failed to parse data for {DataType}", dataType);
                }
            }
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Failed to extract metrics from snapshot for domain {Domain}", snapshot.Domain);
        }

        return stats;
    }

    private ResourceStatisticsDto ExtractFromDataType(ResourceStatisticsDto stats, string dataType, JsonElement dataRoot)
    {
        switch (dataType)
        {
            case "users":
                var userCount = GetArrayCount(dataRoot);
                var enabledUsers = CountByProperty(dataRoot, "accountEnabled", true);
                var guestUsers = CountByStringProperty(dataRoot, "userType", "Guest");
                stats = stats with
                {
                    TotalUsers = userCount,
                    EnabledUsers = enabledUsers > 0 ? enabledUsers : userCount,
                    GuestUsers = guestUsers
                };
                break;

            case "directoryroles":
                var adminCount = CountTotalMembers(dataRoot);
                var globalAdminCount = CountGlobalAdmins(dataRoot);
                stats = stats with
                {
                    AdminUsers = adminCount,
                    GlobalAdmins = globalAdminCount
                };
                break;

            case "riskyusers":
                stats = stats with { RiskyUsers = GetArrayCount(dataRoot) };
                break;

            case "groups":
                var groupCount = GetArrayCount(dataRoot);
                stats = stats with
                {
                    TotalGroups = groupCount,
                    Microsoft365Groups = CountByArrayContains(dataRoot, "groupTypes", "Unified")
                };
                break;

            case "enterpriseapps":
            case "serviceprincipals":
                stats = stats with { EnterpriseApps = GetArrayCount(dataRoot) };
                break;

            case "appregistrations":
            case "applications":
                stats = stats with { AppRegistrations = GetArrayCount(dataRoot) };
                break;

            case "conditionalaccesspolicies":
                var caCount = GetArrayCount(dataRoot);
                var enabledCount = CountByStringProperty(dataRoot, "state", "enabled");
                stats = stats with
                {
                    ConditionalAccessPolicies = caCount,
                    EnabledCaPolicies = enabledCount
                };
                break;

            case "manageddevices":
            case "devices":
                var deviceCount = GetArrayCount(dataRoot);
                var managed = CountByProperty(dataRoot, "isManaged", true);
                var compliant = CountByProperty(dataRoot, "isCompliant", true);
                stats = stats with
                {
                    TotalDevices = deviceCount,
                    ManagedDevices = managed > 0 ? managed : deviceCount,
                    CompliantDevices = compliant
                };
                break;

            case "mailboxes":
                stats = stats with { Mailboxes = GetArrayCount(dataRoot) };
                break;

            case "sharedmailboxes":
                stats = stats with { SharedMailboxes = GetArrayCount(dataRoot) };
                break;

            case "sharepointsites":
            case "sites":
                stats = stats with { SharePointSites = GetArrayCount(dataRoot) };
                break;

            case "teams":
                stats = stats with { TeamsCount = GetArrayCount(dataRoot) };
                break;

            case "dlppolicies":
            case "informationprotectionpolicies":
                stats = stats with { DlpPolicies = GetArrayCount(dataRoot) };
                break;

            case "subscribedskus":
            case "licenses":
                var licenses = CalculateLicenseCounts(dataRoot);
                stats = stats with
                {
                    TotalLicenses = licenses.total,
                    AssignedLicenses = licenses.assigned
                };
                break;
        }

        return stats;
    }

    private static int GetArrayCount(JsonElement root)
    {
        if (root.TryGetProperty("value", out var valueArray) && valueArray.ValueKind == JsonValueKind.Array)
            return valueArray.GetArrayLength();
        if (root.ValueKind == JsonValueKind.Array)
            return root.GetArrayLength();
        return 0;
    }

    private static int CountByProperty(JsonElement root, string propertyName, bool expectedValue)
    {
        var count = 0;
        JsonElement array = root;

        if (root.TryGetProperty("value", out var valueArray))
            array = valueArray;

        if (array.ValueKind != JsonValueKind.Array)
            return 0;

        foreach (var item in array.EnumerateArray())
        {
            if (item.TryGetProperty(propertyName, out var prop) &&
                prop.ValueKind == JsonValueKind.True == expectedValue)
            {
                count++;
            }
        }
        return count;
    }

    private static int CountByStringProperty(JsonElement root, string propertyName, string expectedValue)
    {
        var count = 0;
        JsonElement array = root;

        if (root.TryGetProperty("value", out var valueArray))
            array = valueArray;

        if (array.ValueKind != JsonValueKind.Array)
            return 0;

        foreach (var item in array.EnumerateArray())
        {
            if (item.TryGetProperty(propertyName, out var prop) &&
                prop.ValueKind == JsonValueKind.String &&
                string.Equals(prop.GetString(), expectedValue, StringComparison.OrdinalIgnoreCase))
            {
                count++;
            }
        }
        return count;
    }

    private static int CountByArrayContains(JsonElement root, string propertyName, string containsValue)
    {
        var count = 0;
        JsonElement array = root;

        if (root.TryGetProperty("value", out var valueArray))
            array = valueArray;

        if (array.ValueKind != JsonValueKind.Array)
            return 0;

        foreach (var item in array.EnumerateArray())
        {
            if (item.TryGetProperty(propertyName, out var prop) && prop.ValueKind == JsonValueKind.Array)
            {
                foreach (var element in prop.EnumerateArray())
                {
                    if (element.ValueKind == JsonValueKind.String &&
                        string.Equals(element.GetString(), containsValue, StringComparison.OrdinalIgnoreCase))
                    {
                        count++;
                        break;
                    }
                }
            }
        }
        return count;
    }

    private static int CountTotalMembers(JsonElement root)
    {
        var count = 0;
        JsonElement array = root;

        if (root.TryGetProperty("value", out var valueArray))
            array = valueArray;

        if (array.ValueKind != JsonValueKind.Array)
            return 0;

        var seenIds = new HashSet<string>();

        foreach (var role in array.EnumerateArray())
        {
            if (role.TryGetProperty("members", out var members) && members.ValueKind == JsonValueKind.Array)
            {
                foreach (var member in members.EnumerateArray())
                {
                    if (member.TryGetProperty("id", out var id) && id.ValueKind == JsonValueKind.String)
                    {
                        var idStr = id.GetString();
                        if (!string.IsNullOrEmpty(idStr) && seenIds.Add(idStr))
                        {
                            count++;
                        }
                    }
                }
            }
        }
        return count;
    }

    private static int CountGlobalAdmins(JsonElement root)
    {
        JsonElement array = root;

        if (root.TryGetProperty("value", out var valueArray))
            array = valueArray;

        if (array.ValueKind != JsonValueKind.Array)
            return 0;

        foreach (var role in array.EnumerateArray())
        {
            if (role.TryGetProperty("displayName", out var displayName) &&
                displayName.ValueKind == JsonValueKind.String &&
                displayName.GetString()?.Contains("Global Administrator", StringComparison.OrdinalIgnoreCase) == true)
            {
                if (role.TryGetProperty("members", out var members) && members.ValueKind == JsonValueKind.Array)
                {
                    return members.GetArrayLength();
                }
            }
        }
        return 0;
    }

    private static (int total, int assigned) CalculateLicenseCounts(JsonElement root)
    {
        int total = 0, assigned = 0;
        JsonElement array = root;

        if (root.TryGetProperty("value", out var valueArray))
            array = valueArray;

        if (array.ValueKind != JsonValueKind.Array)
            return (0, 0);

        foreach (var sku in array.EnumerateArray())
        {
            if (sku.TryGetProperty("prepaidUnits", out var prepaid))
            {
                if (prepaid.TryGetProperty("enabled", out var enabled) &&
                    enabled.ValueKind == JsonValueKind.Number)
                {
                    total += enabled.GetInt32();
                }
            }
            if (sku.TryGetProperty("consumedUnits", out var consumed) &&
                consumed.ValueKind == JsonValueKind.Number)
            {
                assigned += consumed.GetInt32();
            }
        }
        return (total, assigned);
    }
}
