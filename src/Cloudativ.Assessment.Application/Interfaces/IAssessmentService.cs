using Cloudativ.Assessment.Application.DTOs;
using Cloudativ.Assessment.Domain.Enums;

namespace Cloudativ.Assessment.Application.Interfaces;

public interface IAssessmentService
{
    Task<Guid> StartAssessmentAsync(StartAssessmentRequest request, CancellationToken cancellationToken = default);
    Task<AssessmentRunDto?> GetRunByIdAsync(Guid runId, CancellationToken cancellationToken = default);
    Task<IReadOnlyList<AssessmentRunDto>> GetAllRunsAsync(int take = 50, CancellationToken cancellationToken = default);
    Task<IReadOnlyList<AssessmentRunDto>> GetRunsByTenantAsync(Guid tenantId, int take = 10, CancellationToken cancellationToken = default);
    Task<AssessmentProgressDto> GetProgressAsync(Guid runId, CancellationToken cancellationToken = default);
    Task CancelAssessmentAsync(Guid runId, CancellationToken cancellationToken = default);
    Task<IReadOnlyList<FindingDto>> GetFindingsAsync(Guid runId, AssessmentDomain? domain = null, Severity? severity = null, CancellationToken cancellationToken = default);
    Task<DomainDetailDto> GetDomainDetailAsync(Guid runId, AssessmentDomain domain, CancellationToken cancellationToken = default);
    Task<byte[]> ExportReportAsync(Guid runId, ReportFormat format, CancellationToken cancellationToken = default);
    Task<byte[]> ExportDomainReportAsync(Guid runId, AssessmentDomain domain, ReportFormat format, CancellationToken cancellationToken = default);
}

public enum ReportFormat
{
    Pdf,
    Html,
    Json,
    Csv
}
