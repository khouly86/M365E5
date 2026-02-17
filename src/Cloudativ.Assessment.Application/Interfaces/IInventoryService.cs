using Cloudativ.Assessment.Application.DTOs;
using Cloudativ.Assessment.Domain.Enums;
using static Cloudativ.Assessment.Domain.Enums.LicenseCategory;

namespace Cloudativ.Assessment.Application.Interfaces;

/// <summary>
/// Service for managing M365 inventory collection, querying, and export.
/// </summary>
public interface IInventoryService
{
    #region Collection

    /// <summary>
    /// Starts a new inventory collection for the specified tenant.
    /// </summary>
    Task<Guid> StartInventoryCollectionAsync(StartInventoryRequest request, CancellationToken ct = default);

    /// <summary>
    /// Gets the progress of an ongoing inventory collection.
    /// </summary>
    Task<InventoryProgressDto> GetProgressAsync(Guid snapshotId, CancellationToken ct = default);

    /// <summary>
    /// Cancels an ongoing inventory collection.
    /// </summary>
    Task CancelInventoryCollectionAsync(Guid snapshotId, CancellationToken ct = default);

    #endregion

    #region Snapshot Queries

    /// <summary>
    /// Gets inventory snapshots for a tenant.
    /// </summary>
    Task<IReadOnlyList<InventorySnapshotDto>> GetSnapshotsAsync(
        Guid tenantId,
        int take = 20,
        CancellationToken ct = default);

    /// <summary>
    /// Gets a specific snapshot by ID.
    /// </summary>
    Task<InventorySnapshotDto?> GetSnapshotAsync(Guid snapshotId, CancellationToken ct = default);

    /// <summary>
    /// Gets the latest snapshot for a tenant, optionally filtered by domain.
    /// </summary>
    Task<InventorySnapshotDto?> GetLatestSnapshotAsync(
        Guid tenantId,
        InventoryDomain? domain = null,
        CancellationToken ct = default);

    #endregion

    #region Dashboard

    /// <summary>
    /// Gets the inventory dashboard summary for a tenant.
    /// </summary>
    Task<InventoryDashboardDto> GetDashboardAsync(Guid tenantId, CancellationToken ct = default);

    #endregion

    #region Tenant Baseline

    /// <summary>
    /// Gets tenant baseline information.
    /// </summary>
    Task<TenantBaselineDto> GetTenantBaselineAsync(
        Guid tenantId,
        Guid? snapshotId = null,
        CancellationToken ct = default);

    /// <summary>
    /// Gets license subscriptions for a tenant.
    /// </summary>
    Task<IReadOnlyList<LicenseSubscriptionDto>> GetSubscriptionsAsync(
        Guid tenantId,
        Guid? snapshotId = null,
        CancellationToken ct = default);

    #endregion

    #region Identity & Access

    /// <summary>
    /// Gets users with filtering and pagination.
    /// </summary>
    Task<PagedResult<UserInventoryDto>> GetUsersAsync(
        Guid tenantId,
        UserInventoryFilter filter,
        Guid? snapshotId = null,
        CancellationToken ct = default);

    /// <summary>
    /// Gets a specific user by object ID.
    /// </summary>
    Task<UserInventoryDto?> GetUserAsync(
        Guid tenantId,
        string objectId,
        Guid? snapshotId = null,
        CancellationToken ct = default);

    /// <summary>
    /// Gets groups with filtering and pagination.
    /// </summary>
    Task<PagedResult<GroupInventoryDto>> GetGroupsAsync(
        Guid tenantId,
        GroupInventoryFilter filter,
        Guid? snapshotId = null,
        CancellationToken ct = default);

    /// <summary>
    /// Gets directory roles with member counts.
    /// </summary>
    Task<IReadOnlyList<DirectoryRoleDto>> GetDirectoryRolesAsync(
        Guid tenantId,
        Guid? snapshotId = null,
        CancellationToken ct = default);

    /// <summary>
    /// Gets conditional access policies.
    /// </summary>
    Task<IReadOnlyList<ConditionalAccessPolicyDto>> GetConditionalAccessPoliciesAsync(
        Guid tenantId,
        Guid? snapshotId = null,
        CancellationToken ct = default);

    #endregion

    #region Devices

    /// <summary>
    /// Gets devices with filtering and pagination.
    /// </summary>
    Task<PagedResult<DeviceInventoryDto>> GetDevicesAsync(
        Guid tenantId,
        DeviceInventoryFilter filter,
        Guid? snapshotId = null,
        CancellationToken ct = default);

    #endregion

    #region Defender XDR

    /// <summary>
    /// Gets Defender for Endpoint inventory.
    /// </summary>
    Task<DefenderForEndpointDto?> GetDefenderForEndpointAsync(
        Guid tenantId,
        Guid? snapshotId = null,
        CancellationToken ct = default);

    /// <summary>
    /// Gets Defender for Office 365 inventory.
    /// </summary>
    Task<DefenderForOffice365Dto?> GetDefenderForOffice365Async(
        Guid tenantId,
        Guid? snapshotId = null,
        CancellationToken ct = default);

    /// <summary>
    /// Gets Defender for Identity inventory.
    /// </summary>
    Task<DefenderForIdentityDto?> GetDefenderForIdentityAsync(
        Guid tenantId,
        Guid? snapshotId = null,
        CancellationToken ct = default);

    /// <summary>
    /// Gets Defender for Cloud Apps inventory.
    /// </summary>
    Task<DefenderForCloudAppsDto?> GetDefenderForCloudAppsAsync(
        Guid tenantId,
        Guid? snapshotId = null,
        CancellationToken ct = default);

    #endregion

    #region Applications

    /// <summary>
    /// Gets enterprise apps with filtering and pagination.
    /// </summary>
    Task<PagedResult<EnterpriseAppDto>> GetAppsAsync(
        Guid tenantId,
        AppInventoryFilter filter,
        Guid? snapshotId = null,
        CancellationToken ct = default);

    /// <summary>
    /// Gets service principals.
    /// </summary>
    Task<IReadOnlyList<ServicePrincipalDto>> GetServicePrincipalsAsync(
        Guid tenantId,
        Guid? snapshotId = null,
        CancellationToken ct = default);

    #endregion

    #region License Utilization

    /// <summary>
    /// Gets E5 license utilization metrics (legacy - for backwards compatibility).
    /// </summary>
    Task<LicenseUtilizationDto> GetLicenseUtilizationAsync(
        Guid tenantId,
        Guid? snapshotId = null,
        CancellationToken ct = default);

    /// <summary>
    /// Gets all license subscriptions for a tenant.
    /// </summary>
    Task<IReadOnlyList<LicenseSubscriptionDto>> GetLicenseSubscriptionsAsync(
        Guid tenantId,
        Guid? snapshotId = null,
        CancellationToken ct = default);

    /// <summary>
    /// Gets multi-license utilization metrics across all license categories.
    /// </summary>
    Task<MultiLicenseUtilizationDto> GetMultiLicenseUtilizationAsync(
        Guid tenantId,
        Guid? snapshotId = null,
        CancellationToken ct = default);

    /// <summary>
    /// Gets license utilization metrics for a specific license category.
    /// </summary>
    Task<LicenseUtilizationByTypeDto?> GetLicenseUtilizationByCategoryAsync(
        Guid tenantId,
        LicenseCategory category,
        Guid? snapshotId = null,
        CancellationToken ct = default);

    /// <summary>
    /// Gets license distribution summary across all categories.
    /// </summary>
    Task<LicenseDistributionDto> GetLicenseDistributionAsync(
        Guid tenantId,
        Guid? snapshotId = null,
        CancellationToken ct = default);

    /// <summary>
    /// Gets license summaries grouped by category.
    /// </summary>
    Task<IReadOnlyList<LicenseSummaryDto>> GetLicenseSummariesByCategoryAsync(
        Guid tenantId,
        Guid? snapshotId = null,
        CancellationToken ct = default);

    #endregion

    #region Collaboration

    /// <summary>
    /// Gets SharePoint sites.
    /// </summary>
    Task<PagedResult<SharePointSiteDto>> GetSharePointSitesAsync(
        Guid tenantId,
        SharePointSiteFilter filter,
        Guid? snapshotId = null,
        CancellationToken ct = default);

    /// <summary>
    /// Gets Teams.
    /// </summary>
    Task<PagedResult<TeamsDto>> GetTeamsAsync(
        Guid tenantId,
        TeamsFilter filter,
        Guid? snapshotId = null,
        CancellationToken ct = default);

    #endregion

    #region High-Risk Findings

    /// <summary>
    /// Gets high-risk findings for a tenant.
    /// </summary>
    Task<IReadOnlyList<HighRiskFindingSummaryDto>> GetHighRiskFindingsAsync(
        Guid tenantId,
        Guid? snapshotId = null,
        CancellationToken ct = default);

    #endregion

    #region Change Detection

    /// <summary>
    /// Gets changes between two inventory snapshots.
    /// </summary>
    Task<InventoryChangesDto> GetChangesAsync(
        Guid tenantId,
        Guid fromSnapshotId,
        Guid toSnapshotId,
        CancellationToken ct = default);

    #endregion

    #region Export

    /// <summary>
    /// Exports inventory data in the specified format.
    /// </summary>
    Task<byte[]> ExportInventoryAsync(
        Guid tenantId,
        InventoryExportFormat format,
        InventoryDomain? domain = null,
        Guid? snapshotId = null,
        CancellationToken ct = default);

    #endregion
}
