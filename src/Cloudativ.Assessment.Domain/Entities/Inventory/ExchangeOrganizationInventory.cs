namespace Cloudativ.Assessment.Domain.Entities.Inventory;

/// <summary>
/// Exchange Online organization settings and mail flow configuration.
/// </summary>
public class ExchangeOrganizationInventory : BaseEntity
{
    public Guid TenantId { get; set; }
    public Guid SnapshotId { get; set; }

    // Mail Flow Connectors
    public int InboundConnectorCount { get; set; }
    public int OutboundConnectorCount { get; set; }
    public string? ConnectorsJson { get; set; }

    // Transport Rules
    public int TransportRuleCount { get; set; }
    public int EnabledTransportRules { get; set; }
    public string? TransportRulesJson { get; set; }

    // Accepted Domains
    public int AcceptedDomainCount { get; set; }
    public string? AcceptedDomainsJson { get; set; }
    public string? DefaultDomain { get; set; }

    // External Forwarding
    public int UsersWithExternalForwarding { get; set; }
    public string? ExternalForwardingUsersJson { get; set; }
    public bool ExternalForwardingBlocked { get; set; }
    public string? RemoteDomainsJson { get; set; }

    // Mailboxes Summary
    public int UserMailboxCount { get; set; }
    public int SharedMailboxCount { get; set; }
    public int ResourceMailboxCount { get; set; }
    public int EquipmentMailboxCount { get; set; }
    public int RoomMailboxCount { get; set; }
    public int GroupMailboxCount { get; set; }
    public int InactiveMailboxCount { get; set; }
    public int ArchivedMailboxCount { get; set; }

    // Mail-Enabled Groups
    public int DistributionGroupCount { get; set; }
    public int SecurityGroupMailEnabledCount { get; set; }
    public int DynamicDistributionGroupCount { get; set; }

    // Authentication
    public bool ModernAuthEnabled { get; set; }
    public bool BasicAuthEnabledForPop { get; set; }
    public bool BasicAuthEnabledForImap { get; set; }
    public bool BasicAuthEnabledForSmtp { get; set; }
    public bool BasicAuthEnabledForEws { get; set; }
    public bool BasicAuthEnabledForOutlook { get; set; }
    public string? AuthPoliciesJson { get; set; }

    // Retention & Compliance
    public int RetentionPolicyCount { get; set; }
    public int RetentionTagCount { get; set; }
    public int LitigationHoldCount { get; set; }
    public int InPlaceHoldCount { get; set; }
    public bool MailboxAuditingDefault { get; set; }
    public int JournalRuleCount { get; set; }

    // Organization Config
    public bool EwsEnabled { get; set; }
    public bool PopEnabled { get; set; }
    public bool ImapEnabled { get; set; }
    public bool MobileDeviceAccessEnabled { get; set; }
    public string? DefaultPublicFolderMailbox { get; set; }

    // Navigation
    public virtual Tenant Tenant { get; set; } = null!;
    public virtual InventorySnapshot Snapshot { get; set; } = null!;
}
