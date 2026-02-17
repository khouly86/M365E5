using Cloudativ.Assessment.Domain.Enums;

namespace Cloudativ.Assessment.Application.DTOs;

#region Core DTOs

public record StartInventoryRequest
{
    public Guid TenantId { get; set; }
    public List<InventoryDomain>? Domains { get; set; }
    public string? InitiatedBy { get; set; }
    public bool IncrementalCollection { get; set; } = false;
}

public record InventorySnapshotDto
{
    public Guid Id { get; init; }
    public Guid TenantId { get; init; }
    public string TenantName { get; init; } = string.Empty;
    public InventoryDomain Domain { get; init; }
    public string DomainDisplayName { get; init; } = string.Empty;
    public DateTime CollectedAt { get; init; }
    public InventoryStatus Status { get; init; }
    public int ItemCount { get; init; }
    public TimeSpan? Duration { get; init; }
    public int? ItemsAdded { get; init; }
    public int? ItemsRemoved { get; init; }
    public int? ItemsModified { get; init; }
    public string? ErrorMessage { get; init; }
    public List<string> Warnings { get; init; } = new();
}

public record InventoryProgressDto
{
    public Guid SnapshotId { get; init; }
    public InventoryStatus Status { get; init; }
    public double ProgressPercentage { get; init; }
    public InventoryDomain? CurrentDomain { get; init; }
    public string? CurrentDomainName { get; init; }
    public List<InventoryDomain> CompletedDomains { get; init; } = new();
    public List<InventoryDomain> PendingDomains { get; init; } = new();
    public List<InventoryDomain> FailedDomains { get; init; } = new();
    public int TotalItemsCollected { get; init; }
    public DateTime StartedAt { get; init; }
    public DateTime? CompletedAt { get; init; }
    public List<string> Errors { get; init; } = new();
}

public record InventoryDashboardDto
{
    public Guid TenantId { get; init; }
    public string TenantName { get; init; } = string.Empty;
    public DateTime? LastCollectionDate { get; init; }

    // Quick Stats
    public int TotalUsers { get; init; }
    public int HighRiskUsers { get; init; }
    public int TotalDevices { get; init; }
    public int NonCompliantDevices { get; init; }
    public int TotalApps { get; init; }
    public int HighPrivilegeApps { get; init; }

    // High-Risk Findings
    public int CriticalFindings { get; init; }
    public int HighFindings { get; init; }
    public int MediumFindings { get; init; }

    // Domain Collection Status
    public List<DomainStatusDto> DomainStatuses { get; init; } = new();

    // High-Risk Findings Summary
    public List<HighRiskFindingSummaryDto> HighRiskSummary { get; init; } = new();

    // E5 Utilization (legacy - kept for backwards compatibility)
    public E5UtilizationSummaryDto? E5Utilization { get; init; }

    // Multi-License Distribution (NEW)
    public List<LicenseSummaryDto> LicensesByCategory { get; init; } = new();
    public int TotalLicensedUsers { get; init; }
    public int TotalUnlicensedUsers { get; init; }
    public List<LicenseUtilizationByTypeDto> UtilizationByLicenseType { get; init; } = new();
    public LicenseDistributionDto? LicenseDistribution { get; init; }
}

public record DomainStatusDto
{
    public InventoryDomain Domain { get; init; }
    public string DisplayName { get; init; } = string.Empty;
    public DateTime? LastCollected { get; init; }
    public InventoryStatus? Status { get; init; }
    public int ItemCount { get; init; }
    public string? ErrorMessage { get; init; }
}

public record HighRiskFindingSummaryDto
{
    public string FindingType { get; init; } = string.Empty;
    public string Title { get; init; } = string.Empty;
    public string Description { get; init; } = string.Empty;
    public string Severity { get; init; } = string.Empty;
    public string Category { get; init; } = string.Empty;
    public int AffectedCount { get; init; }
    public List<string> AffectedResources { get; init; } = new();
    public string? Remediation { get; init; }
}

public record E5UtilizationSummaryDto
{
    public int TotalLicenses { get; init; }
    public int AssignedLicenses { get; init; }
    public double UtilizationPercentage { get; init; }
    public int UsersWithoutMfa { get; init; }
    public int UsersWithoutCa { get; init; }
    public int UsersWithoutDefender { get; init; }
    public decimal EstimatedWaste { get; init; }
}

#endregion

#region Identity DTOs

public record UserInventoryDto
{
    public Guid Id { get; init; }
    public string ObjectId { get; init; } = string.Empty;
    public string UserPrincipalName { get; init; } = string.Empty;
    public string? DisplayName { get; init; }
    public string? Mail { get; init; }
    public string UserType { get; init; } = "Member";
    public bool AccountEnabled { get; init; }
    public DateTime? CreatedDateTime { get; init; }
    public DateTime? LastSignInDateTime { get; init; }
    public bool IsMfaRegistered { get; init; }
    public bool IsMfaCapable { get; init; }
    public bool HasE5License { get; init; }
    public int LicenseCount { get; init; }
    public string? RiskLevel { get; init; }
    public bool IsPrivileged { get; init; }
    public bool IsGlobalAdmin { get; init; }
    public List<string> AssignedRoles { get; init; } = new();
    public List<string> AssignedLicenses { get; init; } = new();
    public List<string> AuthMethods { get; init; } = new();
    public string? Department { get; init; }
    public string? JobTitle { get; init; }
    public string? Country { get; init; }

    // License categorization
    public LicenseCategory PrimaryLicenseCategory { get; init; }
    public string PrimaryLicenseCategoryName { get; init; } = string.Empty;
    public string? PrimaryLicenseTierGroup { get; init; }
    public List<LicenseCategory> AllLicenseCategories { get; init; } = new();
    public bool HasBusinessPremium { get; init; }
    public bool HasFrontlineLicense { get; init; }
}

public record GroupInventoryDto
{
    public Guid Id { get; init; }
    public string ObjectId { get; init; } = string.Empty;
    public string DisplayName { get; init; } = string.Empty;
    public string? Description { get; init; }
    public string? Mail { get; init; }
    public string GroupType { get; init; } = string.Empty;
    public bool IsSecurityGroup { get; init; }
    public bool IsMicrosoft365Group { get; init; }
    public bool IsDynamicMembership { get; init; }
    public int MemberCount { get; init; }
    public int OwnerCount { get; init; }
    public bool HasExternalMembers { get; init; }
    public bool IsRoleAssignable { get; init; }
    public string? Visibility { get; init; }
    public DateTime? CreatedDateTime { get; init; }
}

public record DirectoryRoleDto
{
    public Guid Id { get; init; }
    public string RoleTemplateId { get; init; } = string.Empty;
    public string DisplayName { get; init; } = string.Empty;
    public string? Description { get; init; }
    public bool IsBuiltIn { get; init; }
    public bool IsPrivileged { get; init; }
    public int MemberCount { get; init; }
    public int UserMemberCount { get; init; }
    public int ServicePrincipalMemberCount { get; init; }
    public List<string> Members { get; init; } = new();
}

public record RoleMemberDto
{
    public string ObjectId { get; init; } = string.Empty;
    public string DisplayName { get; init; } = string.Empty;
    public string? UserPrincipalName { get; init; }
    public string MemberType { get; init; } = string.Empty;
}

public record ServicePrincipalDto
{
    public Guid Id { get; init; }
    public string ObjectId { get; init; } = string.Empty;
    public string AppId { get; init; } = string.Empty;
    public string DisplayName { get; init; } = string.Empty;
    public string ServicePrincipalType { get; init; } = string.Empty;
    public bool IsMicrosoftFirstParty { get; init; }
    public bool HasHighPrivilegePermissions { get; init; }
    public List<string> ApplicationPermissions { get; init; } = new();
    public List<string> DelegatedPermissions { get; init; } = new();
}

public record ConditionalAccessPolicyDto
{
    public Guid Id { get; init; }
    public string PolicyId { get; init; } = string.Empty;
    public string DisplayName { get; init; } = string.Empty;
    public string State { get; init; } = string.Empty;
    public DateTime? CreatedDateTime { get; init; }
    public bool RequiresMfa { get; init; }
    public bool BlocksLegacyAuth { get; init; }
    public bool BlocksAccess { get; init; }
    public bool IncludeAllUsers { get; init; }
    public bool IncludeAllApplications { get; init; }
    public bool RequiresCompliantDevice { get; init; }
    public bool RequiresHybridJoin { get; init; }
    public List<string> IncludeUsers { get; init; } = new();
    public List<string> ExcludeUsers { get; init; } = new();
    public List<string> IncludeApplications { get; init; } = new();
    public List<string> ExcludeApplications { get; init; } = new();
    public List<string> GrantControls { get; init; } = new();
    public List<string> SessionControls { get; init; } = new();
}

#endregion

#region Device DTOs

public record DeviceInventoryDto
{
    public Guid Id { get; init; }
    public string DeviceId { get; init; } = string.Empty;
    public string DeviceName { get; init; } = string.Empty;
    public string? SerialNumber { get; init; }
    public string OperatingSystem { get; init; } = string.Empty;
    public string? OsVersion { get; init; }
    public string OwnerType { get; init; } = string.Empty;
    public string? EnrollmentType { get; init; }
    public string ComplianceState { get; init; } = string.Empty;
    public bool IsManaged { get; init; }
    public bool IsEncrypted { get; init; }
    public bool HasDefenderForEndpoint { get; init; }
    public bool IsAzureAdJoined { get; init; }
    public bool RecoveryKeyEscrowed { get; init; }
    public string? PrimaryUserUpn { get; init; }
    public DateTime? EnrolledDateTime { get; init; }
    public DateTime? LastSyncDateTime { get; init; }
    public string? RiskScore { get; init; }
}

#endregion

#region Application DTOs

public record EnterpriseAppDto
{
    public Guid Id { get; init; }
    public string ObjectId { get; init; } = string.Empty;
    public string AppId { get; init; } = string.Empty;
    public string DisplayName { get; init; } = string.Empty;
    public string? PublisherName { get; init; }
    public bool AccountEnabled { get; init; }
    public bool IsMicrosoftApp { get; init; }
    public bool IsVerifiedPublisher { get; init; }
    public bool HasHighPrivilegePermissions { get; init; }
    public bool HasMailReadWrite { get; init; }
    public bool HasDirectoryReadWriteAll { get; init; }
    public bool HasFilesReadWriteAll { get; init; }
    public List<string> ApplicationPermissions { get; init; } = new();
    public List<string> DelegatedPermissions { get; init; } = new();
    public DateTime? NextCredentialExpiration { get; init; }
    public bool HasExpiredCredentials { get; init; }
    public int OwnerCount { get; init; }
    public DateTime? LastSignInDateTime { get; init; }
}

#endregion

#region Collaboration DTOs

public record SharePointSiteDto
{
    public Guid Id { get; init; }
    public string SiteId { get; init; } = string.Empty;
    public string WebUrl { get; init; } = string.Empty;
    public string Title { get; init; } = string.Empty;
    public string Template { get; init; } = string.Empty;
    public bool HasExternalSharing { get; init; }
    public int ExternalUserCount { get; init; }
    public long StorageUsedBytes { get; init; }
    public double StoragePercentUsed { get; init; }
    public string? OwnerUpn { get; init; }
    public bool IsOrphaned { get; init; }
    public bool IsInactive { get; init; }
    public string? SensitivityLabel { get; init; }
    public DateTime? LastActivityDate { get; init; }
}

public record TeamsDto
{
    public Guid Id { get; init; }
    public string TeamId { get; init; } = string.Empty;
    public string DisplayName { get; init; } = string.Empty;
    public string? Description { get; init; }
    public string Visibility { get; init; } = string.Empty;
    public int MemberCount { get; init; }
    public int OwnerCount { get; init; }
    public int GuestCount { get; init; }
    public int StandardChannelCount { get; init; }
    public int PrivateChannelCount { get; init; }
    public int SharedChannelCount { get; init; }
    public bool IsArchived { get; init; }
    public DateTime? LastActivityDate { get; init; }
    public DateTime? CreatedDateTime { get; init; }
}

#endregion

#region Filter DTOs

public record UserInventoryFilter
{
    public Guid TenantId { get; set; }
    public int Page { get; set; } = 1;
    public int PageSize { get; set; } = 50;
    public string? SearchTerm { get; set; }
    public string? UserType { get; set; }
    public bool? IsPrivileged { get; set; }
    public bool? MfaEnabled { get; set; }
    public bool? HasE5License { get; set; }
    public string? RiskLevel { get; set; }
    public bool? AccountEnabled { get; set; }
    public string? SortBy { get; set; }
    public bool SortDescending { get; set; }

    // License category filtering
    public LicenseCategory? LicenseCategory { get; set; }
    public List<LicenseCategory>? LicenseCategories { get; set; }
    public string? TierGroup { get; set; }
    public bool? HasAnyLicense { get; set; }
}

public record GroupInventoryFilter
{
    public Guid TenantId { get; set; }
    public int Page { get; set; } = 1;
    public int PageSize { get; set; } = 50;
    public string? SearchTerm { get; set; }
    public string? GroupType { get; set; }
    public bool? HasExternalMembers { get; set; }
    public bool? IsDynamic { get; set; }
    public string? SortBy { get; set; }
    public bool SortDescending { get; set; }
}

public record DeviceInventoryFilter
{
    public Guid TenantId { get; set; }
    public int Page { get; set; } = 1;
    public int PageSize { get; set; } = 50;
    public string? SearchTerm { get; set; }
    public string? OperatingSystem { get; set; }
    public string? ComplianceState { get; set; }
    public string? OwnerType { get; set; }
    public bool? HasDefender { get; set; }
    public bool? IsEncrypted { get; set; }
    public string? SortBy { get; set; }
    public bool SortDescending { get; set; }
}

public record AppInventoryFilter
{
    public Guid TenantId { get; set; }
    public int Page { get; set; } = 1;
    public int PageSize { get; set; } = 50;
    public string? SearchTerm { get; set; }
    public bool? HasHighPrivilegePermissions { get; set; }
    public bool? IsMicrosoftApp { get; set; }
    public bool? HasExpiredCredentials { get; set; }
    public string? SortBy { get; set; }
    public bool SortDescending { get; set; }
}

public record SharePointSiteFilter
{
    public Guid TenantId { get; set; }
    public int Page { get; set; } = 1;
    public int PageSize { get; set; } = 50;
    public string? SearchTerm { get; set; }
    public bool? HasExternalSharing { get; set; }
    public string? Template { get; set; }
    public string? SortBy { get; set; }
    public bool SortDescending { get; set; }
}

public record TeamsFilter
{
    public Guid TenantId { get; set; }
    public int Page { get; set; } = 1;
    public int PageSize { get; set; } = 50;
    public string? SearchTerm { get; set; }
    public string? Visibility { get; set; }
    public bool? HasGuests { get; set; }
    public bool? IsArchived { get; set; }
    public string? SortBy { get; set; }
    public bool SortDescending { get; set; }
}

#endregion

#region Result DTOs

public record PagedResult<T>
{
    public List<T> Items { get; init; } = new();
    public int TotalCount { get; init; }
    public int Page { get; init; }
    public int PageSize { get; init; }
    public int TotalPages => (int)Math.Ceiling(TotalCount / (double)PageSize);
    public bool HasNextPage => Page < TotalPages;
    public bool HasPreviousPage => Page > 1;
}

public record InventoryChangesDto
{
    public Guid FromSnapshotId { get; init; }
    public Guid ToSnapshotId { get; init; }
    public DateTime FromDate { get; init; }
    public DateTime ToDate { get; init; }
    public Dictionary<InventoryDomain, DomainChangesDto> DomainChanges { get; init; } = new();
}

public record DomainChangesDto
{
    public InventoryDomain Domain { get; init; }
    public int ItemsAdded { get; init; }
    public int ItemsRemoved { get; init; }
    public int ItemsModified { get; init; }
    public List<string> AddedItems { get; init; } = new();
    public List<string> RemovedItems { get; init; } = new();
}

public enum InventoryExportFormat
{
    Csv,
    Excel,
    Json,
    Pdf
}

#endregion

#region Tenant Baseline DTOs

public record TenantBaselineDto
{
    public Guid TenantId { get; init; }
    public string AzureTenantId { get; init; } = string.Empty;
    public string DisplayName { get; init; } = string.Empty;
    public string? PrimaryDomain { get; init; }
    public List<VerifiedDomainDto> VerifiedDomains { get; init; } = new();
    public bool ModernAuthEnabled { get; init; }
    public bool SmtpAuthEnabled { get; init; }
    public bool LegacyProtocolsEnabled { get; init; }
    public bool IsMultiGeoEnabled { get; init; }
    public string? PreferredDataLocation { get; init; }
    public List<LicenseSubscriptionDto> Subscriptions { get; init; } = new();
    public List<ServiceHealthItemDto> ServiceHealthIssues { get; init; } = new();
    public DateTime? LastCollected { get; init; }
}

public record VerifiedDomainDto
{
    public string Name { get; init; } = string.Empty;
    public bool IsDefault { get; init; }
    public bool IsInitial { get; init; }
    public string? Type { get; init; }
}

public record LicenseSubscriptionDto
{
    public string SkuId { get; init; } = string.Empty;
    public string SkuPartNumber { get; init; } = string.Empty;
    public string? DisplayName { get; init; }
    public string? CapabilityStatus { get; init; }
    public int PrepaidUnits { get; init; }
    public int ConsumedUnits { get; init; }
    public int AvailableUnits { get; init; }
    public bool IsTrial { get; init; }
    public DateTime? ExpirationDate { get; init; }

    // License categorization
    public LicenseCategory LicenseCategory { get; init; }
    public string LicenseCategoryDisplayName { get; init; } = string.Empty;
    public string? TierGroup { get; init; }
    public bool IsPrimaryLicense { get; init; }
    public decimal EstimatedMonthlyPricePerUser { get; init; }
    public List<string> IncludedFeatures { get; init; } = new();
}

public record LicenseUtilizationDto
{
    public int TotalLicenses { get; init; }
    public int AssignedLicenses { get; init; }
    public double UtilizationPercentage { get; init; }
    public int UsersWithoutMfa { get; init; }
    public int UsersWithoutCa { get; init; }
    public int UsersWithoutDefender { get; init; }
    public decimal EstimatedWaste { get; init; }
    public List<string> UsersWithoutMfaList { get; init; } = new();
    public List<string> UsersWithoutCaList { get; init; } = new();
    public List<string> UsersWithoutDefenderList { get; init; } = new();
}

public record ServiceHealthItemDto
{
    public string Id { get; init; } = string.Empty;
    public string Service { get; init; } = string.Empty;
    public string Status { get; init; } = string.Empty;
    public string Title { get; init; } = string.Empty;
    public DateTime? StartDateTime { get; init; }
}

#endregion

#region Multi-License DTOs

/// <summary>
/// Summary of licenses for a specific category.
/// </summary>
public record LicenseSummaryDto
{
    public LicenseCategory Category { get; init; }
    public string DisplayName { get; init; } = string.Empty;
    public string ShortName { get; init; } = string.Empty;
    public string? TierGroup { get; init; }
    public string ColorHex { get; init; } = string.Empty;
    public int TotalLicenses { get; init; }
    public int AssignedLicenses { get; init; }
    public int AvailableLicenses { get; init; }
    public double UtilizationPercentage { get; init; }
    public decimal EstimatedMonthlyPricePerUser { get; init; }
    public decimal TotalMonthlyCost { get; init; }
    public decimal EstimatedWaste { get; init; }
    public List<string> IncludedFeatures { get; init; } = new();
    public bool IsPrimaryLicense { get; init; }
}

/// <summary>
/// License utilization broken down by license type.
/// </summary>
public record LicenseUtilizationByTypeDto
{
    public LicenseCategory Category { get; init; }
    public string DisplayName { get; init; } = string.Empty;
    public string? TierGroup { get; init; }
    public int TotalUsers { get; init; }
    public int ActiveUsers { get; init; }
    public int DisabledUsers { get; init; }
    public int UsersUsingMfa { get; init; }
    public int UsersUsingCa { get; init; }
    public int UsersUsingDefender { get; init; }
    public int UsersUsingPurview { get; init; }
    public double OverallFeatureUtilization { get; init; }
    public double IdentityFeatureUtilization { get; init; }
    public double SecurityFeatureUtilization { get; init; }
    public double ComplianceFeatureUtilization { get; init; }
    public decimal EstimatedMonthlyWaste { get; init; }
    public int UsersEligibleForDowngrade { get; init; }
    public List<FeatureUtilizationDto> FeatureBreakdown { get; init; } = new();
}

/// <summary>
/// Utilization metrics for a specific feature.
/// </summary>
public record FeatureUtilizationDto
{
    public string FeatureName { get; init; } = string.Empty;
    public string FeatureCategory { get; init; } = string.Empty;
    public bool IsIncludedInLicense { get; init; }
    public int UsersEligible { get; init; }
    public int UsersUsing { get; init; }
    public int UsersNotUsing { get; init; }
    public double UtilizationPercentage { get; init; }
}

/// <summary>
/// Dashboard summary of license distribution across all categories.
/// </summary>
public record LicenseDistributionDto
{
    public int TotalLicenses { get; init; }
    public int TotalAssigned { get; init; }
    public int TotalAvailable { get; init; }
    public double OverallUtilization { get; init; }
    public int TotalLicensedUsers { get; init; }
    public int TotalUnlicensedUsers { get; init; }
    public decimal TotalMonthlyCost { get; init; }
    public decimal TotalEstimatedWaste { get; init; }
    public decimal TotalEstimatedMonthlyWaste => TotalEstimatedWaste;  // Alias for UI compatibility
    public List<LicenseSummaryDto> ByCategory { get; init; } = new();
    public List<TierGroupSummaryDto> ByTierGroup { get; init; } = new();
}

/// <summary>
/// Summary of licenses grouped by tier (Enterprise, Business, Frontline, etc.).
/// </summary>
public record TierGroupSummaryDto
{
    public string TierGroup { get; init; } = string.Empty;
    public int TotalLicenses { get; init; }
    public int AssignedLicenses { get; init; }
    public int AvailableLicenses { get; init; }
    public double UtilizationPercentage { get; init; }
    public decimal TotalMonthlyCost { get; init; }
    public List<LicenseCategory> Categories { get; init; } = new();
}

/// <summary>
/// Comprehensive multi-license utilization dashboard.
/// </summary>
public record MultiLicenseUtilizationDto
{
    public Guid TenantId { get; init; }
    public DateTime? LastCollected { get; init; }

    // Overall metrics
    public int TotalLicenses { get; init; }
    public int TotalAssigned { get; init; }
    public int TotalAssignedLicenses => TotalAssigned;  // Alias for UI compatibility
    public double OverallUtilization { get; init; }
    public double OverallAssignmentPercentage => TotalLicenses > 0 ? (double)TotalAssigned / TotalLicenses * 100 : 0;
    public double OverallFeatureUtilization => OverallUtilization;  // Alias for UI compatibility
    public decimal TotalMonthlyCost { get; init; }
    public decimal TotalEstimatedWaste { get; init; }
    public decimal TotalEstimatedMonthlyWaste => TotalEstimatedWaste;  // Alias for UI compatibility
    public decimal TotalAnnualWaste { get; init; }

    // Aggregate feature utilization
    public int TotalUsersWithoutMfa { get; init; }
    public int TotalUsersWithoutCa { get; init; }
    public int TotalUsersWithoutDefender { get; init; }

    // By category
    public List<LicenseUtilizationByTypeDto> UtilizationByCategory { get; init; } = new();

    // By tier group
    public List<TierGroupSummaryDto> TierGroups { get; init; } = new();

    // Distribution
    public LicenseDistributionDto Distribution { get; init; } = new();

    // Recommendations
    public List<LicenseRecommendationDto> Recommendations { get; init; } = new();
}

/// <summary>
/// License optimization recommendation.
/// </summary>
public record LicenseRecommendationDto
{
    public string Severity { get; init; } = string.Empty;
    public LicenseCategory? AffectedCategory { get; init; }
    public string Title { get; init; } = string.Empty;
    public string Description { get; init; } = string.Empty;
    public int AffectedUsers { get; init; }
    public decimal PotentialSavings { get; init; }
    public string? RecommendedAction { get; init; }
}

#endregion

#region Defender DTOs

/// <summary>
/// Microsoft Defender for Endpoint inventory DTO.
/// </summary>
public record DefenderForEndpointDto
{
    public Guid Id { get; init; }
    public Guid TenantId { get; init; }
    public DateTime? CollectedAt { get; init; }

    // Onboarding
    public int OnboardedDeviceCount { get; init; }
    public int TotalManagedDeviceCount { get; init; }
    public double OnboardingCoverage { get; init; }
    public int WindowsOnboarded { get; init; }
    public int MacOsOnboarded { get; init; }
    public int LinuxOnboarded { get; init; }
    public int MobileOnboarded { get; init; }

    // Sensor Health
    public int ActiveSensors { get; init; }
    public int InactiveSensors { get; init; }
    public int MisconfiguredSensors { get; init; }
    public int ImpairedCommunication { get; init; }
    public int NoSensorData { get; init; }

    // Risk Distribution
    public int HighRiskDevices { get; init; }
    public int MediumRiskDevices { get; init; }
    public int LowRiskDevices { get; init; }
    public int NoRiskInfoDevices { get; init; }

    // Features
    public bool TamperProtectionEnabled { get; init; }
    public bool EdrInBlockMode { get; init; }
    public bool NetworkProtectionEnabled { get; init; }
    public bool WebProtectionEnabled { get; init; }
    public bool CloudProtectionEnabled { get; init; }
    public bool PuaProtectionEnabled { get; init; }
    public bool RealTimeProtectionEnabled { get; init; }

    // ASR Rules
    public bool AsrRulesConfigured { get; init; }
    public int AsrRulesCount { get; init; }
    public int AsrRulesBlockMode { get; init; }
    public int AsrRulesAuditMode { get; init; }

    // TVM
    public double? ExposureScore { get; init; }
    public double? SecureScore { get; init; }
    public int VulnerabilityCount { get; init; }
    public int CriticalVulnerabilities { get; init; }
    public int HighVulnerabilities { get; init; }
    public int MediumVulnerabilities { get; init; }
    public int MissingPatches { get; init; }
    public int MissingKbCount { get; init; }

    // Alerts
    public int ActiveAlerts { get; init; }
    public int HighSeverityAlerts { get; init; }
    public int MediumSeverityAlerts { get; init; }
    public int LowSeverityAlerts { get; init; }
    public int InformationalAlerts { get; init; }
}

/// <summary>
/// Microsoft Defender for Office 365 inventory DTO.
/// </summary>
public record DefenderForOffice365Dto
{
    public Guid Id { get; init; }
    public Guid TenantId { get; init; }
    public DateTime? CollectedAt { get; init; }

    // Safe Links
    public bool SafeLinksEnabled { get; init; }
    public int SafeLinksPolicyCount { get; init; }
    public bool SafeLinksForOfficeApps { get; init; }
    public bool SafeLinksTrackUserClicks { get; init; }

    // Safe Attachments
    public bool SafeAttachmentsEnabled { get; init; }
    public int SafeAttachmentsPolicyCount { get; init; }
    public string? SafeAttachmentsMode { get; init; }
    public bool SafeAttachmentsForSharePoint { get; init; }

    // Anti-Phish
    public int AntiPhishPolicyCount { get; init; }
    public bool ImpersonationProtectionEnabled { get; init; }
    public bool MailboxIntelligenceEnabled { get; init; }
    public bool SpoofIntelligenceEnabled { get; init; }
    public int ProtectedUsersCount { get; init; }
    public int ProtectedDomainsCount { get; init; }

    // Anti-Spam
    public int AntiSpamPolicyCount { get; init; }
    public string? DefaultSpamAction { get; init; }
    public string? HighConfidenceSpamAction { get; init; }

    // Anti-Malware
    public int AntiMalwarePolicyCount { get; init; }
    public bool CommonAttachmentTypesFilter { get; init; }
    public bool ZeroHourAutoPurgeEnabled { get; init; }

    // Email Auth
    public bool DkimEnabled { get; init; }
    public int DkimEnabledDomains { get; init; }
    public bool DmarcEnabled { get; init; }
    public string? DmarcPolicy { get; init; }
    public bool SpfConfigured { get; init; }

    // Threat Summary
    public int Last30DaysMalwareCount { get; init; }
    public int Last30DaysPhishCount { get; init; }
    public int Last30DaysSpamCount { get; init; }
    public int Last30DaysBlockedCount { get; init; }
}

/// <summary>
/// Microsoft Defender for Identity inventory DTO.
/// </summary>
public record DefenderForIdentityDto
{
    public Guid Id { get; init; }
    public Guid TenantId { get; init; }
    public DateTime? CollectedAt { get; init; }

    // Configuration
    public bool IsConfigured { get; init; }
    public bool IsLicensed { get; init; }
    public string? WorkspaceId { get; init; }

    // Sensors
    public int SensorCount { get; init; }
    public int HealthySensors { get; init; }
    public int UnhealthySensors { get; init; }
    public int OfflineSensors { get; init; }

    // DC Coverage
    public int DomainControllersCovered { get; init; }
    public int TotalDomainControllers { get; init; }
    public double CoveragePercentage { get; init; }

    // Health Issues
    public int OpenHealthIssues { get; init; }
    public int HighSeverityHealthIssues { get; init; }
    public int MediumSeverityHealthIssues { get; init; }
    public int LowSeverityHealthIssues { get; init; }

    // Alerts
    public int HighSeverityAlerts { get; init; }
    public int MediumSeverityAlerts { get; init; }
    public int LowSeverityAlerts { get; init; }
    public int Last30DaysAlerts { get; init; }

    // Detection Config
    public bool HoneytokenAccountsConfigured { get; init; }
    public int HoneytokenAccountCount { get; init; }
    public bool SensitiveGroupsConfigured { get; init; }
}

/// <summary>
/// Microsoft Defender for Cloud Apps (MCAS) inventory DTO.
/// </summary>
public record DefenderForCloudAppsDto
{
    public Guid Id { get; init; }
    public Guid TenantId { get; init; }
    public DateTime? CollectedAt { get; init; }

    // Connected Apps
    public int ConnectedAppCount { get; init; }
    public bool Office365Connected { get; init; }
    public bool AzureConnected { get; init; }
    public bool AwsConnected { get; init; }
    public bool GcpConnected { get; init; }

    // OAuth Apps
    public int OAuthAppCount { get; init; }
    public int HighRiskOAuthApps { get; init; }
    public int MediumRiskOAuthApps { get; init; }
    public int LowRiskOAuthApps { get; init; }

    // App Governance
    public bool AppGovernanceEnabled { get; init; }
    public int AppGovernancePolicyCount { get; init; }
    public int AppGovernanceAlerts { get; init; }

    // Policies
    public int ActivityPolicyCount { get; init; }
    public int AnomalyPolicyCount { get; init; }
    public int SessionPolicyCount { get; init; }
    public int AccessPolicyCount { get; init; }
    public int FilePolicyCount { get; init; }
    public int TotalPolicyCount => ActivityPolicyCount + AnomalyPolicyCount + SessionPolicyCount + AccessPolicyCount + FilePolicyCount;
    public int EnabledPolicies { get; init; }
    public int DisabledPolicies { get; init; }

    // Shadow IT Discovery
    public bool CloudDiscoveryEnabled { get; init; }
    public int DiscoveredAppCount { get; init; }
    public int SanctionedApps { get; init; }
    public int UnsanctionedApps { get; init; }
    public int MonitoredApps { get; init; }

    // Alerts
    public int OpenAlerts { get; init; }
    public int HighSeverityAlerts { get; init; }
    public int MediumSeverityAlerts { get; init; }
    public int LowSeverityAlerts { get; init; }

    // Session Control
    public bool SessionControlEnabled { get; init; }
    public int SessionControlledApps { get; init; }
}

#endregion
