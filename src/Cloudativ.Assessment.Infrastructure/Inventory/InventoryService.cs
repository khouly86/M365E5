using System.Text.Json;
using Cloudativ.Assessment.Application.DTOs;
using Cloudativ.Assessment.Application.Interfaces;
using Cloudativ.Assessment.Domain.Entities.Inventory;
using Cloudativ.Assessment.Domain.Enums;
using Cloudativ.Assessment.Infrastructure.Data;
using Cloudativ.Assessment.Infrastructure.Services;
using Hangfire;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace Cloudativ.Assessment.Infrastructure.Inventory;

/// <summary>
/// Service implementation for inventory management.
/// </summary>
public class InventoryService : IInventoryService
{
    private readonly ApplicationDbContext _dbContext;
    private readonly IInventoryEngine _inventoryEngine;
    private readonly IBackgroundJobClient _backgroundJobs;
    private readonly ILogger<InventoryService> _logger;

    public InventoryService(
        ApplicationDbContext dbContext,
        IInventoryEngine inventoryEngine,
        IBackgroundJobClient backgroundJobs,
        ILogger<InventoryService> logger)
    {
        _dbContext = dbContext;
        _inventoryEngine = inventoryEngine;
        _backgroundJobs = backgroundJobs;
        _logger = logger;
    }

    #region Collection

    public async Task<Guid> StartInventoryCollectionAsync(StartInventoryRequest request, CancellationToken ct = default)
    {
        var snapshotId = await _inventoryEngine.StartCollectionAsync(
            request.TenantId,
            request.Domains,
            request.InitiatedBy,
            ct);

        // Queue background job
        _backgroundJobs.Enqueue<IInventoryEngine>(
            engine => engine.ExecuteCollectionAsync(snapshotId, CancellationToken.None));

        _logger.LogInformation(
            "Queued inventory collection job for tenant {TenantId}, snapshot {SnapshotId}",
            request.TenantId, snapshotId);

        return snapshotId;
    }

    public async Task<InventoryProgressDto> GetProgressAsync(Guid snapshotId, CancellationToken ct = default)
    {
        var progress = await _inventoryEngine.GetProgressAsync(snapshotId, ct);

        return new InventoryProgressDto
        {
            SnapshotId = progress.SnapshotId,
            Status = progress.Status,
            ProgressPercentage = progress.ProgressPercentage,
            CurrentDomain = progress.CurrentDomain,
            CurrentDomainName = progress.CurrentDomain?.GetDisplayName(),
            CompletedDomains = progress.CompletedDomains,
            PendingDomains = progress.PendingDomains,
            FailedDomains = progress.FailedDomains,
            TotalItemsCollected = progress.TotalItemsCollected,
            StartedAt = progress.StartedAt,
            CompletedAt = progress.CompletedAt,
            Errors = progress.Errors
        };
    }

    public async Task CancelInventoryCollectionAsync(Guid snapshotId, CancellationToken ct = default)
    {
        await _inventoryEngine.CancelCollectionAsync(snapshotId, ct);
    }

    #endregion

    #region Snapshot Queries

    public async Task<IReadOnlyList<InventorySnapshotDto>> GetSnapshotsAsync(
        Guid tenantId,
        int take = 20,
        CancellationToken ct = default)
    {
        var snapshots = await _dbContext.InventorySnapshots
            .Include(s => s.Tenant)
            .Where(s => s.TenantId == tenantId)
            .OrderByDescending(s => s.CollectedAt)
            .Take(take)
            .ToListAsync(ct);

        return snapshots.Select(MapToSnapshotDto).ToList();
    }

    public async Task<InventorySnapshotDto?> GetSnapshotAsync(Guid snapshotId, CancellationToken ct = default)
    {
        var snapshot = await _dbContext.InventorySnapshots
            .Include(s => s.Tenant)
            .FirstOrDefaultAsync(s => s.Id == snapshotId, ct);

        return snapshot == null ? null : MapToSnapshotDto(snapshot);
    }

    public async Task<InventorySnapshotDto?> GetLatestSnapshotAsync(
        Guid tenantId,
        InventoryDomain? domain = null,
        CancellationToken ct = default)
    {
        var query = _dbContext.InventorySnapshots
            .Include(s => s.Tenant)
            .Where(s => s.TenantId == tenantId && s.Status == InventoryStatus.Completed);

        if (domain.HasValue)
        {
            query = query.Where(s => s.Domain == domain.Value);
        }

        var snapshot = await query
            .OrderByDescending(s => s.CollectedAt)
            .FirstOrDefaultAsync(ct);

        return snapshot == null ? null : MapToSnapshotDto(snapshot);
    }

    #endregion

    #region Dashboard

    public async Task<InventoryDashboardDto> GetDashboardAsync(Guid tenantId, CancellationToken ct = default)
    {
        var tenant = await _dbContext.Tenants.FindAsync(new object[] { tenantId }, ct);
        if (tenant == null)
        {
            throw new InvalidOperationException($"Tenant {tenantId} not found");
        }

        // Get latest snapshots per domain
        var latestSnapshots = await _dbContext.InventorySnapshots
            .Where(s => s.TenantId == tenantId && s.Status == InventoryStatus.Completed)
            .GroupBy(s => s.Domain)
            .Select(g => g.OrderByDescending(s => s.CollectedAt).First())
            .ToListAsync(ct);

        var latestSnapshotIds = latestSnapshots.Select(s => s.Id).ToList();

        // Get user stats
        var userStats = await _dbContext.UserInventories
            .Where(u => latestSnapshotIds.Contains(u.SnapshotId))
            .GroupBy(u => 1)
            .Select(g => new
            {
                Total = g.Count(),
                Enabled = g.Count(u => u.AccountEnabled),
                Guests = g.Count(u => u.UserType == "Guest"),
                Admins = g.Count(u => u.IsPrivileged),
                Risky = g.Count(u => u.RiskLevel != null && u.RiskLevel != "none")
            })
            .FirstOrDefaultAsync(ct);

        // Get device stats
        var deviceStats = await _dbContext.DeviceInventories
            .Where(d => latestSnapshotIds.Contains(d.SnapshotId))
            .GroupBy(d => 1)
            .Select(g => new
            {
                Total = g.Count(),
                Compliant = g.Count(d => d.ComplianceState == "compliant"),
                Managed = g.Count(d => d.IsManaged)
            })
            .FirstOrDefaultAsync(ct);

        // Get app stats
        var appStats = await _dbContext.EnterpriseAppInventories
            .Where(a => latestSnapshotIds.Contains(a.SnapshotId))
            .GroupBy(a => 1)
            .Select(g => new
            {
                Total = g.Count(),
                HighRisk = g.Count(a => a.HasHighPrivilegePermissions)
            })
            .FirstOrDefaultAsync(ct);

        // Get high-risk findings
        var highRiskFindings = await _dbContext.HighRiskFindingInventories
            .Where(f => latestSnapshotIds.Contains(f.SnapshotId))
            .OrderByDescending(f => f.SeverityOrder)
            .ThenByDescending(f => f.AffectedCount)
            .Take(10)
            .ToListAsync(ct);

        // Get secure score
        var secureScore = await _dbContext.SecureScoreInventories
            .Where(s => latestSnapshotIds.Contains(s.SnapshotId))
            .FirstOrDefaultAsync(ct);

        // Get E5 utilization
        var e5Util = await _dbContext.LicenseUtilizationInventories
            .Where(l => latestSnapshotIds.Contains(l.SnapshotId))
            .FirstOrDefaultAsync(ct);

        // Build domain status list
        var domainStatuses = new List<DomainStatusDto>();
        foreach (InventoryDomain domain in Enum.GetValues<InventoryDomain>())
        {
            var snapshot = latestSnapshots.FirstOrDefault(s => s.Domain == domain);
            domainStatuses.Add(new DomainStatusDto
            {
                Domain = domain,
                DisplayName = domain.GetDisplayName(),
                LastCollected = snapshot?.CollectedAt,
                Status = snapshot?.Status,
                ItemCount = snapshot?.ItemCount ?? 0,
                ErrorMessage = snapshot?.ErrorMessage
            });
        }

        return new InventoryDashboardDto
        {
            TenantId = tenantId,
            TenantName = tenant.Name,
            LastCollectionDate = latestSnapshots.Any() ? latestSnapshots.Max(s => s.CollectedAt) : null,
            TotalUsers = userStats?.Total ?? 0,
            HighRiskUsers = userStats?.Risky ?? 0,
            TotalDevices = deviceStats?.Total ?? 0,
            NonCompliantDevices = (deviceStats?.Total ?? 0) - (deviceStats?.Compliant ?? 0),
            TotalApps = appStats?.Total ?? 0,
            HighPrivilegeApps = appStats?.HighRisk ?? 0,
            CriticalFindings = highRiskFindings.Count(f => f.Severity == "Critical"),
            HighFindings = highRiskFindings.Count(f => f.Severity == "High"),
            MediumFindings = highRiskFindings.Count(f => f.Severity == "Medium"),
            DomainStatuses = domainStatuses,
            HighRiskSummary = highRiskFindings.Select(f => new HighRiskFindingSummaryDto
            {
                FindingType = f.FindingType,
                Title = f.Title,
                Description = f.Description,
                Severity = f.Severity,
                Category = f.Category,
                AffectedCount = f.AffectedCount,
                Remediation = f.Remediation
            }).ToList(),
            E5Utilization = e5Util == null ? null : new E5UtilizationSummaryDto
            {
                TotalLicenses = e5Util.E5LicensesTotal,
                AssignedLicenses = e5Util.E5LicensesAssigned,
                UtilizationPercentage = e5Util.E5UtilizationPercentage,
                UsersWithoutMfa = e5Util.UsersWithE5NoMfa,
                UsersWithoutCa = e5Util.UsersWithE5NoConditionalAccess,
                UsersWithoutDefender = e5Util.UsersWithE5DefenderNotOnboarded,
                EstimatedWaste = e5Util.EstimatedMonthlyWaste
            }
        };
    }

    #endregion

    #region Tenant Baseline

    public async Task<TenantBaselineDto> GetTenantBaselineAsync(
        Guid tenantId,
        Guid? snapshotId = null,
        CancellationToken ct = default)
    {
        var query = _dbContext.TenantInfos.Where(t => t.TenantId == tenantId);

        if (snapshotId.HasValue)
        {
            query = query.Where(t => t.SnapshotId == snapshotId.Value);
        }
        else
        {
            query = query.OrderByDescending(t => t.CreatedAt);
        }

        var tenantInfo = await query.FirstOrDefaultAsync(ct);
        if (tenantInfo == null)
        {
            return new TenantBaselineDto { TenantId = tenantId };
        }

        var subscriptions = await _dbContext.LicenseSubscriptions
            .Where(s => s.TenantId == tenantId && s.SnapshotId == tenantInfo.SnapshotId)
            .ToListAsync(ct);

        return new TenantBaselineDto
        {
            TenantId = tenantId,
            AzureTenantId = tenantInfo.AzureTenantId,
            DisplayName = tenantInfo.DisplayName,
            PrimaryDomain = tenantInfo.PrimaryDomain,
            ModernAuthEnabled = tenantInfo.ModernAuthEnabled,
            SmtpAuthEnabled = tenantInfo.SmtpAuthEnabled,
            LegacyProtocolsEnabled = tenantInfo.LegacyProtocolsEnabled,
            IsMultiGeoEnabled = tenantInfo.IsMultiGeoEnabled,
            PreferredDataLocation = tenantInfo.PreferredDataLocation,
            Subscriptions = subscriptions.Select(s => new LicenseSubscriptionDto
            {
                SkuId = s.SkuId,
                SkuPartNumber = s.SkuPartNumber,
                DisplayName = s.DisplayName,
                PrepaidUnits = s.PrepaidUnits,
                ConsumedUnits = s.ConsumedUnits,
                AvailableUnits = s.AvailableUnits,
                IsTrial = s.IsTrial,
                ExpirationDate = s.ExpirationDate
            }).ToList(),
            LastCollected = tenantInfo.CreatedAt
        };
    }

    public async Task<IReadOnlyList<LicenseSubscriptionDto>> GetSubscriptionsAsync(
        Guid tenantId,
        Guid? snapshotId = null,
        CancellationToken ct = default)
    {
        var query = _dbContext.LicenseSubscriptions.Where(s => s.TenantId == tenantId);

        if (snapshotId.HasValue)
        {
            query = query.Where(s => s.SnapshotId == snapshotId.Value);
        }
        else
        {
            var latestSnapshotId = await _dbContext.InventorySnapshots
                .Where(s => s.TenantId == tenantId && s.Domain == InventoryDomain.TenantBaseline && s.Status == InventoryStatus.Completed)
                .OrderByDescending(s => s.CollectedAt)
                .Select(s => s.Id)
                .FirstOrDefaultAsync(ct);

            if (latestSnapshotId != default)
            {
                query = query.Where(s => s.SnapshotId == latestSnapshotId);
            }
        }

        var subscriptions = await query.ToListAsync(ct);

        return subscriptions.Select(s => new LicenseSubscriptionDto
        {
            SkuId = s.SkuId,
            SkuPartNumber = s.SkuPartNumber,
            DisplayName = s.DisplayName,
            PrepaidUnits = s.PrepaidUnits,
            ConsumedUnits = s.ConsumedUnits,
            AvailableUnits = s.AvailableUnits,
            IsTrial = s.IsTrial,
            ExpirationDate = s.ExpirationDate
        }).ToList();
    }

    #endregion

    #region Identity & Access

    public async Task<PagedResult<UserInventoryDto>> GetUsersAsync(
        Guid tenantId,
        UserInventoryFilter filter,
        Guid? snapshotId = null,
        CancellationToken ct = default)
    {
        var query = await GetLatestSnapshotQueryAsync<UserInventory>(
            tenantId, InventoryDomain.IdentityAccess, snapshotId, ct);

        // Apply filters
        if (!string.IsNullOrWhiteSpace(filter.SearchTerm))
        {
            var searchTerm = filter.SearchTerm.ToLower();
            query = query.Where(u =>
                u.UserPrincipalName.ToLower().Contains(searchTerm) ||
                (u.DisplayName != null && u.DisplayName.ToLower().Contains(searchTerm)) ||
                (u.Mail != null && u.Mail.ToLower().Contains(searchTerm)));
        }

        if (!string.IsNullOrWhiteSpace(filter.UserType))
        {
            query = query.Where(u => u.UserType == filter.UserType);
        }

        if (filter.IsPrivileged.HasValue)
        {
            query = query.Where(u => u.IsPrivileged == filter.IsPrivileged.Value);
        }

        if (filter.MfaEnabled.HasValue)
        {
            query = query.Where(u => u.IsMfaRegistered == filter.MfaEnabled.Value);
        }

        if (filter.HasE5License.HasValue)
        {
            query = query.Where(u => u.HasE5License == filter.HasE5License.Value);
        }

        if (!string.IsNullOrWhiteSpace(filter.RiskLevel))
        {
            query = query.Where(u => u.RiskLevel == filter.RiskLevel);
        }

        if (filter.AccountEnabled.HasValue)
        {
            query = query.Where(u => u.AccountEnabled == filter.AccountEnabled.Value);
        }

        // License category filtering
        if (filter.LicenseCategory.HasValue)
        {
            query = query.Where(u => u.PrimaryLicenseCategory == filter.LicenseCategory.Value);
        }

        if (filter.LicenseCategories != null && filter.LicenseCategories.Any())
        {
            query = query.Where(u => filter.LicenseCategories.Contains(u.PrimaryLicenseCategory));
        }

        if (!string.IsNullOrWhiteSpace(filter.TierGroup))
        {
            query = query.Where(u => u.PrimaryLicenseTierGroup == filter.TierGroup);
        }

        if (filter.HasAnyLicense.HasValue)
        {
            if (filter.HasAnyLicense.Value)
            {
                query = query.Where(u => u.LicenseCount > 0);
            }
            else
            {
                query = query.Where(u => u.LicenseCount == 0);
            }
        }

        // Apply sorting
        query = filter.SortBy?.ToLower() switch
        {
            "upn" => filter.SortDescending
                ? query.OrderByDescending(u => u.UserPrincipalName)
                : query.OrderBy(u => u.UserPrincipalName),
            "displayname" => filter.SortDescending
                ? query.OrderByDescending(u => u.DisplayName)
                : query.OrderBy(u => u.DisplayName),
            "lastsignin" => filter.SortDescending
                ? query.OrderByDescending(u => u.LastSignInDateTime)
                : query.OrderBy(u => u.LastSignInDateTime),
            "created" => filter.SortDescending
                ? query.OrderByDescending(u => u.CreatedDateTime)
                : query.OrderBy(u => u.CreatedDateTime),
            _ => query.OrderBy(u => u.UserPrincipalName)
        };

        var totalCount = await query.CountAsync(ct);

        var users = await query
            .Skip((filter.Page - 1) * filter.PageSize)
            .Take(filter.PageSize)
            .ToListAsync(ct);

        return new PagedResult<UserInventoryDto>
        {
            Items = users.Select(MapToUserDto).ToList(),
            TotalCount = totalCount,
            Page = filter.Page,
            PageSize = filter.PageSize
        };
    }

    public async Task<UserInventoryDto?> GetUserAsync(
        Guid tenantId,
        string objectId,
        Guid? snapshotId = null,
        CancellationToken ct = default)
    {
        var query = await GetLatestSnapshotQueryAsync<UserInventory>(
            tenantId, InventoryDomain.IdentityAccess, snapshotId, ct);

        var user = await query.FirstOrDefaultAsync(u => u.ObjectId == objectId, ct);
        return user == null ? null : MapToUserDto(user);
    }

    public async Task<PagedResult<GroupInventoryDto>> GetGroupsAsync(
        Guid tenantId,
        GroupInventoryFilter filter,
        Guid? snapshotId = null,
        CancellationToken ct = default)
    {
        var query = await GetLatestSnapshotQueryAsync<GroupInventory>(
            tenantId, InventoryDomain.IdentityAccess, snapshotId, ct);

        if (!string.IsNullOrWhiteSpace(filter.SearchTerm))
        {
            var searchTerm = filter.SearchTerm.ToLower();
            query = query.Where(g =>
                g.DisplayName.ToLower().Contains(searchTerm) ||
                (g.Mail != null && g.Mail.ToLower().Contains(searchTerm)));
        }

        if (!string.IsNullOrWhiteSpace(filter.GroupType))
        {
            query = query.Where(g => g.GroupType == filter.GroupType);
        }

        if (filter.HasExternalMembers.HasValue)
        {
            query = query.Where(g => g.HasExternalMembers == filter.HasExternalMembers.Value);
        }

        if (filter.IsDynamic.HasValue)
        {
            query = query.Where(g => g.IsDynamicMembership == filter.IsDynamic.Value);
        }

        var totalCount = await query.CountAsync(ct);

        var groups = await query
            .OrderBy(g => g.DisplayName)
            .Skip((filter.Page - 1) * filter.PageSize)
            .Take(filter.PageSize)
            .ToListAsync(ct);

        return new PagedResult<GroupInventoryDto>
        {
            Items = groups.Select(g => new GroupInventoryDto
            {
                Id = g.Id,
                ObjectId = g.ObjectId,
                DisplayName = g.DisplayName,
                Description = g.Description,
                GroupType = g.GroupType,
                IsSecurityGroup = g.IsSecurityGroup,
                IsMicrosoft365Group = g.IsMicrosoft365Group,
                IsDynamicMembership = g.IsDynamicMembership,
                MemberCount = g.MemberCount,
                OwnerCount = g.OwnerCount,
                HasExternalMembers = g.HasExternalMembers,
                IsRoleAssignable = g.IsRoleAssignable,
                Visibility = g.Visibility,
                CreatedDateTime = g.CreatedDateTime
            }).ToList(),
            TotalCount = totalCount,
            Page = filter.Page,
            PageSize = filter.PageSize
        };
    }

    public async Task<IReadOnlyList<DirectoryRoleDto>> GetDirectoryRolesAsync(
        Guid tenantId,
        Guid? snapshotId = null,
        CancellationToken ct = default)
    {
        var query = await GetLatestSnapshotQueryAsync<DirectoryRoleInventory>(
            tenantId, InventoryDomain.IdentityAccess, snapshotId, ct);

        var roles = await query
            .OrderByDescending(r => r.IsPrivileged)
            .ThenByDescending(r => r.MemberCount)
            .ToListAsync(ct);

        return roles.Select(r => new DirectoryRoleDto
        {
            Id = r.Id,
            RoleTemplateId = r.RoleTemplateId,
            DisplayName = r.DisplayName,
            Description = r.Description,
            IsBuiltIn = r.IsBuiltIn,
            IsPrivileged = r.IsPrivileged,
            MemberCount = r.MemberCount,
            UserMemberCount = r.UserMemberCount,
            ServicePrincipalMemberCount = r.ServicePrincipalMemberCount
        }).ToList();
    }

    public async Task<IReadOnlyList<ConditionalAccessPolicyDto>> GetConditionalAccessPoliciesAsync(
        Guid tenantId,
        Guid? snapshotId = null,
        CancellationToken ct = default)
    {
        var query = await GetLatestSnapshotQueryAsync<ConditionalAccessPolicyInventory>(
            tenantId, InventoryDomain.IdentityAccess, snapshotId, ct);

        var policies = await query
            .OrderBy(p => p.DisplayName)
            .ToListAsync(ct);

        return policies.Select(p => new ConditionalAccessPolicyDto
        {
            Id = p.Id,
            PolicyId = p.PolicyId,
            DisplayName = p.DisplayName,
            State = p.State,
            CreatedDateTime = p.CreatedDateTime,
            RequiresMfa = p.RequiresMfa,
            BlocksLegacyAuth = p.BlocksLegacyAuth,
            BlocksAccess = p.BlocksAccess,
            IncludeAllUsers = p.IncludesAllUsers,
            IncludeAllApplications = p.IncludesAllApps,
            RequiresCompliantDevice = p.RequiresCompliantDevice,
            RequiresHybridJoin = p.RequiresHybridAzureAdJoin,
            IncludeUsers = ParseJsonArray(p.IncludeUsersJson),
            ExcludeUsers = ParseJsonArray(p.ExcludeUsersJson),
            IncludeApplications = ParseJsonArray(p.IncludeApplicationsJson),
            ExcludeApplications = ParseJsonArray(p.ExcludeApplicationsJson),
            GrantControls = ParseJsonArray(p.GrantControlsJson),
            SessionControls = ParseJsonArray(p.SessionControlsJson)
        }).ToList();
    }

    #endregion

    #region Devices

    public async Task<PagedResult<DeviceInventoryDto>> GetDevicesAsync(
        Guid tenantId,
        DeviceInventoryFilter filter,
        Guid? snapshotId = null,
        CancellationToken ct = default)
    {
        var query = await GetLatestSnapshotQueryAsync<DeviceInventory>(
            tenantId, InventoryDomain.DeviceEndpoint, snapshotId, ct);

        if (!string.IsNullOrWhiteSpace(filter.SearchTerm))
        {
            var searchTerm = filter.SearchTerm.ToLower();
            query = query.Where(d =>
                d.DeviceName.ToLower().Contains(searchTerm) ||
                (d.PrimaryUserUpn != null && d.PrimaryUserUpn.ToLower().Contains(searchTerm)));
        }

        if (!string.IsNullOrWhiteSpace(filter.OperatingSystem))
        {
            query = query.Where(d => d.OperatingSystem == filter.OperatingSystem);
        }

        if (!string.IsNullOrWhiteSpace(filter.ComplianceState))
        {
            query = query.Where(d => d.ComplianceState == filter.ComplianceState);
        }

        if (!string.IsNullOrWhiteSpace(filter.OwnerType))
        {
            query = query.Where(d => d.OwnerType == filter.OwnerType);
        }

        if (filter.HasDefender.HasValue)
        {
            query = query.Where(d => d.HasDefenderForEndpoint == filter.HasDefender.Value);
        }

        if (filter.IsEncrypted.HasValue)
        {
            query = query.Where(d => d.IsEncrypted == filter.IsEncrypted.Value);
        }

        var totalCount = await query.CountAsync(ct);

        var devices = await query
            .OrderBy(d => d.DeviceName)
            .Skip((filter.Page - 1) * filter.PageSize)
            .Take(filter.PageSize)
            .ToListAsync(ct);

        return new PagedResult<DeviceInventoryDto>
        {
            Items = devices.Select(d => new DeviceInventoryDto
            {
                Id = d.Id,
                DeviceId = d.DeviceId,
                DeviceName = d.DeviceName,
                OperatingSystem = d.OperatingSystem,
                OsVersion = d.OsVersion,
                OwnerType = d.OwnerType,
                ComplianceState = d.ComplianceState,
                IsManaged = d.IsManaged,
                IsEncrypted = d.IsEncrypted,
                HasDefenderForEndpoint = d.HasDefenderForEndpoint,
                PrimaryUserUpn = d.PrimaryUserUpn,
                EnrolledDateTime = d.EnrolledDateTime,
                LastSyncDateTime = d.LastSyncDateTime,
                RiskScore = d.RiskScore
            }).ToList(),
            TotalCount = totalCount,
            Page = filter.Page,
            PageSize = filter.PageSize
        };
    }

    #endregion

    #region Defender XDR

    public async Task<DefenderForEndpointDto?> GetDefenderForEndpointAsync(
        Guid tenantId,
        Guid? snapshotId = null,
        CancellationToken ct = default)
    {
        var query = await GetLatestSnapshotQueryAsync<DefenderForEndpointInventory>(
            tenantId, InventoryDomain.DefenderXDR, snapshotId, ct);

        if (query == null) return null;

        var entity = await query.FirstOrDefaultAsync(ct);
        if (entity == null) return null;

        var snapshot = await _dbContext.InventorySnapshots
            .FirstOrDefaultAsync(s => s.Id == entity.SnapshotId, ct);

        return new DefenderForEndpointDto
        {
            Id = entity.Id,
            TenantId = entity.TenantId,
            CollectedAt = snapshot?.CollectedAt,
            OnboardedDeviceCount = entity.OnboardedDeviceCount,
            TotalManagedDeviceCount = entity.TotalManagedDeviceCount,
            OnboardingCoverage = entity.OnboardingCoverage,
            WindowsOnboarded = entity.WindowsOnboarded,
            MacOsOnboarded = entity.MacOsOnboarded,
            LinuxOnboarded = entity.LinuxOnboarded,
            MobileOnboarded = entity.MobileOnboarded,
            ActiveSensors = entity.ActiveSensors,
            InactiveSensors = entity.InactiveSensors,
            MisconfiguredSensors = entity.MisconfiguredSensors,
            ImpairedCommunication = entity.ImpairedCommunication,
            NoSensorData = entity.NoSensorData,
            HighRiskDevices = entity.HighRiskDevices,
            MediumRiskDevices = entity.MediumRiskDevices,
            LowRiskDevices = entity.LowRiskDevices,
            NoRiskInfoDevices = entity.NoRiskInfoDevices,
            TamperProtectionEnabled = entity.TamperProtectionEnabled,
            EdrInBlockMode = entity.EdrInBlockMode,
            NetworkProtectionEnabled = entity.NetworkProtectionEnabled,
            WebProtectionEnabled = entity.WebProtectionEnabled,
            CloudProtectionEnabled = entity.CloudProtectionEnabled,
            PuaProtectionEnabled = entity.PuaProtectionEnabled,
            RealTimeProtectionEnabled = entity.RealTimeProtectionEnabled,
            AsrRulesConfigured = entity.AsrRulesConfigured,
            AsrRulesCount = entity.AsrRulesCount,
            AsrRulesBlockMode = entity.AsrRulesBlockMode,
            AsrRulesAuditMode = entity.AsrRulesAuditMode,
            ExposureScore = entity.ExposureScore,
            SecureScore = entity.SecureScore,
            VulnerabilityCount = entity.VulnerabilityCount,
            CriticalVulnerabilities = entity.CriticalVulnerabilities,
            HighVulnerabilities = entity.HighVulnerabilities,
            MediumVulnerabilities = entity.MediumVulnerabilities,
            MissingPatches = entity.MissingPatches,
            MissingKbCount = entity.MissingKbCount,
            ActiveAlerts = entity.ActiveAlerts,
            HighSeverityAlerts = entity.HighSeverityAlerts,
            MediumSeverityAlerts = entity.MediumSeverityAlerts,
            LowSeverityAlerts = entity.LowSeverityAlerts,
            InformationalAlerts = entity.InformationalAlerts
        };
    }

    public async Task<DefenderForOffice365Dto?> GetDefenderForOffice365Async(
        Guid tenantId,
        Guid? snapshotId = null,
        CancellationToken ct = default)
    {
        var query = await GetLatestSnapshotQueryAsync<DefenderForOffice365Inventory>(
            tenantId, InventoryDomain.DefenderXDR, snapshotId, ct);

        if (query == null) return null;

        var entity = await query.FirstOrDefaultAsync(ct);
        if (entity == null) return null;

        var snapshot = await _dbContext.InventorySnapshots
            .FirstOrDefaultAsync(s => s.Id == entity.SnapshotId, ct);

        return new DefenderForOffice365Dto
        {
            Id = entity.Id,
            TenantId = entity.TenantId,
            CollectedAt = snapshot?.CollectedAt,
            SafeLinksEnabled = entity.SafeLinksEnabled,
            SafeLinksPolicyCount = entity.SafeLinksPolicyCount,
            SafeLinksForOfficeApps = entity.SafeLinksForOfficeApps,
            SafeLinksTrackUserClicks = entity.SafeLinksTrackUserClicks,
            SafeAttachmentsEnabled = entity.SafeAttachmentsEnabled,
            SafeAttachmentsPolicyCount = entity.SafeAttachmentsPolicyCount,
            SafeAttachmentsMode = entity.SafeAttachmentsMode,
            SafeAttachmentsForSharePoint = entity.SafeAttachmentsForSharePoint,
            AntiPhishPolicyCount = entity.AntiPhishPolicyCount,
            ImpersonationProtectionEnabled = entity.ImpersonationProtectionEnabled,
            MailboxIntelligenceEnabled = entity.MailboxIntelligenceEnabled,
            SpoofIntelligenceEnabled = entity.SpoofIntelligenceEnabled,
            ProtectedUsersCount = entity.ProtectedUsersCount,
            ProtectedDomainsCount = entity.ProtectedDomainsCount,
            AntiSpamPolicyCount = entity.AntiSpamPolicyCount,
            DefaultSpamAction = entity.DefaultSpamAction,
            HighConfidenceSpamAction = entity.HighConfidenceSpamAction,
            AntiMalwarePolicyCount = entity.AntiMalwarePolicyCount,
            CommonAttachmentTypesFilter = entity.CommonAttachmentTypesFilter,
            ZeroHourAutoPurgeEnabled = entity.ZeroHourAutoPurgeEnabled,
            DkimEnabled = entity.DkimEnabled,
            DkimEnabledDomains = entity.DkimEnabledDomains,
            DmarcEnabled = entity.DmarcEnabled,
            DmarcPolicy = entity.DmarcPolicy,
            SpfConfigured = entity.SpfConfigured,
            Last30DaysMalwareCount = entity.Last30DaysMalwareCount,
            Last30DaysPhishCount = entity.Last30DaysPhishCount,
            Last30DaysSpamCount = entity.Last30DaysSpamCount,
            Last30DaysBlockedCount = entity.Last30DaysBlockedCount
        };
    }

    public async Task<DefenderForIdentityDto?> GetDefenderForIdentityAsync(
        Guid tenantId,
        Guid? snapshotId = null,
        CancellationToken ct = default)
    {
        var query = await GetLatestSnapshotQueryAsync<DefenderForIdentityInventory>(
            tenantId, InventoryDomain.DefenderXDR, snapshotId, ct);

        if (query == null) return null;

        var entity = await query.FirstOrDefaultAsync(ct);
        if (entity == null) return null;

        var snapshot = await _dbContext.InventorySnapshots
            .FirstOrDefaultAsync(s => s.Id == entity.SnapshotId, ct);

        return new DefenderForIdentityDto
        {
            Id = entity.Id,
            TenantId = entity.TenantId,
            CollectedAt = snapshot?.CollectedAt,
            IsConfigured = entity.IsConfigured,
            IsLicensed = entity.IsLicensed,
            WorkspaceId = entity.WorkspaceId,
            SensorCount = entity.SensorCount,
            HealthySensors = entity.HealthySensors,
            UnhealthySensors = entity.UnhealthySensors,
            OfflineSensors = entity.OfflineSensors,
            DomainControllersCovered = entity.DomainControllersCovered,
            TotalDomainControllers = entity.TotalDomainControllers,
            CoveragePercentage = entity.CoveragePercentage,
            OpenHealthIssues = entity.OpenHealthIssues,
            HighSeverityHealthIssues = entity.HighSeverityHealthIssues,
            MediumSeverityHealthIssues = entity.MediumSeverityHealthIssues,
            LowSeverityHealthIssues = entity.LowSeverityHealthIssues,
            HighSeverityAlerts = entity.HighSeverityAlerts,
            MediumSeverityAlerts = entity.MediumSeverityAlerts,
            LowSeverityAlerts = entity.LowSeverityAlerts,
            Last30DaysAlerts = entity.Last30DaysAlerts,
            HoneytokenAccountsConfigured = entity.HoneytokenAccountsConfigured,
            HoneytokenAccountCount = entity.HoneytokenAccountCount,
            SensitiveGroupsConfigured = entity.SensitiveGroupsConfigured
        };
    }

    public async Task<DefenderForCloudAppsDto?> GetDefenderForCloudAppsAsync(
        Guid tenantId,
        Guid? snapshotId = null,
        CancellationToken ct = default)
    {
        var query = await GetLatestSnapshotQueryAsync<DefenderForCloudAppsInventory>(
            tenantId, InventoryDomain.DefenderXDR, snapshotId, ct);

        if (query == null) return null;

        var entity = await query.FirstOrDefaultAsync(ct);
        if (entity == null) return null;

        var snapshot = await _dbContext.InventorySnapshots
            .FirstOrDefaultAsync(s => s.Id == entity.SnapshotId, ct);

        return new DefenderForCloudAppsDto
        {
            Id = entity.Id,
            TenantId = entity.TenantId,
            CollectedAt = snapshot?.CollectedAt,
            ConnectedAppCount = entity.ConnectedAppCount,
            Office365Connected = entity.Office365Connected,
            AzureConnected = entity.AzureConnected,
            AwsConnected = entity.AwsConnected,
            GcpConnected = entity.GcpConnected,
            OAuthAppCount = entity.OAuthAppCount,
            HighRiskOAuthApps = entity.HighRiskOAuthApps,
            MediumRiskOAuthApps = entity.MediumRiskOAuthApps,
            LowRiskOAuthApps = entity.LowRiskOAuthApps,
            AppGovernanceEnabled = entity.AppGovernanceEnabled,
            AppGovernancePolicyCount = entity.AppGovernancePolicyCount,
            AppGovernanceAlerts = entity.AppGovernanceAlerts,
            ActivityPolicyCount = entity.ActivityPolicyCount,
            AnomalyPolicyCount = entity.AnomalyPolicyCount,
            SessionPolicyCount = entity.SessionPolicyCount,
            AccessPolicyCount = entity.AccessPolicyCount,
            FilePolicyCount = entity.FilePolicyCount,
            EnabledPolicies = entity.EnabledPolicies,
            DisabledPolicies = entity.DisabledPolicies,
            CloudDiscoveryEnabled = entity.CloudDiscoveryEnabled,
            DiscoveredAppCount = entity.DiscoveredAppCount,
            SanctionedApps = entity.SanctionedApps,
            UnsanctionedApps = entity.UnsanctionedApps,
            MonitoredApps = entity.MonitoredApps,
            OpenAlerts = entity.OpenAlerts,
            HighSeverityAlerts = entity.HighSeverityAlerts,
            MediumSeverityAlerts = entity.MediumSeverityAlerts,
            LowSeverityAlerts = entity.LowSeverityAlerts,
            SessionControlEnabled = entity.SessionControlEnabled,
            SessionControlledApps = entity.SessionControlledApps
        };
    }

    #endregion

    #region Applications

    public async Task<PagedResult<EnterpriseAppDto>> GetAppsAsync(
        Guid tenantId,
        AppInventoryFilter filter,
        Guid? snapshotId = null,
        CancellationToken ct = default)
    {
        var query = await GetLatestSnapshotQueryAsync<EnterpriseAppInventory>(
            tenantId, InventoryDomain.ApplicationsOAuth, snapshotId, ct);

        if (!string.IsNullOrWhiteSpace(filter.SearchTerm))
        {
            var searchTerm = filter.SearchTerm.ToLower();
            query = query.Where(a =>
                a.DisplayName.ToLower().Contains(searchTerm) ||
                (a.PublisherName != null && a.PublisherName.ToLower().Contains(searchTerm)));
        }

        if (filter.HasHighPrivilegePermissions.HasValue)
        {
            query = query.Where(a => a.HasHighPrivilegePermissions == filter.HasHighPrivilegePermissions.Value);
        }

        if (filter.IsMicrosoftApp.HasValue)
        {
            query = query.Where(a => a.IsMicrosoftApp == filter.IsMicrosoftApp.Value);
        }

        if (filter.HasExpiredCredentials.HasValue)
        {
            query = query.Where(a => a.HasExpiredCredentials == filter.HasExpiredCredentials.Value);
        }

        var totalCount = await query.CountAsync(ct);

        var apps = await query
            .OrderBy(a => a.DisplayName)
            .Skip((filter.Page - 1) * filter.PageSize)
            .Take(filter.PageSize)
            .ToListAsync(ct);

        return new PagedResult<EnterpriseAppDto>
        {
            Items = apps.Select(a => new EnterpriseAppDto
            {
                Id = a.Id,
                ObjectId = a.ObjectId,
                AppId = a.AppId,
                DisplayName = a.DisplayName,
                PublisherName = a.PublisherName,
                AccountEnabled = a.AccountEnabled,
                IsMicrosoftApp = a.IsMicrosoftApp,
                IsVerifiedPublisher = a.IsVerifiedPublisher,
                HasHighPrivilegePermissions = a.HasHighPrivilegePermissions,
                HasMailReadWrite = a.HasMailReadWrite,
                HasDirectoryReadWriteAll = a.HasDirectoryReadWriteAll,
                HasFilesReadWriteAll = a.HasFilesReadWriteAll,
                ApplicationPermissions = ParseJsonArray(a.ApplicationPermissionsJson),
                DelegatedPermissions = ParseJsonArray(a.DelegatedPermissionsJson),
                NextCredentialExpiration = a.NextCredentialExpiration,
                HasExpiredCredentials = a.HasExpiredCredentials,
                OwnerCount = a.OwnerCount,
                LastSignInDateTime = a.LastSignInDateTime
            }).ToList(),
            TotalCount = totalCount,
            Page = filter.Page,
            PageSize = filter.PageSize
        };
    }

    #endregion

    #region Collaboration

    public async Task<PagedResult<SharePointSiteDto>> GetSharePointSitesAsync(
        Guid tenantId,
        SharePointSiteFilter filter,
        Guid? snapshotId = null,
        CancellationToken ct = default)
    {
        var query = await GetLatestSnapshotQueryAsync<SharePointSiteInventory>(
            tenantId, InventoryDomain.SharePointOneDriveTeams, snapshotId, ct);

        if (!string.IsNullOrWhiteSpace(filter.SearchTerm))
        {
            var term = filter.SearchTerm.ToLower();
            query = query.Where(s =>
                (s.Title != null && s.Title.ToLower().Contains(term)) ||
                s.WebUrl.ToLower().Contains(term));
        }

        if (filter.HasExternalSharing.HasValue)
        {
            query = query.Where(s => s.HasExternalSharing == filter.HasExternalSharing.Value);
        }

        var totalCount = await query.CountAsync(ct);

        var sites = await query
            .OrderBy(s => s.Title)
            .Skip((filter.Page - 1) * filter.PageSize)
            .Take(filter.PageSize)
            .ToListAsync(ct);

        return new PagedResult<SharePointSiteDto>
        {
            Items = sites.Select(s => new SharePointSiteDto
            {
                Id = s.Id,
                SiteId = s.SiteId,
                WebUrl = s.WebUrl,
                Title = s.Title ?? string.Empty,
                Template = s.Template,
                HasExternalSharing = s.HasExternalSharing,
                ExternalUserCount = s.ExternalUserCount,
                StorageUsedBytes = s.StorageUsedBytes,
                StoragePercentUsed = s.StoragePercentUsed,
                OwnerUpn = s.OwnerUpn,
                IsOrphaned = s.IsOrphaned,
                IsInactive = s.IsInactive,
                SensitivityLabel = s.SensitivityLabel,
                LastActivityDate = s.LastActivityDate
            }).ToList(),
            TotalCount = totalCount,
            Page = filter.Page,
            PageSize = filter.PageSize
        };
    }

    public async Task<PagedResult<TeamsDto>> GetTeamsAsync(
        Guid tenantId,
        TeamsFilter filter,
        Guid? snapshotId = null,
        CancellationToken ct = default)
    {
        var query = await GetLatestSnapshotQueryAsync<TeamsInventory>(
            tenantId, InventoryDomain.SharePointOneDriveTeams, snapshotId, ct);

        if (!string.IsNullOrWhiteSpace(filter.SearchTerm))
        {
            var term = filter.SearchTerm.ToLower();
            query = query.Where(t => t.DisplayName.ToLower().Contains(term));
        }

        if (filter.HasGuests.HasValue)
        {
            query = filter.HasGuests.Value
                ? query.Where(t => t.GuestCount > 0)
                : query.Where(t => t.GuestCount == 0);
        }

        if (filter.IsArchived.HasValue)
        {
            query = query.Where(t => t.IsArchived == filter.IsArchived.Value);
        }

        var totalCount = await query.CountAsync(ct);

        var teams = await query
            .OrderBy(t => t.DisplayName)
            .Skip((filter.Page - 1) * filter.PageSize)
            .Take(filter.PageSize)
            .ToListAsync(ct);

        return new PagedResult<TeamsDto>
        {
            Items = teams.Select(t => new TeamsDto
            {
                Id = t.Id,
                TeamId = t.TeamId,
                DisplayName = t.DisplayName,
                Description = t.Description,
                Visibility = t.Visibility,
                MemberCount = t.MemberCount,
                OwnerCount = t.OwnerCount,
                GuestCount = t.GuestCount,
                StandardChannelCount = t.StandardChannelCount,
                PrivateChannelCount = t.PrivateChannelCount,
                SharedChannelCount = t.SharedChannelCount,
                IsArchived = t.IsArchived,
                LastActivityDate = t.LastActivityDate,
                CreatedDateTime = t.CreatedDateTime
            }).ToList(),
            TotalCount = totalCount,
            Page = filter.Page,
            PageSize = filter.PageSize
        };
    }

    #endregion

    #region Service Principals

    public async Task<IReadOnlyList<ServicePrincipalDto>> GetServicePrincipalsAsync(
        Guid tenantId,
        Guid? snapshotId = null,
        CancellationToken ct = default)
    {
        var query = await GetLatestSnapshotQueryAsync<ServicePrincipalInventory>(
            tenantId, InventoryDomain.ApplicationsOAuth, snapshotId, ct);

        var sps = await query
            .OrderBy(s => s.DisplayName)
            .ToListAsync(ct);

        return sps.Select(s => new ServicePrincipalDto
        {
            Id = s.Id,
            ObjectId = s.ObjectId,
            AppId = s.AppId,
            DisplayName = s.DisplayName,
            ServicePrincipalType = s.ServicePrincipalType,
            IsMicrosoftFirstParty = s.IsMicrosoftFirstParty,
            HasHighPrivilegePermissions = s.HasHighPrivilegePermissions,
            ApplicationPermissions = ParseJsonArray(s.ApplicationPermissionsJson),
            DelegatedPermissions = ParseJsonArray(s.DelegatedPermissionsJson)
        }).ToList();
    }

    #endregion

    #region License Utilization

    public async Task<LicenseUtilizationDto> GetLicenseUtilizationAsync(
        Guid tenantId,
        Guid? snapshotId = null,
        CancellationToken ct = default)
    {
        var query = await GetLatestSnapshotQueryAsync<LicenseUtilizationInventory>(
            tenantId, InventoryDomain.LicenseUtilization, snapshotId, ct);

        var util = await query.FirstOrDefaultAsync(ct);

        if (util == null)
        {
            return new LicenseUtilizationDto();
        }

        return new LicenseUtilizationDto
        {
            TotalLicenses = util.E5LicensesTotal,
            AssignedLicenses = util.E5LicensesAssigned,
            UtilizationPercentage = util.E5UtilizationPercentage,
            UsersWithoutMfa = util.UsersWithE5NoMfa,
            UsersWithoutCa = util.UsersWithE5NoConditionalAccess,
            UsersWithoutDefender = util.UsersWithE5DefenderNotOnboarded,
            EstimatedWaste = util.EstimatedMonthlyWaste,
            UsersWithoutMfaList = ParseJsonArray(util.NoMfaUsersJson),
            UsersWithoutCaList = new List<string>(),
            UsersWithoutDefenderList = new List<string>()
        };
    }

    public async Task<IReadOnlyList<LicenseSubscriptionDto>> GetLicenseSubscriptionsAsync(
        Guid tenantId,
        Guid? snapshotId = null,
        CancellationToken ct = default)
    {
        var query = _dbContext.LicenseSubscriptions.Where(s => s.TenantId == tenantId);

        if (snapshotId.HasValue)
        {
            query = query.Where(s => s.SnapshotId == snapshotId.Value);
        }
        else
        {
            var latestSnapshotId = await _dbContext.InventorySnapshots
                .Where(s => s.TenantId == tenantId && s.Domain == InventoryDomain.TenantBaseline && s.Status == InventoryStatus.Completed)
                .OrderByDescending(s => s.CollectedAt)
                .Select(s => s.Id)
                .FirstOrDefaultAsync(ct);

            if (latestSnapshotId != default)
            {
                query = query.Where(s => s.SnapshotId == latestSnapshotId);
            }
        }

        var subscriptions = await query.ToListAsync(ct);

        return subscriptions.Select(MapToLicenseSubscriptionDto).ToList();
    }

    public async Task<MultiLicenseUtilizationDto> GetMultiLicenseUtilizationAsync(
        Guid tenantId,
        Guid? snapshotId = null,
        CancellationToken ct = default)
    {
        var distribution = await GetLicenseDistributionAsync(tenantId, snapshotId, ct);

        // Get per-category utilization
        var categoryUtils = await _dbContext.LicenseCategoryUtilizations
            .Where(u => u.TenantId == tenantId)
            .ToListAsync(ct);

        if (snapshotId.HasValue)
        {
            categoryUtils = categoryUtils.Where(u => u.SnapshotId == snapshotId.Value).ToList();
        }
        else
        {
            // Get latest per category
            var latestSnapshotIds = await _dbContext.InventorySnapshots
                .Where(s => s.TenantId == tenantId && s.Domain == InventoryDomain.LicenseUtilization && s.Status == InventoryStatus.Completed)
                .GroupBy(s => 1)
                .Select(g => g.OrderByDescending(s => s.CollectedAt).Select(s => s.Id).FirstOrDefault())
                .ToListAsync(ct);

            if (latestSnapshotIds.Any())
            {
                categoryUtils = categoryUtils.Where(u => latestSnapshotIds.Contains(u.SnapshotId)).ToList();
            }
        }

        var utilizationByCategory = categoryUtils.Select(u => new LicenseUtilizationByTypeDto
        {
            Category = u.LicenseCategory,
            DisplayName = u.CategoryDisplayName,
            TierGroup = u.TierGroup,
            TotalUsers = u.TotalUsersWithLicense,
            ActiveUsers = u.ActiveUsersWithLicense,
            DisabledUsers = u.DisabledUsersWithLicense,
            UsersUsingMfa = u.UsersWithMfaEnabled,
            UsersUsingCa = u.UsersWithConditionalAccess,
            UsersUsingDefender = u.UsersWithDefenderForEndpoint,
            UsersUsingPurview = u.UsersWithPurviewLabels,
            OverallFeatureUtilization = u.OverallFeatureUtilization,
            IdentityFeatureUtilization = u.IdentityFeatureUtilization,
            SecurityFeatureUtilization = u.SecurityFeatureUtilization,
            ComplianceFeatureUtilization = u.ComplianceFeatureUtilization,
            EstimatedMonthlyWaste = u.EstimatedMonthlyWaste,
            UsersEligibleForDowngrade = u.UsersEligibleForDowngrade
        }).ToList();

        // Get aggregate metrics
        var aggregateUtil = await _dbContext.LicenseUtilizationInventories
            .Where(u => u.TenantId == tenantId)
            .OrderByDescending(u => u.CreatedAt)
            .FirstOrDefaultAsync(ct);

        return new MultiLicenseUtilizationDto
        {
            TenantId = tenantId,
            LastCollected = aggregateUtil?.CreatedAt,
            TotalLicenses = distribution.TotalLicenses,
            TotalAssigned = distribution.TotalAssigned,
            OverallUtilization = distribution.OverallUtilization,
            TotalMonthlyCost = distribution.TotalMonthlyCost,
            TotalEstimatedWaste = distribution.TotalEstimatedWaste,
            TotalAnnualWaste = distribution.TotalEstimatedWaste * 12,
            TotalUsersWithoutMfa = aggregateUtil?.TotalUsersWithoutMfa ?? 0,
            TotalUsersWithoutCa = aggregateUtil?.TotalUsersWithoutCa ?? 0,
            TotalUsersWithoutDefender = aggregateUtil?.TotalUsersWithoutDefender ?? 0,
            UtilizationByCategory = utilizationByCategory,
            Distribution = distribution
        };
    }

    public async Task<LicenseUtilizationByTypeDto?> GetLicenseUtilizationByCategoryAsync(
        Guid tenantId,
        LicenseCategory category,
        Guid? snapshotId = null,
        CancellationToken ct = default)
    {
        var query = _dbContext.LicenseCategoryUtilizations
            .Where(u => u.TenantId == tenantId && u.LicenseCategory == category);

        if (snapshotId.HasValue)
        {
            query = query.Where(u => u.SnapshotId == snapshotId.Value);
        }
        else
        {
            query = query.OrderByDescending(u => u.CreatedAt);
        }

        var util = await query.FirstOrDefaultAsync(ct);
        if (util == null) return null;

        return new LicenseUtilizationByTypeDto
        {
            Category = util.LicenseCategory,
            DisplayName = util.CategoryDisplayName,
            TierGroup = util.TierGroup,
            TotalUsers = util.TotalUsersWithLicense,
            ActiveUsers = util.ActiveUsersWithLicense,
            DisabledUsers = util.DisabledUsersWithLicense,
            UsersUsingMfa = util.UsersWithMfaEnabled,
            UsersUsingCa = util.UsersWithConditionalAccess,
            UsersUsingDefender = util.UsersWithDefenderForEndpoint,
            UsersUsingPurview = util.UsersWithPurviewLabels,
            OverallFeatureUtilization = util.OverallFeatureUtilization,
            IdentityFeatureUtilization = util.IdentityFeatureUtilization,
            SecurityFeatureUtilization = util.SecurityFeatureUtilization,
            ComplianceFeatureUtilization = util.ComplianceFeatureUtilization,
            EstimatedMonthlyWaste = util.EstimatedMonthlyWaste,
            UsersEligibleForDowngrade = util.UsersEligibleForDowngrade,
            FeatureBreakdown = ParseFeatureBreakdown(util.FeatureUtilizationBreakdownJson)
        };
    }

    public async Task<LicenseDistributionDto> GetLicenseDistributionAsync(
        Guid tenantId,
        Guid? snapshotId = null,
        CancellationToken ct = default)
    {
        var subscriptions = await GetLicenseSubscriptionsAsync(tenantId, snapshotId, ct);

        var byCategory = subscriptions
            .GroupBy(s => s.LicenseCategory)
            .Where(g => g.Key != LicenseCategory.Unknown)
            .Select(g => new LicenseSummaryDto
            {
                Category = g.Key,
                DisplayName = g.Key.GetDisplayName(),
                ShortName = g.Key.GetShortName(),
                TierGroup = g.Key.GetTierGroup(),
                ColorHex = g.Key.GetColorHex(),
                TotalLicenses = g.Sum(s => s.PrepaidUnits),
                AssignedLicenses = g.Sum(s => s.ConsumedUnits),
                AvailableLicenses = g.Sum(s => s.AvailableUnits),
                UtilizationPercentage = g.Sum(s => s.PrepaidUnits) > 0
                    ? (double)g.Sum(s => s.ConsumedUnits) / g.Sum(s => s.PrepaidUnits) * 100
                    : 0,
                EstimatedMonthlyPricePerUser = LicenseSkuMapper.GetEstimatedMonthlyPrice(g.Key),
                TotalMonthlyCost = g.Sum(s => s.ConsumedUnits) * LicenseSkuMapper.GetEstimatedMonthlyPrice(g.Key),
                IncludedFeatures = LicenseSkuMapper.GetIncludedFeatures(g.Key).ToList(),
                IsPrimaryLicense = g.Key.IsPrimaryLicense()
            })
            .OrderByDescending(s => s.Category.GetTierRanking())
            .ThenByDescending(s => s.TotalLicenses)
            .ToList();

        var byTierGroup = byCategory
            .GroupBy(s => s.TierGroup ?? "Other")
            .Select(g => new TierGroupSummaryDto
            {
                TierGroup = g.Key,
                TotalLicenses = g.Sum(s => s.TotalLicenses),
                AssignedLicenses = g.Sum(s => s.AssignedLicenses),
                AvailableLicenses = g.Sum(s => s.AvailableLicenses),
                UtilizationPercentage = g.Sum(s => s.TotalLicenses) > 0
                    ? (double)g.Sum(s => s.AssignedLicenses) / g.Sum(s => s.TotalLicenses) * 100
                    : 0,
                TotalMonthlyCost = g.Sum(s => s.TotalMonthlyCost),
                Categories = g.Select(s => s.Category).ToList()
            })
            .OrderByDescending(g => g.TierGroup == "Enterprise" ? 100 :
                                    g.TierGroup == "Business" ? 90 :
                                    g.TierGroup == "Education" ? 80 :
                                    g.TierGroup == "Government" ? 70 :
                                    g.TierGroup == "Frontline" ? 60 : 0)
            .ToList();

        var totalLicenses = byCategory.Sum(s => s.TotalLicenses);
        var totalAssigned = byCategory.Sum(s => s.AssignedLicenses);
        var totalCost = byCategory.Sum(s => s.TotalMonthlyCost);

        // Estimate waste (simplified - users with premium licenses not using key features)
        var estimatedWaste = byCategory
            .Where(s => s.Category.GetTierRanking() >= 60) // E1 and above
            .Sum(s => s.TotalMonthlyCost * 0.2m); // Assume 20% waste as baseline

        return new LicenseDistributionDto
        {
            TotalLicenses = totalLicenses,
            TotalAssigned = totalAssigned,
            TotalAvailable = totalLicenses - totalAssigned,
            OverallUtilization = totalLicenses > 0 ? (double)totalAssigned / totalLicenses * 100 : 0,
            TotalMonthlyCost = totalCost,
            TotalEstimatedWaste = estimatedWaste,
            ByCategory = byCategory,
            ByTierGroup = byTierGroup
        };
    }

    public async Task<IReadOnlyList<LicenseSummaryDto>> GetLicenseSummariesByCategoryAsync(
        Guid tenantId,
        Guid? snapshotId = null,
        CancellationToken ct = default)
    {
        var distribution = await GetLicenseDistributionAsync(tenantId, snapshotId, ct);
        return distribution.ByCategory;
    }

    private static List<FeatureUtilizationDto> ParseFeatureBreakdown(string? json)
    {
        if (string.IsNullOrWhiteSpace(json)) return new List<FeatureUtilizationDto>();
        try
        {
            return JsonSerializer.Deserialize<List<FeatureUtilizationDto>>(json) ?? new List<FeatureUtilizationDto>();
        }
        catch
        {
            return new List<FeatureUtilizationDto>();
        }
    }

    #endregion

    #region High-Risk Findings

    public async Task<IReadOnlyList<HighRiskFindingSummaryDto>> GetHighRiskFindingsAsync(
        Guid tenantId,
        Guid? snapshotId = null,
        CancellationToken ct = default)
    {
        var query = await GetLatestSnapshotQueryAsync<HighRiskFindingInventory>(
            tenantId, InventoryDomain.HighRiskFindings, snapshotId, ct);

        var findings = await query
            .OrderByDescending(f => f.SeverityOrder)
            .ThenByDescending(f => f.AffectedCount)
            .ToListAsync(ct);

        return findings.Select(f => new HighRiskFindingSummaryDto
        {
            FindingType = f.FindingType,
            Title = f.Title,
            Severity = f.Severity,
            Category = f.Category,
            AffectedCount = f.AffectedCount,
            Remediation = f.Remediation
        }).ToList();
    }

    #endregion

    #region Change Detection

    public async Task<InventoryChangesDto> GetChangesAsync(
        Guid tenantId,
        Guid fromSnapshotId,
        Guid toSnapshotId,
        CancellationToken ct = default)
    {
        var fromSnapshot = await _dbContext.InventorySnapshots
            .FirstOrDefaultAsync(s => s.Id == fromSnapshotId, ct);
        var toSnapshot = await _dbContext.InventorySnapshots
            .FirstOrDefaultAsync(s => s.Id == toSnapshotId, ct);

        if (fromSnapshot == null || toSnapshot == null)
        {
            throw new InvalidOperationException("Snapshot not found");
        }

        // Compare snapshots and calculate changes
        var changes = new Dictionary<InventoryDomain, DomainChangesDto>();

        // This is a simplified implementation - in production, you'd compare actual inventory items
        var allDomainSnapshots = await _dbContext.InventorySnapshots
            .Where(s => s.TenantId == tenantId &&
                        (s.CollectedAt >= fromSnapshot.CollectedAt && s.CollectedAt <= toSnapshot.CollectedAt))
            .ToListAsync(ct);

        foreach (InventoryDomain domain in Enum.GetValues<InventoryDomain>())
        {
            var latestTo = allDomainSnapshots
                .Where(s => s.Domain == domain && s.CollectedAt <= toSnapshot.CollectedAt)
                .OrderByDescending(s => s.CollectedAt)
                .FirstOrDefault();

            changes[domain] = new DomainChangesDto
            {
                Domain = domain,
                ItemsAdded = latestTo?.ItemsAdded ?? 0,
                ItemsRemoved = latestTo?.ItemsRemoved ?? 0,
                ItemsModified = latestTo?.ItemsModified ?? 0
            };
        }

        return new InventoryChangesDto
        {
            FromSnapshotId = fromSnapshotId,
            ToSnapshotId = toSnapshotId,
            FromDate = fromSnapshot.CollectedAt,
            ToDate = toSnapshot.CollectedAt,
            DomainChanges = changes
        };
    }

    #endregion

    #region Export

    public async Task<byte[]> ExportInventoryAsync(
        Guid tenantId,
        InventoryExportFormat format,
        InventoryDomain? domain = null,
        Guid? snapshotId = null,
        CancellationToken ct = default)
    {
        // This is a placeholder implementation
        // In production, you'd implement proper export logic for each format
        throw new NotImplementedException("Export functionality not yet implemented");
    }

    #endregion

    #region Private Helpers

    private async Task<IQueryable<T>> GetLatestSnapshotQueryAsync<T>(
        Guid tenantId,
        InventoryDomain domain,
        Guid? snapshotId,
        CancellationToken ct) where T : class
    {
        Guid targetSnapshotId;

        if (snapshotId.HasValue)
        {
            targetSnapshotId = snapshotId.Value;
        }
        else
        {
            var latestSnapshot = await _dbContext.InventorySnapshots
                .Where(s => s.TenantId == tenantId &&
                            s.Domain == domain &&
                            s.Status == InventoryStatus.Completed)
                .OrderByDescending(s => s.CollectedAt)
                .FirstOrDefaultAsync(ct);

            targetSnapshotId = latestSnapshot?.Id ?? Guid.Empty;
        }

        // Get the DbSet for the entity type
        var dbSet = _dbContext.Set<T>();

        // Apply snapshot filter using reflection to find SnapshotId property
        var parameter = System.Linq.Expressions.Expression.Parameter(typeof(T), "e");
        var snapshotIdProperty = typeof(T).GetProperty("SnapshotId");

        if (snapshotIdProperty != null)
        {
            var propertyAccess = System.Linq.Expressions.Expression.Property(parameter, snapshotIdProperty);
            var constant = System.Linq.Expressions.Expression.Constant(targetSnapshotId);
            var equality = System.Linq.Expressions.Expression.Equal(propertyAccess, constant);
            var lambda = System.Linq.Expressions.Expression.Lambda<Func<T, bool>>(equality, parameter);

            return dbSet.Where(lambda);
        }

        return dbSet;
    }

    private static InventorySnapshotDto MapToSnapshotDto(InventorySnapshot snapshot) => new()
    {
        Id = snapshot.Id,
        TenantId = snapshot.TenantId,
        TenantName = snapshot.Tenant?.Name ?? string.Empty,
        Domain = snapshot.Domain,
        DomainDisplayName = snapshot.Domain.GetDisplayName(),
        CollectedAt = snapshot.CollectedAt,
        Status = snapshot.Status,
        ItemCount = snapshot.ItemCount,
        Duration = snapshot.Duration,
        ItemsAdded = snapshot.ItemsAdded,
        ItemsRemoved = snapshot.ItemsRemoved,
        ItemsModified = snapshot.ItemsModified,
        ErrorMessage = snapshot.ErrorMessage
    };

    private static UserInventoryDto MapToUserDto(UserInventory user) => new()
    {
        Id = user.Id,
        ObjectId = user.ObjectId,
        UserPrincipalName = user.UserPrincipalName,
        DisplayName = user.DisplayName,
        Mail = user.Mail,
        UserType = user.UserType,
        AccountEnabled = user.AccountEnabled,
        CreatedDateTime = user.CreatedDateTime,
        LastSignInDateTime = user.LastSignInDateTime,
        IsMfaRegistered = user.IsMfaRegistered,
        IsMfaCapable = user.IsMfaCapable,
        HasE5License = user.HasE5License,
        LicenseCount = user.LicenseCount,
        RiskLevel = user.RiskLevel,
        IsPrivileged = user.IsPrivileged,
        IsGlobalAdmin = user.IsGlobalAdmin,
        Department = user.Department,
        JobTitle = user.JobTitle,
        Country = user.Country,
        AssignedRoles = ParseJsonArray(user.AssignedRolesJson),
        AssignedLicenses = ParseJsonArray(user.AssignedLicensesJson),
        // License categorization
        PrimaryLicenseCategory = user.PrimaryLicenseCategory,
        PrimaryLicenseCategoryName = user.PrimaryLicenseCategory.GetDisplayName(),
        PrimaryLicenseTierGroup = user.PrimaryLicenseTierGroup,
        AllLicenseCategories = ParseJsonArray<LicenseCategory>(user.AllLicenseCategoriesJson),
        HasBusinessPremium = user.HasBusinessPremium,
        HasFrontlineLicense = user.HasFrontlineLicense
    };

    private static List<string> ParseJsonArray(string? json)
    {
        if (string.IsNullOrWhiteSpace(json)) return new List<string>();
        try
        {
            return JsonSerializer.Deserialize<List<string>>(json) ?? new List<string>();
        }
        catch
        {
            return new List<string>();
        }
    }

    private static List<T> ParseJsonArray<T>(string? json) where T : struct
    {
        if (string.IsNullOrWhiteSpace(json)) return new List<T>();
        try
        {
            return JsonSerializer.Deserialize<List<T>>(json) ?? new List<T>();
        }
        catch
        {
            return new List<T>();
        }
    }

    private LicenseSubscriptionDto MapToLicenseSubscriptionDto(LicenseSubscription s) => new()
    {
        SkuId = s.SkuId,
        SkuPartNumber = s.SkuPartNumber,
        DisplayName = s.DisplayName,
        CapabilityStatus = s.CapabilityStatus,
        PrepaidUnits = s.PrepaidUnits,
        ConsumedUnits = s.ConsumedUnits,
        AvailableUnits = s.AvailableUnits,
        IsTrial = s.IsTrial,
        ExpirationDate = s.ExpirationDate,
        LicenseCategory = s.LicenseCategory,
        LicenseCategoryDisplayName = s.LicenseCategory.GetDisplayName(),
        TierGroup = s.TierGroup,
        IsPrimaryLicense = s.IsPrimaryLicense,
        EstimatedMonthlyPricePerUser = s.EstimatedMonthlyPricePerUser,
        IncludedFeatures = LicenseSkuMapper.GetIncludedFeatures(s.LicenseCategory).ToList()
    };

    #endregion
}

