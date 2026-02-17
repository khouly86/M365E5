namespace Cloudativ.Assessment.Domain.Entities.Inventory;

/// <summary>
/// Aggregate license utilization tracking across all license categories.
/// </summary>
public class LicenseUtilizationInventory : BaseEntity
{
    public Guid TenantId { get; set; }
    public Guid SnapshotId { get; set; }

    // === AGGREGATE LICENSE STATS ===
    public int TotalLicenses { get; set; }
    public int TotalAssignedLicenses { get; set; }
    public int TotalAvailableLicenses { get; set; }
    public int TotalSuspendedLicenses { get; set; }
    public double OverallUtilizationPercentage { get; set; }

    // === LICENSE DISTRIBUTION BY CATEGORY ===
    public string? LicenseSummaryByCategoryJson { get; set; }
    public int TotalLicenseCategories { get; set; }
    public int TotalPrimaryLicenseUsers { get; set; }
    public int TotalUnlicensedUsers { get; set; }

    // === LEGACY FIELDS (kept for backwards compatibility) ===
    // E5 License Stats
    public int E5LicensesTotal { get; set; }
    public int E5LicensesAssigned { get; set; }
    public int E5LicensesAvailable { get; set; }
    public int E5LicensesSuspended { get; set; }

    // E3 License Stats (for comparison)
    public int E3LicensesTotal { get; set; }
    public int E3LicensesAssigned { get; set; }
    public int E3LicensesAvailable { get; set; }

    // Other License Stats
    public int F1LicensesTotal { get; set; }
    public int F3LicensesTotal { get; set; }
    public string? AllLicenseSummaryJson { get; set; }

    // === NEW: License counts by tier group ===
    public int EnterpriseLicensesTotal { get; set; }
    public int EnterpriseLicensesAssigned { get; set; }
    public int BusinessLicensesTotal { get; set; }
    public int BusinessLicensesAssigned { get; set; }
    public int FrontlineLicensesTotal { get; set; }
    public int FrontlineLicensesAssigned { get; set; }
    public int EducationLicensesTotal { get; set; }
    public int EducationLicensesAssigned { get; set; }
    public int GovernmentLicensesTotal { get; set; }
    public int GovernmentLicensesAssigned { get; set; }
    public int AddOnLicensesTotal { get; set; }
    public int AddOnLicensesAssigned { get; set; }

    // Feature Utilization - Identity
    public int UsersWithE5NoMfa { get; set; }
    public int UsersWithE5NoConditionalAccess { get; set; }
    public int UsersWithE5NoPimCoverage { get; set; }
    public int UsersWithE5NoIdentityProtection { get; set; }
    public int AdminsWithoutPim { get; set; }

    // Feature Utilization - Security
    public int UsersWithE5DefenderNotOnboarded { get; set; }
    public int DevicesWithE5NoDefender { get; set; }
    public int UsersWithE5NoDefenderForOffice { get; set; }
    public int UsersWithE5NoCloudAppSecurity { get; set; }

    // Feature Utilization - Compliance
    public int UsersWithE5NoPurviewLabels { get; set; }
    public int UsersWithE5NoDlp { get; set; }
    public int UsersWithE5NoRetention { get; set; }
    public int UsersWithE5NoEDiscovery { get; set; }
    public int UsersWithE5NoInsiderRisk { get; set; }

    // Feature Utilization - Productivity
    public int UsersWithE5NoTeamsPhoneSystem { get; set; }
    public int UsersWithE5NoAudioConferencing { get; set; }
    public int UsersWithE5NoPowerBI { get; set; }

    // Value Leakage Lists (JSON arrays of user UPNs - limited to top 100)
    public string? NoMfaUsersJson { get; set; }
    public string? NoCaUsersJson { get; set; }
    public string? DefenderNotOnboardedJson { get; set; }
    public string? NoPurviewUsersJson { get; set; }
    public string? NoPimUsersJson { get; set; }
    public string? NoDefenderForOfficeJson { get; set; }

    // Calculated Metrics
    public double E5UtilizationPercentage { get; set; }
    public double IdentityFeatureUtilization { get; set; }
    public double SecurityFeatureUtilization { get; set; }
    public double ComplianceFeatureUtilization { get; set; }
    public double OverallFeatureUtilization { get; set; }

    // Cost Analysis
    public decimal E5MonthlyPricePerUser { get; set; }
    public decimal E3MonthlyPricePerUser { get; set; }
    public decimal EstimatedMonthlyWaste { get; set; }
    public decimal EstimatedAnnualWaste { get; set; }
    public decimal PotentialSavingsIfDowngraded { get; set; }
    public int UsersEligibleForDowngrade { get; set; }

    // Recommendations
    public string? RecommendationsJson { get; set; }
    public int CriticalRecommendations { get; set; }
    public int HighRecommendations { get; set; }
    public int MediumRecommendations { get; set; }

    // === NEW: Aggregate feature utilization across ALL license types ===
    public int TotalUsersWithoutMfa { get; set; }
    public int TotalUsersWithoutCa { get; set; }
    public int TotalUsersWithoutDefender { get; set; }
    public decimal TotalEstimatedMonthlyWaste { get; set; }
    public decimal TotalEstimatedAnnualWaste { get; set; }

    // Top categories by waste
    public string? TopWasteByCategoryJson { get; set; }

    // Navigation
    public virtual Tenant Tenant { get; set; } = null!;
    public virtual InventorySnapshot Snapshot { get; set; } = null!;
}
