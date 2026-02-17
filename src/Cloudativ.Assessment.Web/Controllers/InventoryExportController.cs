using Cloudativ.Assessment.Application.DTOs;
using Cloudativ.Assessment.Application.Interfaces;
using Cloudativ.Assessment.Domain.Enums;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Cloudativ.Assessment.Web.Controllers;

[ApiController]
[Route("api/inventory/export")]
[Authorize]
public class InventoryExportController : ControllerBase
{
    private readonly IInventoryExportService _exportService;
    private readonly ITenantService _tenantService;
    private readonly ILogger<InventoryExportController> _logger;

    public InventoryExportController(
        IInventoryExportService exportService,
        ITenantService tenantService,
        ILogger<InventoryExportController> logger)
    {
        _exportService = exportService;
        _tenantService = tenantService;
        _logger = logger;
    }

    #region Users

    [HttpGet("users/{tenantId}/excel")]
    public async Task<IActionResult> ExportUsersToExcel(
        Guid tenantId,
        [FromQuery] string? searchTerm = null,
        [FromQuery] string? userType = null,
        [FromQuery] bool? mfaEnabled = null,
        [FromQuery] string? riskLevel = null,
        [FromQuery] bool? isPrivileged = null,
        [FromQuery] LicenseCategory? licenseCategory = null,
        [FromQuery] string? tierGroup = null,
        [FromQuery] bool? hasAnyLicense = null,
        CancellationToken ct = default)
    {
        try
        {
            var filter = new UserInventoryFilter
            {
                TenantId = tenantId,
                SearchTerm = searchTerm,
                UserType = userType,
                MfaEnabled = mfaEnabled,
                RiskLevel = riskLevel,
                IsPrivileged = isPrivileged,
                LicenseCategory = licenseCategory,
                TierGroup = tierGroup,
                HasAnyLicense = hasAnyLicense
            };

            var bytes = await _exportService.ExportUsersToExcelAsync(tenantId, filter, ct);
            return File(bytes, "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet",
                $"Users_{DateTime.UtcNow:yyyyMMdd_HHmmss}.xlsx");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error exporting users to Excel");
            return StatusCode(500, "Export failed");
        }
    }

    [HttpGet("users/{tenantId}/pdf")]
    public async Task<IActionResult> ExportUsersToPdf(
        Guid tenantId,
        [FromQuery] string? searchTerm = null,
        [FromQuery] string? userType = null,
        [FromQuery] bool? mfaEnabled = null,
        [FromQuery] string? riskLevel = null,
        [FromQuery] bool? isPrivileged = null,
        [FromQuery] LicenseCategory? licenseCategory = null,
        [FromQuery] string? tierGroup = null,
        [FromQuery] bool? hasAnyLicense = null,
        CancellationToken ct = default)
    {
        try
        {
            var tenant = await _tenantService.GetByIdAsync(tenantId);
            var filter = new UserInventoryFilter
            {
                TenantId = tenantId,
                SearchTerm = searchTerm,
                UserType = userType,
                MfaEnabled = mfaEnabled,
                RiskLevel = riskLevel,
                IsPrivileged = isPrivileged,
                LicenseCategory = licenseCategory,
                TierGroup = tierGroup,
                HasAnyLicense = hasAnyLicense
            };

            var bytes = await _exportService.ExportUsersToPdfAsync(tenantId, filter, tenant?.Name ?? "Unknown", ct);
            return File(bytes, "application/pdf", $"Users_{DateTime.UtcNow:yyyyMMdd_HHmmss}.pdf");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error exporting users to PDF");
            return StatusCode(500, "Export failed");
        }
    }

    #endregion

    #region Groups

    [HttpGet("groups/{tenantId}/excel")]
    public async Task<IActionResult> ExportGroupsToExcel(
        Guid tenantId,
        [FromQuery] string? searchTerm = null,
        [FromQuery] string? groupType = null,
        CancellationToken ct = default)
    {
        try
        {
            var filter = new GroupInventoryFilter
            {
                TenantId = tenantId,
                SearchTerm = searchTerm,
                GroupType = groupType
            };

            var bytes = await _exportService.ExportGroupsToExcelAsync(tenantId, filter, ct);
            return File(bytes, "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet",
                $"Groups_{DateTime.UtcNow:yyyyMMdd_HHmmss}.xlsx");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error exporting groups to Excel");
            return StatusCode(500, "Export failed");
        }
    }

    [HttpGet("groups/{tenantId}/pdf")]
    public async Task<IActionResult> ExportGroupsToPdf(
        Guid tenantId,
        [FromQuery] string? searchTerm = null,
        [FromQuery] string? groupType = null,
        CancellationToken ct = default)
    {
        try
        {
            var tenant = await _tenantService.GetByIdAsync(tenantId);
            var filter = new GroupInventoryFilter
            {
                TenantId = tenantId,
                SearchTerm = searchTerm,
                GroupType = groupType
            };

            var bytes = await _exportService.ExportGroupsToPdfAsync(tenantId, filter, tenant?.Name ?? "Unknown", ct);
            return File(bytes, "application/pdf", $"Groups_{DateTime.UtcNow:yyyyMMdd_HHmmss}.pdf");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error exporting groups to PDF");
            return StatusCode(500, "Export failed");
        }
    }

    #endregion

    #region Devices

    [HttpGet("devices/{tenantId}/excel")]
    public async Task<IActionResult> ExportDevicesToExcel(
        Guid tenantId,
        [FromQuery] string? searchTerm = null,
        [FromQuery] string? operatingSystem = null,
        [FromQuery] string? complianceState = null,
        CancellationToken ct = default)
    {
        try
        {
            var filter = new DeviceInventoryFilter
            {
                TenantId = tenantId,
                SearchTerm = searchTerm,
                OperatingSystem = operatingSystem,
                ComplianceState = complianceState
            };

            var bytes = await _exportService.ExportDevicesToExcelAsync(tenantId, filter, ct);
            return File(bytes, "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet",
                $"Devices_{DateTime.UtcNow:yyyyMMdd_HHmmss}.xlsx");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error exporting devices to Excel");
            return StatusCode(500, "Export failed");
        }
    }

    [HttpGet("devices/{tenantId}/pdf")]
    public async Task<IActionResult> ExportDevicesToPdf(
        Guid tenantId,
        [FromQuery] string? searchTerm = null,
        [FromQuery] string? operatingSystem = null,
        [FromQuery] string? complianceState = null,
        CancellationToken ct = default)
    {
        try
        {
            var tenant = await _tenantService.GetByIdAsync(tenantId);
            var filter = new DeviceInventoryFilter
            {
                TenantId = tenantId,
                SearchTerm = searchTerm,
                OperatingSystem = operatingSystem,
                ComplianceState = complianceState
            };

            var bytes = await _exportService.ExportDevicesToPdfAsync(tenantId, filter, tenant?.Name ?? "Unknown", ct);
            return File(bytes, "application/pdf", $"Devices_{DateTime.UtcNow:yyyyMMdd_HHmmss}.pdf");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error exporting devices to PDF");
            return StatusCode(500, "Export failed");
        }
    }

    #endregion

    #region Applications

    [HttpGet("apps/{tenantId}/excel")]
    public async Task<IActionResult> ExportAppsToExcel(
        Guid tenantId,
        [FromQuery] string? searchTerm = null,
        CancellationToken ct = default)
    {
        try
        {
            var filter = new AppInventoryFilter
            {
                TenantId = tenantId,
                SearchTerm = searchTerm
            };

            var bytes = await _exportService.ExportAppsToExcelAsync(tenantId, filter, ct);
            return File(bytes, "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet",
                $"Applications_{DateTime.UtcNow:yyyyMMdd_HHmmss}.xlsx");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error exporting apps to Excel");
            return StatusCode(500, "Export failed");
        }
    }

    [HttpGet("apps/{tenantId}/pdf")]
    public async Task<IActionResult> ExportAppsToPdf(
        Guid tenantId,
        [FromQuery] string? searchTerm = null,
        CancellationToken ct = default)
    {
        try
        {
            var tenant = await _tenantService.GetByIdAsync(tenantId);
            var filter = new AppInventoryFilter
            {
                TenantId = tenantId,
                SearchTerm = searchTerm
            };

            var bytes = await _exportService.ExportAppsToPdfAsync(tenantId, filter, tenant?.Name ?? "Unknown", ct);
            return File(bytes, "application/pdf", $"Applications_{DateTime.UtcNow:yyyyMMdd_HHmmss}.pdf");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error exporting apps to PDF");
            return StatusCode(500, "Export failed");
        }
    }

    #endregion

    #region Roles

    [HttpGet("roles/{tenantId}/excel")]
    public async Task<IActionResult> ExportRolesToExcel(Guid tenantId, CancellationToken ct = default)
    {
        try
        {
            var bytes = await _exportService.ExportRolesToExcelAsync(tenantId, null, ct);
            return File(bytes, "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet",
                $"DirectoryRoles_{DateTime.UtcNow:yyyyMMdd_HHmmss}.xlsx");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error exporting roles to Excel");
            return StatusCode(500, "Export failed");
        }
    }

    [HttpGet("roles/{tenantId}/pdf")]
    public async Task<IActionResult> ExportRolesToPdf(Guid tenantId, CancellationToken ct = default)
    {
        try
        {
            var tenant = await _tenantService.GetByIdAsync(tenantId);
            var bytes = await _exportService.ExportRolesToPdfAsync(tenantId, null, tenant?.Name ?? "Unknown", ct);
            return File(bytes, "application/pdf", $"DirectoryRoles_{DateTime.UtcNow:yyyyMMdd_HHmmss}.pdf");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error exporting roles to PDF");
            return StatusCode(500, "Export failed");
        }
    }

    #endregion

    #region Conditional Access

    [HttpGet("ca-policies/{tenantId}/excel")]
    public async Task<IActionResult> ExportCAPoliciesToExcel(Guid tenantId, CancellationToken ct = default)
    {
        try
        {
            var bytes = await _exportService.ExportCAPolicesToExcelAsync(tenantId, null, ct);
            return File(bytes, "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet",
                $"CAPolicies_{DateTime.UtcNow:yyyyMMdd_HHmmss}.xlsx");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error exporting CA policies to Excel");
            return StatusCode(500, "Export failed");
        }
    }

    [HttpGet("ca-policies/{tenantId}/pdf")]
    public async Task<IActionResult> ExportCAPoliciesToPdf(Guid tenantId, CancellationToken ct = default)
    {
        try
        {
            var tenant = await _tenantService.GetByIdAsync(tenantId);
            var bytes = await _exportService.ExportCAPoliciesToPdfAsync(tenantId, null, tenant?.Name ?? "Unknown", ct);
            return File(bytes, "application/pdf", $"CAPolicies_{DateTime.UtcNow:yyyyMMdd_HHmmss}.pdf");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error exporting CA policies to PDF");
            return StatusCode(500, "Export failed");
        }
    }

    #endregion

    #region Service Principals

    [HttpGet("service-principals/{tenantId}/excel")]
    public async Task<IActionResult> ExportServicePrincipalsToExcel(Guid tenantId, CancellationToken ct = default)
    {
        try
        {
            var bytes = await _exportService.ExportServicePrincipalsToExcelAsync(tenantId, null, ct);
            return File(bytes, "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet",
                $"ServicePrincipals_{DateTime.UtcNow:yyyyMMdd_HHmmss}.xlsx");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error exporting service principals to Excel");
            return StatusCode(500, "Export failed");
        }
    }

    [HttpGet("service-principals/{tenantId}/pdf")]
    public async Task<IActionResult> ExportServicePrincipalsToPdf(Guid tenantId, CancellationToken ct = default)
    {
        try
        {
            var tenant = await _tenantService.GetByIdAsync(tenantId);
            var bytes = await _exportService.ExportServicePrincipalsToPdfAsync(tenantId, null, tenant?.Name ?? "Unknown", ct);
            return File(bytes, "application/pdf", $"ServicePrincipals_{DateTime.UtcNow:yyyyMMdd_HHmmss}.pdf");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error exporting service principals to PDF");
            return StatusCode(500, "Export failed");
        }
    }

    #endregion
}
