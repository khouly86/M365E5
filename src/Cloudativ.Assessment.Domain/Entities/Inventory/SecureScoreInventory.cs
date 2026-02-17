namespace Cloudativ.Assessment.Domain.Entities.Inventory;

/// <summary>
/// Microsoft Secure Score and posture metrics.
/// </summary>
public class SecureScoreInventory : BaseEntity
{
    public Guid TenantId { get; set; }
    public Guid SnapshotId { get; set; }

    // Overall Score
    public double CurrentScore { get; set; }
    public double MaxScore { get; set; }
    public double ScorePercentage { get; set; }
    public double? PreviousScore { get; set; }
    public double? ScoreChange { get; set; }
    public DateTime? ScoreDate { get; set; }

    // By Category
    public double IdentityScore { get; set; }
    public double IdentityMaxScore { get; set; }
    public double DeviceScore { get; set; }
    public double DeviceMaxScore { get; set; }
    public double AppsScore { get; set; }
    public double AppsMaxScore { get; set; }
    public double DataScore { get; set; }
    public double DataMaxScore { get; set; }
    public double InfrastructureScore { get; set; }
    public double InfrastructureMaxScore { get; set; }

    // Improvement Actions
    public int TotalActions { get; set; }
    public int CompletedActions { get; set; }
    public int NotApplicableActions { get; set; }
    public int ToAddressActions { get; set; }
    public int InProgressActions { get; set; }
    public int PlannedActions { get; set; }
    public int RiskAcceptedActions { get; set; }
    public int ThirdPartyActions { get; set; }
    public int ResolvedThroughAlternate { get; set; }

    // Top Improvement Actions
    public string? TopImprovementActionsJson { get; set; }
    public double PotentialScoreIncrease { get; set; }

    // Score by Vendor
    public double MicrosoftScore { get; set; }
    public double? ThirdPartyScore { get; set; }

    // Defender Exposure Score
    public double? DefenderExposureScore { get; set; }
    public double? DefenderSecureScore { get; set; }

    // Compliance Manager
    public int ComplianceAssessmentCount { get; set; }
    public double? OverallComplianceScore { get; set; }
    public string? ComplianceAssessmentsJson { get; set; }

    // Historical Trend
    public string? ScoreTrendJson { get; set; }
    public double? Score7DaysAgo { get; set; }
    public double? Score30DaysAgo { get; set; }
    public double? Score90DaysAgo { get; set; }

    // License Comparison (E3 vs E5 features)
    public double? E5FeatureScore { get; set; }
    public double? E3FeatureScore { get; set; }
    public string? E5FeaturesNotUsedJson { get; set; }

    // Navigation
    public virtual Tenant Tenant { get; set; } = null!;
    public virtual InventorySnapshot Snapshot { get; set; } = null!;
}
