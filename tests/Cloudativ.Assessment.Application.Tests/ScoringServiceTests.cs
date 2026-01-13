using Cloudativ.Assessment.Application.Services;
using Cloudativ.Assessment.Domain.Enums;
using Cloudativ.Assessment.Domain.Interfaces;
using FluentAssertions;
using Microsoft.Extensions.Logging;
using Moq;
using Xunit;

namespace Cloudativ.Assessment.Application.Tests;

public class ScoringServiceTests
{
    private readonly IScoringService _scoringService;

    public ScoringServiceTests()
    {
        var loggerMock = new Mock<ILogger<ScoringService>>();
        _scoringService = new ScoringService(loggerMock.Object);
    }

    [Fact]
    public void CalculateDomainScore_WithNoFindings_ReturnsFullScore()
    {
        // Arrange
        var findings = new NormalizedFindings
        {
            Domain = AssessmentDomain.IdentityAndAccess,
            Findings = new List<NormalizedFinding>()
        };

        // Act
        var result = _scoringService.CalculateDomainScore(findings);

        // Assert
        result.Score.Should().Be(100);
        result.Grade.Should().Be("A");
        result.TotalChecks.Should().Be(0);
        result.PassedChecks.Should().Be(0);
        result.FailedChecks.Should().Be(0);
    }

    [Fact]
    public void CalculateDomainScore_WithAllCompliantFindings_ReturnsFullScore()
    {
        // Arrange
        var findings = new NormalizedFindings
        {
            Domain = AssessmentDomain.IdentityAndAccess,
            Findings = new List<NormalizedFinding>
            {
                CreateFinding(Severity.Medium, isCompliant: true),
                CreateFinding(Severity.Low, isCompliant: true),
                CreateFinding(Severity.Informational, isCompliant: true)
            }
        };

        // Act
        var result = _scoringService.CalculateDomainScore(findings);

        // Assert
        result.Score.Should().Be(100);
        result.Grade.Should().Be("A");
        result.PassedChecks.Should().Be(3);
        result.FailedChecks.Should().Be(0);
    }

    [Fact]
    public void CalculateDomainScore_WithOneCriticalFinding_DeductsCorrectly()
    {
        // Arrange
        var findings = new NormalizedFindings
        {
            Domain = AssessmentDomain.IdentityAndAccess,
            Findings = new List<NormalizedFinding>
            {
                CreateFinding(Severity.Critical, isCompliant: false)
            }
        };

        // Act
        var result = _scoringService.CalculateDomainScore(findings);

        // Assert
        result.Score.Should().Be(85); // 100 - 15 (critical weight)
        result.Grade.Should().Be("B");
        result.CriticalCount.Should().Be(1);
        result.FailedChecks.Should().Be(1);
    }

    [Fact]
    public void CalculateDomainScore_WithMultipleSeverities_DeductsCorrectly()
    {
        // Arrange
        var findings = new NormalizedFindings
        {
            Domain = AssessmentDomain.DataProtectionCompliance,
            Findings = new List<NormalizedFinding>
            {
                CreateFinding(Severity.Critical, isCompliant: false),
                CreateFinding(Severity.High, isCompliant: false),
                CreateFinding(Severity.Medium, isCompliant: false),
                CreateFinding(Severity.Low, isCompliant: false),
                CreateFinding(Severity.Low, isCompliant: true) // Compliant - should not deduct
            }
        };

        // Act
        var result = _scoringService.CalculateDomainScore(findings);

        // Assert
        // Deductions: 15 (critical) + 8 (high) + 4 (medium) + 1 (low) = 28
        result.Score.Should().Be(72);
        result.Grade.Should().Be("C");
        result.CriticalCount.Should().Be(1);
        result.HighCount.Should().Be(1);
        result.MediumCount.Should().Be(1);
        result.LowCount.Should().Be(1);
        result.PassedChecks.Should().Be(1);
        result.FailedChecks.Should().Be(4);
    }

    [Fact]
    public void CalculateDomainScore_WithManyFindings_RespectsMaxDeductionPerCategory()
    {
        // Arrange - 5 critical findings should hit the max cap (40)
        var findings = new NormalizedFindings
        {
            Domain = AssessmentDomain.IdentityAndAccess,
            Findings = Enumerable.Range(0, 5)
                .Select(_ => CreateFinding(Severity.Critical, isCompliant: false))
                .ToList()
        };

        // Act
        var result = _scoringService.CalculateDomainScore(findings);

        // Assert
        // 5 critical = 5 * 15 = 75, but capped at 40
        result.Score.Should().Be(60);
        result.Grade.Should().Be("D");
        result.CriticalCount.Should().Be(5);
    }

    [Fact]
    public void CalculateDomainScore_ExtremeCase_MinimumScoreIsZero()
    {
        // Arrange - Many findings across all severities
        var findings = new NormalizedFindings
        {
            Domain = AssessmentDomain.IdentityAndAccess,
            Findings = new List<NormalizedFinding>()
        };

        // Add 10 critical (capped at 40), 10 high (capped at 40), 10 medium (capped at 40), 10 low (capped at 40)
        for (int i = 0; i < 10; i++)
        {
            findings.Findings.Add(CreateFinding(Severity.Critical, isCompliant: false));
            findings.Findings.Add(CreateFinding(Severity.High, isCompliant: false));
            findings.Findings.Add(CreateFinding(Severity.Medium, isCompliant: false));
            findings.Findings.Add(CreateFinding(Severity.Low, isCompliant: false));
        }

        // Act
        var result = _scoringService.CalculateDomainScore(findings);

        // Assert
        // Max deduction = 40 + 40 + 40 + 10 = 130, but score minimum is 0
        result.Score.Should().Be(0);
        result.Grade.Should().Be("F");
    }

    [Theory]
    [InlineData(100, "A")]
    [InlineData(95, "A")]
    [InlineData(90, "A")]
    [InlineData(89, "B")]
    [InlineData(80, "B")]
    [InlineData(79, "C")]
    [InlineData(70, "C")]
    [InlineData(69, "D")]
    [InlineData(60, "D")]
    [InlineData(59, "F")]
    [InlineData(0, "F")]
    public void CalculateGrade_ReturnsCorrectGrade(int score, string expectedGrade)
    {
        // Act
        var result = _scoringService.CalculateGrade(score);

        // Assert
        result.Should().Be(expectedGrade);
    }

    [Fact]
    public void CalculateOverallScore_WithNoDomainScores_ReturnsZero()
    {
        // Arrange
        var domainScores = new List<DomainScore>();

        // Act
        var result = _scoringService.CalculateOverallScore(domainScores);

        // Assert
        result.Should().Be(0);
    }

    [Fact]
    public void CalculateOverallScore_WithSingleDomain_ReturnsThatscore()
    {
        // Arrange
        var domainScores = new List<DomainScore>
        {
            new() { Domain = AssessmentDomain.IdentityAndAccess, Score = 85 }
        };

        // Act
        var result = _scoringService.CalculateOverallScore(domainScores);

        // Assert
        result.Should().Be(85);
    }

    [Fact]
    public void CalculateOverallScore_WithMultipleDomains_CalculatesWeightedAverage()
    {
        // Arrange
        var domainScores = new List<DomainScore>
        {
            new() { Domain = AssessmentDomain.IdentityAndAccess, Score = 80 },      // Weight 1.5
            new() { Domain = AssessmentDomain.DataProtectionCompliance, Score = 70 }, // Weight 1.3
            new() { Domain = AssessmentDomain.AuditLogging, Score = 90 }             // Weight 1.0
        };

        // Act
        var result = _scoringService.CalculateOverallScore(domainScores);

        // Assert
        // Weighted: (80*1.5 + 70*1.3 + 90*1.0) / (1.5 + 1.3 + 1.0) = 301 / 3.8 = 79.2
        result.Should().BeInRange(78, 80);
    }

    [Fact]
    public void CalculateOverallScore_IAMWeighedHigher()
    {
        // Arrange - IAM low, others high. Since IAM has higher weight, overall should be pulled down
        var domainScores = new List<DomainScore>
        {
            new() { Domain = AssessmentDomain.IdentityAndAccess, Score = 50 },      // Weight 1.5
            new() { Domain = AssessmentDomain.CollaborationSecurity, Score = 100 }, // Weight 1.0
            new() { Domain = AssessmentDomain.DeviceEndpoint, Score = 100 }         // Weight 1.0
        };

        // Act
        var result = _scoringService.CalculateOverallScore(domainScores);

        // Assert
        // Weighted: (50*1.5 + 100*1.0 + 100*1.0) / (1.5 + 1.0 + 1.0) = 275 / 3.5 = 78.5
        result.Should().BeLessThan(83); // Would be 83.3 if equally weighted
    }

    [Fact]
    public void CalculateDomainScore_GeneratesTopRecommendations()
    {
        // Arrange
        var findings = new NormalizedFindings
        {
            Domain = AssessmentDomain.IdentityAndAccess,
            Findings = new List<NormalizedFinding>
            {
                CreateFinding(Severity.Critical, isCompliant: false, remediation: "Enable MFA for all admins"),
                CreateFinding(Severity.High, isCompliant: false, remediation: "Block legacy authentication"),
                CreateFinding(Severity.Medium, isCompliant: false, remediation: "Review guest users")
            }
        };

        // Act
        var result = _scoringService.CalculateDomainScore(findings);

        // Assert
        result.TopRecommendations.Should().NotBeEmpty();
        result.TopRecommendations.Should().Contain("Enable MFA for all admins");
    }

    private static NormalizedFinding CreateFinding(
        Severity severity,
        bool isCompliant,
        string? remediation = null)
    {
        return new NormalizedFinding
        {
            CheckId = Guid.NewGuid().ToString(),
            CheckName = $"Test Check {severity}",
            Title = $"Test Finding {severity}",
            Description = "Test description",
            Severity = severity,
            IsCompliant = isCompliant,
            Remediation = remediation
        };
    }
}
