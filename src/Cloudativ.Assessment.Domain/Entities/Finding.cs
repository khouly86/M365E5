using Cloudativ.Assessment.Domain.Enums;

namespace Cloudativ.Assessment.Domain.Entities;

public class Finding : BaseEntity
{
    public Guid AssessmentRunId { get; set; }
    public AssessmentDomain Domain { get; set; }
    public Severity Severity { get; set; }
    public string Title { get; set; } = string.Empty;
    public string Description { get; set; } = string.Empty;
    public string? Category { get; set; }
    public string? EvidenceJson { get; set; }
    public string? Remediation { get; set; }
    public string? References { get; set; }
    public string? AffectedResources { get; set; }
    public bool IsCompliant { get; set; }

    // For tracking
    public string? CheckId { get; set; }
    public string? CheckName { get; set; }

    // Navigation properties
    public virtual AssessmentRun AssessmentRun { get; set; } = null!;
}
