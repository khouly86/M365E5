using Cloudativ.Assessment.Domain.Enums;

namespace Cloudativ.Assessment.Domain.Entities;

/// <summary>
/// Represents a governance compliance analysis result for a specific tenant, assessment run, and compliance standard.
/// </summary>
public class GovernanceAnalysis : BaseEntity
{
    /// <summary>
    /// The tenant this analysis belongs to.
    /// </summary>
    public Guid TenantId { get; set; }

    /// <summary>
    /// The assessment run this analysis is based on.
    /// </summary>
    public Guid AssessmentRunId { get; set; }

    /// <summary>
    /// The compliance standard being analyzed.
    /// </summary>
    public ComplianceStandard Standard { get; set; }

    /// <summary>
    /// Overall compliance score (0-100).
    /// </summary>
    public int ComplianceScore { get; set; }

    /// <summary>
    /// Total number of controls evaluated.
    /// </summary>
    public int TotalControls { get; set; }

    /// <summary>
    /// Number of fully compliant controls.
    /// </summary>
    public int CompliantControls { get; set; }

    /// <summary>
    /// Number of partially compliant controls.
    /// </summary>
    public int PartiallyCompliantControls { get; set; }

    /// <summary>
    /// Number of non-compliant controls.
    /// </summary>
    public int NonCompliantControls { get; set; }

    /// <summary>
    /// JSON array of compliance gaps identified.
    /// Schema: [{ ControlId, ControlName, GapDescription, Severity, CurrentState, RequiredState }]
    /// </summary>
    public string? ComplianceGapsJson { get; set; }

    /// <summary>
    /// JSON array of recommendations for improving compliance.
    /// Schema: [{ Title, Priority, ImplementationGuidance, RelatedControlIds, EstimatedEffort }]
    /// </summary>
    public string? RecommendationsJson { get; set; }

    /// <summary>
    /// JSON array of compliant areas with supporting evidence.
    /// Schema: [{ ControlId, ControlName, ComplianceStatus, Evidence }]
    /// </summary>
    public string? CompliantAreasJson { get; set; }

    /// <summary>
    /// The AI model used for this analysis.
    /// </summary>
    public string? AiModelUsed { get; set; }

    /// <summary>
    /// Number of tokens used in the AI request/response.
    /// </summary>
    public int? TokensUsed { get; set; }

    /// <summary>
    /// Raw JSON response from the AI model (for debugging/auditing).
    /// </summary>
    public string? RawResponseJson { get; set; }

    /// <summary>
    /// The version of the compliance standard document used.
    /// </summary>
    public string? StandardDocumentVersion { get; set; }

    /// <summary>
    /// Timestamp when the analysis was completed.
    /// </summary>
    public DateTime AnalyzedAt { get; set; } = DateTime.UtcNow;

    /// <summary>
    /// Error message if analysis failed.
    /// </summary>
    public string? ErrorMessage { get; set; }

    /// <summary>
    /// Whether the analysis completed successfully.
    /// </summary>
    public bool IsSuccessful { get; set; } = true;

    // Navigation properties
    public virtual Tenant Tenant { get; set; } = null!;
    public virtual AssessmentRun AssessmentRun { get; set; } = null!;
}
