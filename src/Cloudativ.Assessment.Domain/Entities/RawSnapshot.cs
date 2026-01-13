using Cloudativ.Assessment.Domain.Enums;

namespace Cloudativ.Assessment.Domain.Entities;

public class RawSnapshot : BaseEntity
{
    public Guid AssessmentRunId { get; set; }
    public AssessmentDomain Domain { get; set; }
    public string DataType { get; set; } = string.Empty;
    public string PayloadJson { get; set; } = string.Empty;
    public DateTime FetchedAt { get; set; } = DateTime.UtcNow;
    public long PayloadSizeBytes { get; set; }
    public bool IsCompressed { get; set; }
    public string? ErrorMessage { get; set; }

    // Navigation properties
    public virtual AssessmentRun AssessmentRun { get; set; } = null!;
}
