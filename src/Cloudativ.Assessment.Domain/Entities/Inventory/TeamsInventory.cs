namespace Cloudativ.Assessment.Domain.Entities.Inventory;

/// <summary>
/// Microsoft Teams inventory.
/// </summary>
public class TeamsInventory : BaseEntity
{
    public Guid TenantId { get; set; }
    public Guid SnapshotId { get; set; }

    public string TeamId { get; set; } = string.Empty;
    public string DisplayName { get; set; } = string.Empty;
    public string? Description { get; set; }
    public string Visibility { get; set; } = string.Empty;
    public DateTime? CreatedDateTime { get; set; }
    public string? Classification { get; set; }

    // Associated Group
    public string? GroupId { get; set; }
    public string? MailNickname { get; set; }

    // Members
    public int MemberCount { get; set; }
    public int OwnerCount { get; set; }
    public int GuestCount { get; set; }
    public bool HasExternalMembers { get; set; }
    public string? OwnersJson { get; set; }

    // Channels
    public int TotalChannelCount { get; set; }
    public int StandardChannelCount { get; set; }
    public int PrivateChannelCount { get; set; }
    public int SharedChannelCount { get; set; }
    public string? ChannelsJson { get; set; }

    // Activity
    public bool IsArchived { get; set; }
    public DateTime? LastActivityDate { get; set; }
    public int InactiveDays { get; set; }
    public int Last30DaysActiveUsers { get; set; }
    public int Last30DaysMessages { get; set; }
    public int Last30DaysMeetings { get; set; }

    // Settings
    public bool AllowCreateUpdateChannels { get; set; }
    public bool AllowDeleteChannels { get; set; }
    public bool AllowAddRemoveApps { get; set; }
    public bool AllowCreateUpdateRemoveTabs { get; set; }
    public bool AllowCreateUpdateRemoveConnectors { get; set; }

    // Guest Settings
    public bool AllowGuestCreateUpdateChannels { get; set; }
    public bool AllowGuestDeleteChannels { get; set; }

    // Fun Settings
    public bool AllowGiphy { get; set; }
    public bool AllowStickersAndMemes { get; set; }
    public bool AllowCustomMemes { get; set; }

    // Messaging Settings
    public bool AllowUserEditMessages { get; set; }
    public bool AllowUserDeleteMessages { get; set; }
    public bool AllowOwnerDeleteMessages { get; set; }
    public bool AllowTeamMentions { get; set; }
    public bool AllowChannelMentions { get; set; }

    // Sensitivity
    public string? SensitivityLabel { get; set; }
    public string? SensitivityLabelId { get; set; }

    // Apps
    public int InstalledAppCount { get; set; }
    public int TabCount { get; set; }
    public int ConnectorCount { get; set; }

    // Navigation
    public virtual Tenant Tenant { get; set; } = null!;
    public virtual InventorySnapshot Snapshot { get; set; } = null!;
}
