using Cloudativ.Assessment.Domain.Enums;

namespace Cloudativ.Assessment.Application.DTOs;

public record AssessmentRunDto
{
    public Guid Id { get; init; }
    public Guid TenantId { get; init; }
    public string TenantName { get; init; } = string.Empty;
    public DateTime StartedAt { get; init; }
    public DateTime? CompletedAt { get; init; }
    public AssessmentStatus Status { get; init; }
    public string? InitiatedBy { get; init; }
    public int? OverallScore { get; init; }
    public Dictionary<string, DomainScoreSummary> DomainScores { get; init; } = new();
    public string? ErrorMessage { get; init; }
    public TimeSpan? Duration => CompletedAt.HasValue ? CompletedAt.Value - StartedAt : null;
}

public record DomainScoreSummary
{
    public AssessmentDomain Domain { get; init; }
    public string DisplayName { get; init; } = string.Empty;
    public int Score { get; init; }
    public string Grade { get; init; } = "N/A";
    public int CriticalCount { get; init; }
    public int HighCount { get; init; }
    public int MediumCount { get; init; }
    public int LowCount { get; init; }
    public int PassedChecks { get; init; }
    public int FailedChecks { get; init; }
    public bool IsAvailable { get; init; } = true;
    public string? UnavailableReason { get; init; }
}

public record FindingDto
{
    public Guid Id { get; init; }
    public Guid AssessmentRunId { get; init; }
    public AssessmentDomain Domain { get; init; }
    public string DomainDisplayName { get; init; } = string.Empty;
    public Severity Severity { get; init; }
    public string Title { get; init; } = string.Empty;
    public string Description { get; init; } = string.Empty;
    public string? Category { get; init; }
    public string? Evidence { get; init; }
    public string? Remediation { get; init; }
    public string? References { get; init; }
    public List<string> AffectedResources { get; init; } = new();
    public bool IsCompliant { get; init; }
    public string? CheckId { get; init; }
    public DateTime CreatedAt { get; init; }
}

public record StartAssessmentRequest
{
    public Guid TenantId { get; init; }
    public List<AssessmentDomain>? DomainsToAssess { get; init; }
    public string? InitiatedBy { get; init; }
    public bool UseAiScoring { get; init; } = false;
}

public record AssessmentProgressDto
{
    public Guid RunId { get; init; }
    public AssessmentStatus Status { get; init; }
    public int ProgressPercentage { get; init; }
    public string CurrentOperation { get; init; } = string.Empty;
    public List<string> CompletedDomains { get; init; } = new();
    public List<string> PendingDomains { get; init; } = new();
    public string? CurrentDomain { get; init; }
    public List<string> Errors { get; init; } = new();
}
