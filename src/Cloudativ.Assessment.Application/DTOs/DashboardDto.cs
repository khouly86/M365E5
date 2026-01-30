using Cloudativ.Assessment.Domain.Enums;

namespace Cloudativ.Assessment.Application.DTOs;

public record DashboardSummaryDto
{
    public int TotalTenants { get; init; }
    public int ActiveTenants { get; init; }
    public int PendingOnboarding { get; init; }
    public int TotalAssessments { get; init; }
    public int AssessmentsThisMonth { get; init; }
    public double AverageScore { get; init; }
    public int CriticalFindingsCount { get; init; }
    public int HighFindingsCount { get; init; }
    public List<TenantScoreDto> TopTenants { get; init; } = new();
    public List<TenantScoreDto> LowestTenants { get; init; } = new();
    public List<RecentAssessmentDto> RecentAssessments { get; init; } = new();
}

public record TenantScoreDto
{
    public Guid TenantId { get; init; }
    public string TenantName { get; init; } = string.Empty;
    public int Score { get; init; }
    public string Grade { get; init; } = "N/A";
    public DateTime? LastAssessment { get; init; }
    public int TrendDirection { get; init; } // -1 down, 0 same, 1 up
}

public record RecentAssessmentDto
{
    public Guid RunId { get; init; }
    public Guid TenantId { get; init; }
    public string TenantName { get; init; } = string.Empty;
    public DateTime CompletedAt { get; init; }
    public int Score { get; init; }
    public AssessmentStatus Status { get; init; }
}

public record TenantDashboardDto
{
    public TenantDto Tenant { get; init; } = null!;
    public AssessmentRunDto? LatestAssessment { get; init; }
    public List<AssessmentRunDto> RecentAssessments { get; init; } = new();
    public List<ScoreTrendPoint> ScoreTrend { get; init; } = new();
    public FindingsSummaryDto FindingsSummary { get; init; } = new();
    public Dictionary<AssessmentDomain, DomainScoreSummary> DomainBreakdown { get; init; } = new();
}

public record ScoreTrendPoint
{
    public DateTime Date { get; init; }
    public int Score { get; init; }
    public string Label { get; init; } = string.Empty;
}

public record FindingsSummaryDto
{
    public int TotalFindings { get; init; }
    public int CriticalCount { get; init; }
    public int HighCount { get; init; }
    public int MediumCount { get; init; }
    public int LowCount { get; init; }
    public int InformationalCount { get; init; }
    public int CompliantCount { get; init; }
    public int NonCompliantCount { get; init; }
    public Dictionary<AssessmentDomain, int> FindingsByDomain { get; init; } = new();
    public List<FindingDto> TopCriticalFindings { get; init; } = new();
}

public record DomainDetailDto
{
    public AssessmentDomain Domain { get; init; }
    public string DisplayName { get; init; } = string.Empty;
    public string Description { get; init; } = string.Empty;
    public DomainScoreSummary Score { get; init; } = null!;
    public List<FindingDto> Findings { get; init; } = new();
    public Dictionary<string, object?> Metrics { get; init; } = new();
    public string? RawDataJson { get; init; }
    public List<string> Recommendations { get; init; } = new();
    public bool IsAvailable { get; init; } = true;
    public string? UnavailableReason { get; init; }
}

public record ResourceStatisticsDto
{
    public Guid? TenantId { get; init; }
    public string TenantName { get; init; } = string.Empty;
    public DateTime? LastAssessmentDate { get; init; }

    // Identity & Access
    public int TotalUsers { get; init; }
    public int EnabledUsers { get; init; }
    public int GuestUsers { get; init; }
    public int AdminUsers { get; init; }
    public int GlobalAdmins { get; init; }
    public int RiskyUsers { get; init; }

    // Groups
    public int TotalGroups { get; init; }
    public int SecurityGroups { get; init; }
    public int Microsoft365Groups { get; init; }

    // Applications
    public int EnterpriseApps { get; init; }
    public int AppRegistrations { get; init; }
    public int HighRiskApps { get; init; }

    // Devices
    public int TotalDevices { get; init; }
    public int ManagedDevices { get; init; }
    public int CompliantDevices { get; init; }

    // Policies
    public int ConditionalAccessPolicies { get; init; }
    public int EnabledCaPolicies { get; init; }
    public int DlpPolicies { get; init; }

    // Exchange
    public int Mailboxes { get; init; }
    public int SharedMailboxes { get; init; }

    // SharePoint & Teams
    public int SharePointSites { get; init; }
    public int TeamsCount { get; init; }

    // Licenses
    public int TotalLicenses { get; init; }
    public int AssignedLicenses { get; init; }
}
