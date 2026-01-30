using Cloudativ.Assessment.Application.DTOs;

namespace Cloudativ.Assessment.Application.Interfaces;

/// <summary>
/// Service for exporting governance analysis results.
/// </summary>
public interface IGovernanceExportService
{
    /// <summary>
    /// Exports governance analysis results to PDF format.
    /// </summary>
    Task<byte[]> ExportToPdfAsync(
        Guid assessmentRunId,
        string tenantName,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Exports governance analysis results to Excel format.
    /// </summary>
    Task<byte[]> ExportToExcelAsync(
        Guid assessmentRunId,
        string tenantName,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Exports a single governance analysis to PDF format.
    /// </summary>
    Task<byte[]> ExportSingleAnalysisToPdfAsync(
        Guid analysisId,
        string tenantName,
        CancellationToken cancellationToken = default);
}
