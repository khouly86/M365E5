using Cloudativ.Assessment.Application.DTOs;
using Cloudativ.Assessment.Domain.Enums;

namespace Cloudativ.Assessment.Application.Interfaces;

public interface IInventoryExportService
{
    // Users
    Task<byte[]> ExportUsersToExcelAsync(Guid tenantId, UserInventoryFilter filter, CancellationToken ct = default);
    Task<byte[]> ExportUsersToPdfAsync(Guid tenantId, UserInventoryFilter filter, string tenantName, CancellationToken ct = default);

    // Groups
    Task<byte[]> ExportGroupsToExcelAsync(Guid tenantId, GroupInventoryFilter filter, CancellationToken ct = default);
    Task<byte[]> ExportGroupsToPdfAsync(Guid tenantId, GroupInventoryFilter filter, string tenantName, CancellationToken ct = default);

    // Devices
    Task<byte[]> ExportDevicesToExcelAsync(Guid tenantId, DeviceInventoryFilter filter, CancellationToken ct = default);
    Task<byte[]> ExportDevicesToPdfAsync(Guid tenantId, DeviceInventoryFilter filter, string tenantName, CancellationToken ct = default);

    // Applications
    Task<byte[]> ExportAppsToExcelAsync(Guid tenantId, AppInventoryFilter filter, CancellationToken ct = default);
    Task<byte[]> ExportAppsToPdfAsync(Guid tenantId, AppInventoryFilter filter, string tenantName, CancellationToken ct = default);

    // Directory Roles
    Task<byte[]> ExportRolesToExcelAsync(Guid tenantId, Guid? snapshotId, CancellationToken ct = default);
    Task<byte[]> ExportRolesToPdfAsync(Guid tenantId, Guid? snapshotId, string tenantName, CancellationToken ct = default);

    // Conditional Access
    Task<byte[]> ExportCAPolicesToExcelAsync(Guid tenantId, Guid? snapshotId, CancellationToken ct = default);
    Task<byte[]> ExportCAPoliciesToPdfAsync(Guid tenantId, Guid? snapshotId, string tenantName, CancellationToken ct = default);

    // Service Principals
    Task<byte[]> ExportServicePrincipalsToExcelAsync(Guid tenantId, Guid? snapshotId, CancellationToken ct = default);
    Task<byte[]> ExportServicePrincipalsToPdfAsync(Guid tenantId, Guid? snapshotId, string tenantName, CancellationToken ct = default);
}
