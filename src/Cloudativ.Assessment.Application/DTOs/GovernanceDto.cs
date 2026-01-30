using Cloudativ.Assessment.Domain.Enums;

namespace Cloudativ.Assessment.Application.DTOs;

/// <summary>
/// DTO for governance compliance analysis results.
/// </summary>
public record GovernanceAnalysisDto
{
    public Guid Id { get; init; }
    public Guid TenantId { get; init; }
    public Guid AssessmentRunId { get; init; }
    public ComplianceStandard Standard { get; init; }
    public string StandardDisplayName { get; init; } = string.Empty;
    public int ComplianceScore { get; init; }
    public int TotalControls { get; init; }
    public int CompliantControls { get; init; }
    public int PartiallyCompliantControls { get; init; }
    public int NonCompliantControls { get; init; }
    public List<ComplianceGapDto> ComplianceGaps { get; init; } = new();
    public List<ComplianceRecommendationDto> Recommendations { get; init; } = new();
    public List<CompliantAreaDto> CompliantAreas { get; init; } = new();
    public string? AiModelUsed { get; init; }
    public int? TokensUsed { get; init; }
    public DateTime AnalyzedAt { get; init; }
    public bool IsSuccessful { get; init; }
    public string? ErrorMessage { get; init; }
}

/// <summary>
/// DTO for a compliance gap identified during governance analysis.
/// </summary>
public record ComplianceGapDto
{
    public string ControlId { get; init; } = string.Empty;
    public string ControlName { get; init; } = string.Empty;
    public string GapDescription { get; init; } = string.Empty;
    public string Severity { get; init; } = string.Empty; // Critical, High, Medium, Low
    public string? CurrentState { get; init; }
    public string? RequiredState { get; init; }
}

/// <summary>
/// DTO for a compliance recommendation.
/// </summary>
public record ComplianceRecommendationDto
{
    public string Title { get; init; } = string.Empty;
    public string Priority { get; init; } = string.Empty; // Critical, High, Medium, Low
    public string ImplementationGuidance { get; init; } = string.Empty;
    public List<string> RelatedControlIds { get; init; } = new();
    public string? EstimatedEffort { get; init; } // Quick Win, Short Term, Long Term
}

/// <summary>
/// DTO for a compliant area with evidence.
/// </summary>
public record CompliantAreaDto
{
    public string ControlId { get; init; } = string.Empty;
    public string ControlName { get; init; } = string.Empty;
    public string ComplianceStatus { get; init; } = string.Empty; // Compliant, Partially Compliant
    public string Evidence { get; init; } = string.Empty;
}

/// <summary>
/// Request DTO for running a governance analysis.
/// </summary>
public record RunGovernanceAnalysisRequest
{
    public Guid TenantId { get; init; }
    public Guid AssessmentRunId { get; init; }
    public List<ComplianceStandard> Standards { get; init; } = new();
}

/// <summary>
/// Summary DTO for governance analysis overview.
/// </summary>
public record GovernanceAnalysisSummaryDto
{
    public Guid TenantId { get; init; }
    public Guid AssessmentRunId { get; init; }
    public DateTime AnalyzedAt { get; init; }
    public int StandardsAnalyzed { get; init; }
    public int OverallAverageScore { get; init; }
    public int TotalGaps { get; init; }
    public int TotalRecommendations { get; init; }
    public List<StandardScoreDto> StandardScores { get; init; } = new();
}

/// <summary>
/// Score for a specific standard in a summary.
/// </summary>
public record StandardScoreDto
{
    public ComplianceStandard Standard { get; init; }
    public string StandardDisplayName { get; init; } = string.Empty;
    public int Score { get; init; }
    public int GapsCount { get; init; }
    public bool IsSuccessful { get; init; }
}

/// <summary>
/// Available compliance standard info for UI selection.
/// </summary>
public record ComplianceStandardInfoDto
{
    public ComplianceStandard Standard { get; init; }
    public string DisplayName { get; init; } = string.Empty;
    public string Description { get; init; } = string.Empty;
    public bool IsRecommended { get; init; }
    public bool IsEnabled { get; init; }
}
