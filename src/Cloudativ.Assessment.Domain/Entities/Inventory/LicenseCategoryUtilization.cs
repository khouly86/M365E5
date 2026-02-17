using Cloudativ.Assessment.Domain.Enums;

namespace Cloudativ.Assessment.Domain.Entities.Inventory;

/// <summary>
/// Per-license-category utilization tracking for analyzing feature usage and identifying value leakage.
/// </summary>
public class LicenseCategoryUtilization : BaseEntity
{
    public Guid TenantId { get; set; }
    public Guid SnapshotId { get; set; }

    // License Category Info
    public LicenseCategory LicenseCategory { get; set; }
    public string CategoryDisplayName { get; set; } = string.Empty;
    public string? TierGroup { get; set; }

    // License Counts
    public int TotalLicenses { get; set; }
    public int AssignedLicenses { get; set; }
    public int AvailableLicenses { get; set; }
    public int SuspendedLicenses { get; set; }

    // User Counts
    public int TotalUsersWithLicense { get; set; }
    public int ActiveUsersWithLicense { get; set; }
    public int DisabledUsersWithLicense { get; set; }

    // Feature Utilization - Identity
    public int UsersWithMfaEnabled { get; set; }
    public int UsersWithoutMfa { get; set; }
    public int UsersWithConditionalAccess { get; set; }
    public int UsersWithoutCa { get; set; }
    public int UsersWithPimCoverage { get; set; }
    public int UsersWithIdentityProtection { get; set; }

    // Feature Utilization - Security
    public int UsersWithDefenderForEndpoint { get; set; }
    public int UsersWithDefenderForOffice { get; set; }
    public int UsersWithDefenderForIdentity { get; set; }
    public int UsersWithDefenderForCloudApps { get; set; }

    // Feature Utilization - Compliance
    public int UsersWithPurviewLabels { get; set; }
    public int UsersWithDlp { get; set; }
    public int UsersWithRetention { get; set; }
    public int UsersWithEDiscovery { get; set; }
    public int UsersWithInsiderRisk { get; set; }

    // Feature Utilization - Productivity
    public int UsersWithTeamsPhoneSystem { get; set; }
    public int UsersWithAudioConferencing { get; set; }
    public int UsersWithPowerBI { get; set; }

    // Calculated Utilization Percentages
    public double OverallFeatureUtilization { get; set; }
    public double IdentityFeatureUtilization { get; set; }
    public double SecurityFeatureUtilization { get; set; }
    public double ComplianceFeatureUtilization { get; set; }
    public double ProductivityFeatureUtilization { get; set; }

    // Cost Analysis
    public decimal EstimatedMonthlyPricePerUser { get; set; }
    public decimal TotalMonthlyLicenseCost { get; set; }
    public decimal EstimatedMonthlyWaste { get; set; }
    public decimal EstimatedAnnualWaste { get; set; }
    public decimal PotentialSavingsIfDowngraded { get; set; }
    public int UsersEligibleForDowngrade { get; set; }

    // Feature Details (JSON)
    public string? IncludedFeaturesJson { get; set; }
    public string? UsedFeaturesJson { get; set; }
    public string? UnusedFeaturesJson { get; set; }
    public string? FeatureUtilizationBreakdownJson { get; set; }

    // Top Affected Users (JSON - limited to 100)
    public string? TopUsersWithoutMfaJson { get; set; }
    public string? TopUsersWithoutCaJson { get; set; }
    public string? TopUsersNotUsingSecurityJson { get; set; }
    public string? UsersEligibleForDowngradeJson { get; set; }

    // Recommendations
    public string? RecommendationsJson { get; set; }
    public int CriticalRecommendations { get; set; }
    public int HighRecommendations { get; set; }
    public int MediumRecommendations { get; set; }

    // Navigation
    public virtual Tenant Tenant { get; set; } = null!;
    public virtual InventorySnapshot Snapshot { get; set; } = null!;
}
