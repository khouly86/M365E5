namespace Cloudativ.Assessment.Domain.Entities.Inventory;

/// <summary>
/// Audit log configuration and SIEM integration status.
/// </summary>
public class AuditLogInventory : BaseEntity
{
    public Guid TenantId { get; set; }
    public Guid SnapshotId { get; set; }

    // Unified Audit Log
    public bool UnifiedAuditLogEnabled { get; set; }
    public int RetentionDays { get; set; }
    public int DefaultRetentionDays { get; set; }
    public bool AdvancedAuditEnabled { get; set; }
    public int AdvancedAuditRetentionDays { get; set; }

    // Mailbox Auditing
    public bool MailboxAuditingEnabled { get; set; }
    public bool MailboxAuditingByDefaultEnabled { get; set; }
    public int MailboxAuditLogAgeLimit { get; set; }

    // Sign-in Logs
    public bool SignInLogsAvailable { get; set; }
    public int SignInLogRetentionDays { get; set; }

    // Activity Logs
    public bool AzureActivityLogsEnabled { get; set; }
    public bool SharePointAuditingEnabled { get; set; }
    public bool TeamsAuditingEnabled { get; set; }
    public bool ExchangeAuditingEnabled { get; set; }

    // SIEM Integration
    public bool HasSentinelIntegration { get; set; }
    public bool HasSplunkIntegration { get; set; }
    public bool HasOtherSiemIntegration { get; set; }
    public string? SiemIntegrationsJson { get; set; }

    // Diagnostic Settings
    public string? DiagnosticSettingsJson { get; set; }
    public int DiagnosticSettingCount { get; set; }
    public bool LogsToStorageAccount { get; set; }
    public bool LogsToLogAnalytics { get; set; }
    public bool LogsToEventHub { get; set; }

    // Alert Policies
    public int AlertPolicyCount { get; set; }
    public int EnabledAlertPolicies { get; set; }
    public int CustomAlertPolicies { get; set; }
    public int SystemAlertPolicies { get; set; }
    public string? AlertPoliciesJson { get; set; }

    // Threat Intelligence
    public bool ThreatIntelligenceEnabled { get; set; }

    // Activity Alerts
    public int ActivityAlertCount { get; set; }
    public string? ActivityAlertsJson { get; set; }

    // Security & Compliance Center Alerts
    public int SecurityAlertPolicyCount { get; set; }
    public int ComplianceAlertPolicyCount { get; set; }

    // Navigation
    public virtual Tenant Tenant { get; set; } = null!;
    public virtual InventorySnapshot Snapshot { get; set; } = null!;
}
