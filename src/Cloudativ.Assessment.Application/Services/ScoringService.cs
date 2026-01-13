using Cloudativ.Assessment.Domain.Enums;
using Cloudativ.Assessment.Domain.Interfaces;
using Microsoft.Extensions.Logging;

namespace Cloudativ.Assessment.Application.Services;

public interface IScoringService
{
    DomainScore CalculateDomainScore(NormalizedFindings findings);
    int CalculateOverallScore(IEnumerable<DomainScore> domainScores);
    string CalculateGrade(int score);
}

public class ScoringService : IScoringService
{
    private readonly ILogger<ScoringService> _logger;

    // Weights for severity impact on score (percentage deduction per finding)
    private const double CriticalWeight = 15.0;
    private const double HighWeight = 8.0;
    private const double MediumWeight = 4.0;
    private const double LowWeight = 1.0;

    // Maximum deduction per category to prevent single category from destroying score
    private const double MaxCategoryDeduction = 40.0;

    public ScoringService(ILogger<ScoringService> logger)
    {
        _logger = logger;
    }

    public DomainScore CalculateDomainScore(NormalizedFindings findings)
    {
        var score = new DomainScore
        {
            Domain = findings.Domain,
            TotalChecks = findings.Findings.Count
        };

        if (findings.Findings.Count == 0)
        {
            score.Score = 100;
            score.Grade = CalculateGrade(score.Score);
            return score;
        }

        // Count findings by severity
        var nonCompliantFindings = findings.Findings.Where(f => !f.IsCompliant).ToList();
        score.CriticalCount = nonCompliantFindings.Count(f => f.Severity == Severity.Critical);
        score.HighCount = nonCompliantFindings.Count(f => f.Severity == Severity.High);
        score.MediumCount = nonCompliantFindings.Count(f => f.Severity == Severity.Medium);
        score.LowCount = nonCompliantFindings.Count(f => f.Severity == Severity.Low);

        score.PassedChecks = findings.Findings.Count(f => f.IsCompliant);
        score.FailedChecks = nonCompliantFindings.Count;

        // Calculate deductions
        double totalDeduction = 0;

        // Critical findings - severe impact
        totalDeduction += Math.Min(score.CriticalCount * CriticalWeight, MaxCategoryDeduction);

        // High findings
        totalDeduction += Math.Min(score.HighCount * HighWeight, MaxCategoryDeduction);

        // Medium findings
        totalDeduction += Math.Min(score.MediumCount * MediumWeight, MaxCategoryDeduction);

        // Low findings
        totalDeduction += Math.Min(score.LowCount * LowWeight, MaxCategoryDeduction);

        // Calculate final score (minimum 0)
        score.Score = Math.Max(0, (int)(100 - totalDeduction));
        score.Grade = CalculateGrade(score.Score);

        // Generate top recommendations
        score.TopRecommendations = GenerateRecommendations(nonCompliantFindings);

        _logger.LogDebug(
            "Domain {Domain} score calculated: {Score} (Critical: {Critical}, High: {High}, Medium: {Medium}, Low: {Low})",
            findings.Domain, score.Score, score.CriticalCount, score.HighCount, score.MediumCount, score.LowCount);

        return score;
    }

    public int CalculateOverallScore(IEnumerable<DomainScore> domainScores)
    {
        var scores = domainScores.ToList();
        if (scores.Count == 0)
            return 0;

        // Weighted average with domain weights
        var domainWeights = new Dictionary<AssessmentDomain, double>
        {
            { AssessmentDomain.IdentityAndAccess, 1.5 },
            { AssessmentDomain.PrivilegedAccess, 1.4 },
            { AssessmentDomain.DataProtectionCompliance, 1.3 },
            { AssessmentDomain.ExchangeEmailSecurity, 1.2 },
            { AssessmentDomain.MicrosoftDefender, 1.2 },
            { AssessmentDomain.AppGovernance, 1.1 },
            { AssessmentDomain.AuditLogging, 1.0 },
            { AssessmentDomain.DeviceEndpoint, 1.0 },
            { AssessmentDomain.CollaborationSecurity, 1.0 }
        };

        double totalWeight = 0;
        double weightedSum = 0;

        foreach (var domainScore in scores)
        {
            var weight = domainWeights.GetValueOrDefault(domainScore.Domain, 1.0);
            weightedSum += domainScore.Score * weight;
            totalWeight += weight;
        }

        return totalWeight > 0 ? (int)(weightedSum / totalWeight) : 0;
    }

    public string CalculateGrade(int score) => score switch
    {
        >= 90 => "A",
        >= 80 => "B",
        >= 70 => "C",
        >= 60 => "D",
        _ => "F"
    };

    private static List<string> GenerateRecommendations(List<NormalizedFinding> nonCompliantFindings)
    {
        return nonCompliantFindings
            .OrderBy(f => f.Severity)
            .Take(5)
            .Where(f => !string.IsNullOrEmpty(f.Remediation))
            .Select(f => f.Remediation!)
            .ToList();
    }
}
