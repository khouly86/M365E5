using System.Text.Json;
using Cloudativ.Assessment.Application.DTOs;
using Cloudativ.Assessment.Application.Interfaces;
using Cloudativ.Assessment.Domain.Enums;
using Cloudativ.Assessment.Domain.Interfaces;
using Microsoft.Extensions.Logging;

namespace Cloudativ.Assessment.Infrastructure.Services;

public class DashboardService : IDashboardService
{
    private readonly IUnitOfWork _unitOfWork;
    private readonly ILogger<DashboardService> _logger;

    public DashboardService(IUnitOfWork unitOfWork, ILogger<DashboardService> logger)
    {
        _unitOfWork = unitOfWork;
        _logger = logger;
    }

    public async Task<DashboardSummaryDto> GetSummaryAsync(CancellationToken cancellationToken = default)
    {
        var tenants = await _unitOfWork.Tenants.GetAllAsync(cancellationToken);
        var totalTenants = tenants.Count;
        var activeTenants = tenants.Count(t => t.OnboardingStatus == OnboardingStatus.Active ||
                                               t.OnboardingStatus == OnboardingStatus.Validated);
        var pendingOnboarding = tenants.Count(t => t.OnboardingStatus == OnboardingStatus.Pending ||
                                                   t.OnboardingStatus == OnboardingStatus.InProgress);

        var runs = await _unitOfWork.AssessmentRuns.GetAllAsync(cancellationToken);
        var completedRuns = runs.Where(r => r.Status == AssessmentStatus.Completed).ToList();
        var thisMonth = DateTime.UtcNow.AddMonths(-1);
        var runsThisMonth = completedRuns.Count(r => r.CompletedAt >= thisMonth);

        var averageScore = completedRuns.Any() && completedRuns.Any(r => r.OverallScore.HasValue)
            ? completedRuns.Where(r => r.OverallScore.HasValue).Average(r => r.OverallScore!.Value)
            : 0;

        // Get findings counts
        var criticalFindings = 0;
        var highFindings = 0;

        foreach (var run in completedRuns.OrderByDescending(r => r.CompletedAt).Take(10))
        {
            var findings = await _unitOfWork.Findings.GetByRunIdAsync(run.Id, cancellationToken);
            criticalFindings += findings.Count(f => f.Severity == Severity.Critical && !f.IsCompliant);
            highFindings += findings.Count(f => f.Severity == Severity.High && !f.IsCompliant);
        }

        // Get tenant scores
        var tenantScores = new List<TenantScoreDto>();
        foreach (var tenant in tenants)
        {
            var latestRun = await _unitOfWork.AssessmentRuns.GetLatestByTenantAsync(tenant.Id, cancellationToken);
            if (latestRun?.OverallScore.HasValue == true)
            {
                tenantScores.Add(new TenantScoreDto
                {
                    TenantId = tenant.Id,
                    TenantName = tenant.Name,
                    Score = latestRun.OverallScore.Value,
                    Grade = GetGrade(latestRun.OverallScore.Value),
                    LastAssessment = latestRun.CompletedAt
                });
            }
        }

        // Recent assessments
        var recentAssessments = completedRuns
            .OrderByDescending(r => r.CompletedAt)
            .Take(10)
            .Select(r =>
            {
                var tenant = tenants.FirstOrDefault(t => t.Id == r.TenantId);
                return new RecentAssessmentDto
                {
                    RunId = r.Id,
                    TenantId = r.TenantId,
                    TenantName = tenant?.Name ?? "Unknown",
                    CompletedAt = r.CompletedAt ?? r.StartedAt,
                    Score = r.OverallScore ?? 0,
                    Status = r.Status
                };
            })
            .ToList();

        return new DashboardSummaryDto
        {
            TotalTenants = totalTenants,
            ActiveTenants = activeTenants,
            PendingOnboarding = pendingOnboarding,
            TotalAssessments = runs.Count,
            AssessmentsThisMonth = runsThisMonth,
            AverageScore = averageScore,
            CriticalFindingsCount = criticalFindings,
            HighFindingsCount = highFindings,
            TopTenants = tenantScores.OrderByDescending(t => t.Score).Take(5).ToList(),
            LowestTenants = tenantScores.OrderBy(t => t.Score).Take(5).ToList(),
            RecentAssessments = recentAssessments
        };
    }

    public async Task<TenantDashboardDto> GetTenantDashboardAsync(Guid tenantId, CancellationToken cancellationToken = default)
    {
        var tenant = await _unitOfWork.Tenants.GetByIdAsync(tenantId, cancellationToken);
        if (tenant == null)
            throw new InvalidOperationException($"Tenant with ID '{tenantId}' not found");

        var runs = await _unitOfWork.AssessmentRuns.GetByTenantAsync(tenantId, 20, cancellationToken);
        var latestRun = runs.FirstOrDefault();
        var totalRuns = await _unitOfWork.AssessmentRuns.CountAsync(r => r.TenantId == tenantId, cancellationToken);

        var tenantDto = new TenantDto
        {
            Id = tenant.Id,
            Name = tenant.Name,
            Domain = tenant.Domain,
            AzureTenantId = tenant.AzureTenantId,
            OnboardingStatus = tenant.OnboardingStatus,
            Industry = tenant.Industry,
            ContactEmail = tenant.ContactEmail,
            CreatedAt = tenant.CreatedAt,
            LastAssessmentScore = latestRun?.OverallScore,
            LastAssessmentDate = latestRun?.CompletedAt ?? latestRun?.StartedAt,
            TotalAssessments = totalRuns
        };

        var recentAssessments = runs.Select(r => new AssessmentRunDto
        {
            Id = r.Id,
            TenantId = r.TenantId,
            TenantName = tenant.Name,
            StartedAt = r.StartedAt,
            CompletedAt = r.CompletedAt,
            Status = r.Status,
            InitiatedBy = r.InitiatedBy,
            OverallScore = r.OverallScore,
            DomainScores = !string.IsNullOrEmpty(r.SummaryScoresJson)
                ? JsonSerializer.Deserialize<Dictionary<string, DomainScoreSummary>>(r.SummaryScoresJson) ?? new()
                : new()
        }).ToList();

        var scoreTrend = runs
            .Where(r => r.OverallScore.HasValue && r.CompletedAt.HasValue)
            .OrderBy(r => r.CompletedAt)
            .Select(r => new ScoreTrendPoint
            {
                Date = r.CompletedAt!.Value,
                Score = r.OverallScore!.Value,
                Label = r.CompletedAt!.Value.ToString("MMM dd")
            })
            .ToList();

        var findingsSummary = new FindingsSummaryDto();
        var domainBreakdown = new Dictionary<AssessmentDomain, DomainScoreSummary>();

        if (latestRun != null)
        {
            var findings = await _unitOfWork.Findings.GetByRunIdAsync(latestRun.Id, cancellationToken);

            findingsSummary = new FindingsSummaryDto
            {
                TotalFindings = findings.Count,
                CriticalCount = findings.Count(f => f.Severity == Severity.Critical && !f.IsCompliant),
                HighCount = findings.Count(f => f.Severity == Severity.High && !f.IsCompliant),
                MediumCount = findings.Count(f => f.Severity == Severity.Medium && !f.IsCompliant),
                LowCount = findings.Count(f => f.Severity == Severity.Low && !f.IsCompliant),
                InformationalCount = findings.Count(f => f.Severity == Severity.Informational),
                CompliantCount = findings.Count(f => f.IsCompliant),
                NonCompliantCount = findings.Count(f => !f.IsCompliant),
                FindingsByDomain = findings.GroupBy(f => f.Domain)
                    .ToDictionary(g => g.Key, g => g.Count()),
                TopCriticalFindings = findings
                    .Where(f => (f.Severity == Severity.Critical || f.Severity == Severity.High) && !f.IsCompliant)
                    .OrderBy(f => f.Severity)
                    .Take(10)
                    .Select(f => new FindingDto
                    {
                        Id = f.Id,
                        AssessmentRunId = f.AssessmentRunId,
                        Domain = f.Domain,
                        DomainDisplayName = f.Domain.GetDisplayName(),
                        Severity = f.Severity,
                        Title = f.Title,
                        Description = f.Description,
                        Category = f.Category,
                        IsCompliant = f.IsCompliant
                    })
                    .ToList()
            };

            if (!string.IsNullOrEmpty(latestRun.SummaryScoresJson))
            {
                var scores = JsonSerializer.Deserialize<Dictionary<string, DomainScoreSummary>>(latestRun.SummaryScoresJson);
                if (scores != null)
                {
                    foreach (var kvp in scores)
                    {
                        if (Enum.TryParse<AssessmentDomain>(kvp.Key, out var domain))
                        {
                            domainBreakdown[domain] = kvp.Value;
                        }
                    }
                }
            }
        }

        return new TenantDashboardDto
        {
            Tenant = tenantDto,
            LatestAssessment = latestRun != null ? recentAssessments.FirstOrDefault() : null,
            RecentAssessments = recentAssessments,
            ScoreTrend = scoreTrend,
            FindingsSummary = findingsSummary,
            DomainBreakdown = domainBreakdown
        };
    }

    public async Task<List<ScoreTrendPoint>> GetScoreTrendAsync(Guid tenantId, int monthsBack = 12, CancellationToken cancellationToken = default)
    {
        var cutoff = DateTime.UtcNow.AddMonths(-monthsBack);
        var runs = await _unitOfWork.AssessmentRuns.GetByTenantAsync(tenantId, 100, cancellationToken);

        return runs
            .Where(r => r.OverallScore.HasValue && r.CompletedAt.HasValue && r.CompletedAt >= cutoff)
            .OrderBy(r => r.CompletedAt)
            .Select(r => new ScoreTrendPoint
            {
                Date = r.CompletedAt!.Value,
                Score = r.OverallScore!.Value,
                Label = r.CompletedAt!.Value.ToString("MMM dd")
            })
            .ToList();
    }

    public async Task<FindingsSummaryDto> GetFindingsSummaryAsync(Guid runId, CancellationToken cancellationToken = default)
    {
        var findings = await _unitOfWork.Findings.GetByRunIdAsync(runId, cancellationToken);

        return new FindingsSummaryDto
        {
            TotalFindings = findings.Count,
            CriticalCount = findings.Count(f => f.Severity == Severity.Critical && !f.IsCompliant),
            HighCount = findings.Count(f => f.Severity == Severity.High && !f.IsCompliant),
            MediumCount = findings.Count(f => f.Severity == Severity.Medium && !f.IsCompliant),
            LowCount = findings.Count(f => f.Severity == Severity.Low && !f.IsCompliant),
            InformationalCount = findings.Count(f => f.Severity == Severity.Informational),
            CompliantCount = findings.Count(f => f.IsCompliant),
            NonCompliantCount = findings.Count(f => !f.IsCompliant),
            FindingsByDomain = findings.GroupBy(f => f.Domain)
                .ToDictionary(g => g.Key, g => g.Count())
        };
    }

    private static string GetGrade(int score) => score switch
    {
        >= 90 => "A",
        >= 80 => "B",
        >= 70 => "C",
        >= 60 => "D",
        _ => "F"
    };
}
