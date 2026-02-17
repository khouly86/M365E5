namespace Cloudativ.Assessment.Domain.Entities.Inventory;

/// <summary>
/// DLP policy inventory from Microsoft Purview.
/// </summary>
public class DlpPolicyInventory : BaseEntity
{
    public Guid TenantId { get; set; }
    public Guid SnapshotId { get; set; }

    public string PolicyId { get; set; } = string.Empty;
    public string DisplayName { get; set; } = string.Empty;
    public string? Description { get; set; }
    public bool IsEnabled { get; set; }
    public string Mode { get; set; } = string.Empty;
    public int Priority { get; set; }
    public DateTime? CreatedDateTime { get; set; }
    public DateTime? LastModifiedDateTime { get; set; }

    // Workload Scope
    public string? WorkloadsJson { get; set; }
    public bool AppliesToExchange { get; set; }
    public bool AppliesToSharePoint { get; set; }
    public bool AppliesToOneDrive { get; set; }
    public bool AppliesToTeams { get; set; }
    public bool AppliesToEndpoint { get; set; }
    public bool AppliesToPowerBI { get; set; }
    public bool AppliesToThirdPartyApps { get; set; }

    // Rules
    public int RuleCount { get; set; }
    public int EnabledRuleCount { get; set; }
    public string? RulesJson { get; set; }

    // Sensitive Info Types
    public string? SensitiveInfoTypesJson { get; set; }
    public int SensitiveInfoTypeCount { get; set; }

    // Trainable Classifiers
    public bool UsesTrainableClassifiers { get; set; }
    public string? TrainableClassifiersJson { get; set; }
    public int TrainableClassifierCount { get; set; }

    // EDM (Exact Data Match)
    public bool UsesExactDataMatch { get; set; }
    public string? EdmSchemaNames { get; set; }

    // Actions
    public bool BlocksContent { get; set; }
    public bool NotifiesUser { get; set; }
    public bool GeneratesAlert { get; set; }
    public bool GeneratesIncidentReport { get; set; }
    public bool EncryptsContent { get; set; }

    // Statistics (if available)
    public int? Last30DaysMatches { get; set; }
    public int? Last30DaysIncidents { get; set; }

    // Navigation
    public virtual Tenant Tenant { get; set; } = null!;
    public virtual InventorySnapshot Snapshot { get; set; } = null!;
}
