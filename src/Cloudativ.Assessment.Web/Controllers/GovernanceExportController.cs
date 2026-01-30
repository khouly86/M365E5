using Cloudativ.Assessment.Application.Interfaces;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Cloudativ.Assessment.Web.Controllers;

[ApiController]
[Route("api/[controller]")]
[Authorize]
public class GovernanceExportController : ControllerBase
{
    private readonly IGovernanceExportService _exportService;
    private readonly ITenantService _tenantService;
    private readonly ILogger<GovernanceExportController> _logger;

    public GovernanceExportController(
        IGovernanceExportService exportService,
        ITenantService tenantService,
        ILogger<GovernanceExportController> logger)
    {
        _exportService = exportService;
        _tenantService = tenantService;
        _logger = logger;
    }

    /// <summary>
    /// Export governance analysis to PDF
    /// </summary>
    [HttpGet("pdf/{tenantId}/{runId}")]
    public async Task<IActionResult> ExportPdf(Guid tenantId, Guid runId)
    {
        try
        {
            var tenant = await _tenantService.GetByIdAsync(tenantId);
            if (tenant == null)
            {
                return NotFound("Tenant not found");
            }

            var pdfBytes = await _exportService.ExportToPdfAsync(runId, tenant.Name);
            var fileName = $"Governance_Report_{tenant.Domain}_{DateTime.Now:yyyyMMdd_HHmmss}.pdf";

            return File(pdfBytes, "application/pdf", fileName);
        }
        catch (InvalidOperationException ex)
        {
            _logger.LogWarning(ex, "No data available for PDF export");
            return BadRequest(ex.Message);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error exporting governance analysis to PDF");
            return StatusCode(500, "Error generating PDF report");
        }
    }

    /// <summary>
    /// Export governance analysis to Excel
    /// </summary>
    [HttpGet("excel/{tenantId}/{runId}")]
    public async Task<IActionResult> ExportExcel(Guid tenantId, Guid runId)
    {
        try
        {
            var tenant = await _tenantService.GetByIdAsync(tenantId);
            if (tenant == null)
            {
                return NotFound("Tenant not found");
            }

            var excelBytes = await _exportService.ExportToExcelAsync(runId, tenant.Name);
            var fileName = $"Governance_Report_{tenant.Domain}_{DateTime.Now:yyyyMMdd_HHmmss}.xlsx";

            return File(excelBytes, "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet", fileName);
        }
        catch (InvalidOperationException ex)
        {
            _logger.LogWarning(ex, "No data available for Excel export");
            return BadRequest(ex.Message);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error exporting governance analysis to Excel");
            return StatusCode(500, "Error generating Excel report");
        }
    }

    /// <summary>
    /// Export single governance analysis to PDF
    /// </summary>
    [HttpGet("pdf/single/{tenantId}/{analysisId}")]
    public async Task<IActionResult> ExportSinglePdf(Guid tenantId, Guid analysisId)
    {
        try
        {
            var tenant = await _tenantService.GetByIdAsync(tenantId);
            if (tenant == null)
            {
                return NotFound("Tenant not found");
            }

            var pdfBytes = await _exportService.ExportSingleAnalysisToPdfAsync(analysisId, tenant.Name);
            var fileName = $"Governance_Analysis_{tenant.Domain}_{DateTime.Now:yyyyMMdd_HHmmss}.pdf";

            return File(pdfBytes, "application/pdf", fileName);
        }
        catch (InvalidOperationException ex)
        {
            _logger.LogWarning(ex, "No data available for PDF export");
            return BadRequest(ex.Message);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error exporting single governance analysis to PDF");
            return StatusCode(500, "Error generating PDF report");
        }
    }
}
