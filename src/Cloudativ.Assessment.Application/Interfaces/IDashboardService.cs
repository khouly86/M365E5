using Cloudativ.Assessment.Application.DTOs;

namespace Cloudativ.Assessment.Application.Interfaces;

public interface IDashboardService
{
    Task<DashboardSummaryDto> GetSummaryAsync(CancellationToken cancellationToken = default);
    Task<TenantDashboardDto> GetTenantDashboardAsync(Guid tenantId, CancellationToken cancellationToken = default);
    Task<List<ScoreTrendPoint>> GetScoreTrendAsync(Guid tenantId, int monthsBack = 12, CancellationToken cancellationToken = default);
    Task<FindingsSummaryDto> GetFindingsSummaryAsync(Guid runId, CancellationToken cancellationToken = default);
    Task<ResourceStatisticsDto?> GetResourceStatisticsAsync(Guid tenantId, CancellationToken cancellationToken = default);
    Task<ResourceStatisticsDto> GetAggregatedResourceStatisticsAsync(CancellationToken cancellationToken = default);
}
