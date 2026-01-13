using Cloudativ.Assessment.Domain.Enums;

namespace Cloudativ.Assessment.Domain.Interfaces;

public interface IAssessmentModule
{
    AssessmentDomain Domain { get; }
    string DisplayName { get; }
    string Description { get; }
    IReadOnlyList<string> RequiredPermissions { get; }

    Task<CollectionResult> CollectAsync(IGraphClientWrapper graphClient, CancellationToken cancellationToken = default);
    NormalizedFindings Normalize(CollectionResult rawData);
    DomainScore Score(NormalizedFindings findings);
    Task<bool> ValidatePermissionsAsync(IGraphClientWrapper graphClient, CancellationToken cancellationToken = default);
}

public class CollectionResult
{
    public AssessmentDomain Domain { get; set; }
    public bool Success { get; set; }
    public string? ErrorMessage { get; set; }
    public Dictionary<string, object?> RawData { get; set; } = new();
    public DateTime CollectedAt { get; set; } = DateTime.UtcNow;
    public List<string> Warnings { get; set; } = new();
    public List<string> UnavailableEndpoints { get; set; } = new();
}

public class NormalizedFindings
{
    public AssessmentDomain Domain { get; set; }
    public List<NormalizedFinding> Findings { get; set; } = new();
    public Dictionary<string, object?> Metrics { get; set; } = new();
    public List<string> Summary { get; set; } = new();
}

public class NormalizedFinding
{
    public string CheckId { get; set; } = string.Empty;
    public string CheckName { get; set; } = string.Empty;
    public string Title { get; set; } = string.Empty;
    public string Description { get; set; } = string.Empty;
    public Severity Severity { get; set; }
    public bool IsCompliant { get; set; }
    public string? Category { get; set; }
    public string? Evidence { get; set; }
    public string? Remediation { get; set; }
    public string? References { get; set; }
    public List<string> AffectedResources { get; set; } = new();
}

public class DomainScore
{
    public AssessmentDomain Domain { get; set; }
    public int Score { get; set; }
    public int MaxScore { get; set; } = 100;
    public string Grade { get; set; } = "N/A";
    public int CriticalCount { get; set; }
    public int HighCount { get; set; }
    public int MediumCount { get; set; }
    public int LowCount { get; set; }
    public int PassedChecks { get; set; }
    public int FailedChecks { get; set; }
    public int TotalChecks { get; set; }
    public List<string> TopRecommendations { get; set; } = new();

    public static string CalculateGrade(int score) => score switch
    {
        >= 90 => "A",
        >= 80 => "B",
        >= 70 => "C",
        >= 60 => "D",
        _ => "F"
    };
}
