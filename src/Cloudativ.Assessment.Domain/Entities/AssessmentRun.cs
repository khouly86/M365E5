using Cloudativ.Assessment.Domain.Enums;

namespace Cloudativ.Assessment.Domain.Entities;

public class AssessmentRun : BaseEntity
{
    public Guid TenantId { get; set; }
    public DateTime StartedAt { get; set; } = DateTime.UtcNow;
    public DateTime? CompletedAt { get; set; }
    public AssessmentStatus Status { get; set; } = AssessmentStatus.Pending;
    public string? InitiatedBy { get; set; }

    // Summary scores per domain (JSON)
    public string? SummaryScoresJson { get; set; }

    // Overall score (0-100)
    public int? OverallScore { get; set; }

    // AI-generated maturity assessment (optional)
    public string? AiAnalysisJson { get; set; }

    // Error details if failed
    public string? ErrorMessage { get; set; }

    // Domains that were assessed
    public string? AssessedDomainsJson { get; set; }

    // Navigation properties
    public virtual Tenant Tenant { get; set; } = null!;
    public virtual ICollection<Finding> Findings { get; set; } = new List<Finding>();
    public virtual ICollection<RawSnapshot> RawSnapshots { get; set; } = new List<RawSnapshot>();
}
