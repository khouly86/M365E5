using Cloudativ.Assessment.Application.DTOs;
using Cloudativ.Assessment.Domain.Enums;

namespace Cloudativ.Assessment.Application.Interfaces;

/// <summary>
/// Service for running and managing governance compliance analyses.
/// </summary>
public interface IGovernanceService
{
    /// <summary>
    /// Runs a compliance analysis for a single standard.
    /// </summary>
    Task<GovernanceAnalysisDto> RunAnalysisAsync(
        Guid tenantId,
        Guid assessmentRunId,
        ComplianceStandard standard,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Runs compliance analyses for multiple standards concurrently.
    /// </summary>
    Task<IReadOnlyList<GovernanceAnalysisDto>> RunMultipleAnalysesAsync(
        Guid tenantId,
        Guid assessmentRunId,
        IEnumerable<ComplianceStandard> standards,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Gets a specific governance analysis by ID.
    /// </summary>
    Task<GovernanceAnalysisDto?> GetAnalysisAsync(
        Guid analysisId,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Gets all governance analyses for a specific assessment run.
    /// </summary>
    Task<IReadOnlyList<GovernanceAnalysisDto>> GetAnalysesByRunAsync(
        Guid assessmentRunId,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Gets all governance analyses for a specific tenant.
    /// </summary>
    Task<IReadOnlyList<GovernanceAnalysisDto>> GetAnalysesByTenantAsync(
        Guid tenantId,
        int take = 50,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Gets a summary of governance analyses for an assessment run.
    /// </summary>
    Task<GovernanceAnalysisSummaryDto?> GetAnalysisSummaryAsync(
        Guid assessmentRunId,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Gets the recommended compliance standards for an industry.
    /// </summary>
    Task<IReadOnlyList<ComplianceStandardInfoDto>> GetRecommendedStandardsForIndustryAsync(
        string? industry,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Gets all available compliance standards with their enabled status.
    /// </summary>
    Task<IReadOnlyList<ComplianceStandardInfoDto>> GetAvailableStandardsAsync(
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Checks if OpenAI integration is enabled and configured.
    /// </summary>
    Task<bool> IsOpenAiEnabledAsync(CancellationToken cancellationToken = default);

    /// <summary>
    /// Deletes a governance analysis.
    /// </summary>
    Task DeleteAnalysisAsync(
        Guid analysisId,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Deletes all governance analyses for an assessment run.
    /// </summary>
    Task DeleteAnalysesByRunAsync(
        Guid assessmentRunId,
        CancellationToken cancellationToken = default);
}
