namespace Cloudativ.Assessment.Domain.Entities.Inventory;

/// <summary>
/// Compliance features inventory (Insider Risk, Communication Compliance, eDiscovery, etc.).
/// </summary>
public class ComplianceInventory : BaseEntity
{
    public Guid TenantId { get; set; }
    public Guid SnapshotId { get; set; }

    // Insider Risk Management
    public bool InsiderRiskEnabled { get; set; }
    public int InsiderRiskPolicyCount { get; set; }
    public int InsiderRiskEnabledPolicies { get; set; }
    public string? InsiderRiskPoliciesJson { get; set; }
    public int InsiderRiskOpenAlerts { get; set; }
    public int InsiderRiskOpenCases { get; set; }

    // Communication Compliance
    public bool CommunicationComplianceEnabled { get; set; }
    public int CommunicationCompliancePolicyCount { get; set; }
    public string? CommunicationCompliancePoliciesJson { get; set; }
    public int CommunicationCompliancePendingReview { get; set; }

    // Information Barriers
    public bool InformationBarriersEnabled { get; set; }
    public int InformationBarrierPolicyCount { get; set; }
    public int InformationBarrierSegmentCount { get; set; }
    public string? InformationBarrierPoliciesJson { get; set; }

    // eDiscovery
    public int ActiveEDiscoveryCases { get; set; }
    public int TotalEDiscoveryCases { get; set; }
    public int EDiscoveryStandardCases { get; set; }
    public int EDiscoveryPremiumCases { get; set; }
    public int ClosedEDiscoveryCases { get; set; }

    // Content Search
    public int ContentSearchCount { get; set; }
    public int ActiveContentSearches { get; set; }

    // Retention Labels
    public int RetentionLabelCount { get; set; }
    public int PublishedRetentionLabels { get; set; }
    public int AutoApplyRetentionLabels { get; set; }
    public string? RetentionLabelsJson { get; set; }

    // Retention Policies
    public int RetentionPolicyCount { get; set; }
    public string? RetentionPoliciesJson { get; set; }
    public bool RetentionForExchange { get; set; }
    public bool RetentionForSharePoint { get; set; }
    public bool RetentionForOneDrive { get; set; }
    public bool RetentionForTeams { get; set; }
    public bool RetentionForYammer { get; set; }

    // Records Management
    public bool RecordsManagementEnabled { get; set; }
    public int FilePlanCount { get; set; }
    public bool DispositionReviewEnabled { get; set; }

    // Audit
    public bool AdvancedAuditEnabled { get; set; }
    public int AuditRetentionDays { get; set; }

    // Navigation
    public virtual Tenant Tenant { get; set; } = null!;
    public virtual InventorySnapshot Snapshot { get; set; } = null!;
}
