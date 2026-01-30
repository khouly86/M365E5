using Cloudativ.Assessment.Domain.Enums;

namespace Cloudativ.Assessment.Application.Interfaces;

public interface IReportService
{
    Task<byte[]> GeneratePdfReportAsync(Guid assessmentRunId, CancellationToken cancellationToken = default);
    Task<byte[]> GenerateExcelReportAsync(Guid assessmentRunId, CancellationToken cancellationToken = default);

    // Domain-specific export methods
    Task<byte[]> GenerateDomainPdfReportAsync(Guid assessmentRunId, AssessmentDomain domain, CancellationToken cancellationToken = default);
    Task<byte[]> GenerateDomainExcelReportAsync(Guid assessmentRunId, AssessmentDomain domain, CancellationToken cancellationToken = default);
}
