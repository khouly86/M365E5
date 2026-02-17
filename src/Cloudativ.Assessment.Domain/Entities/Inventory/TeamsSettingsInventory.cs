namespace Cloudativ.Assessment.Domain.Entities.Inventory;

/// <summary>
/// Teams tenant-level settings and policies.
/// </summary>
public class TeamsSettingsInventory : BaseEntity
{
    public Guid TenantId { get; set; }
    public Guid SnapshotId { get; set; }

    // External Access
    public bool AllowFederatedUsers { get; set; }
    public bool AllowTeamsConsumer { get; set; }
    public bool AllowTeamsConsumerInbound { get; set; }
    public bool AllowSkypeBusinessInterop { get; set; }
    public bool AllowPublicUsers { get; set; }
    public string? AllowedDomainsJson { get; set; }
    public string? BlockedDomainsJson { get; set; }

    // Guest Access
    public bool AllowGuestAccess { get; set; }
    public bool GuestCanCreateChannels { get; set; }
    public bool GuestCanDeleteChannels { get; set; }
    public bool AllowGuestUserToAccessMeetings { get; set; }

    // Meeting Policies Summary
    public string? MeetingPoliciesJson { get; set; }
    public int MeetingPolicyCount { get; set; }
    public bool DefaultAllowAnonymousJoin { get; set; }
    public bool DefaultAllowRecording { get; set; }
    public bool DefaultAllowTranscription { get; set; }
    public bool DefaultAllowCloudRecording { get; set; }
    public bool DefaultAllowIPVideo { get; set; }
    public string? DefaultScreenSharingMode { get; set; }
    public bool DefaultAllowPrivateMeetNow { get; set; }

    // Messaging Policies Summary
    public string? MessagingPoliciesJson { get; set; }
    public int MessagingPolicyCount { get; set; }
    public bool DefaultAllowOwnerDeleteMessage { get; set; }
    public bool DefaultAllowUserEditMessage { get; set; }
    public bool DefaultAllowUserDeleteMessage { get; set; }
    public bool DefaultAllowUrlPreviews { get; set; }

    // Apps
    public bool AllowThirdPartyApps { get; set; }
    public bool AllowSideloading { get; set; }
    public bool DefaultCatalogAppsEnabled { get; set; }
    public bool ExternalCatalogAppsEnabled { get; set; }
    public bool CustomAppsEnabled { get; set; }
    public int BlockedAppsCount { get; set; }
    public string? BlockedAppsJson { get; set; }
    public string? AppPermissionPoliciesJson { get; set; }
    public int AppPermissionPolicyCount { get; set; }

    // App Setup Policies
    public string? AppSetupPoliciesJson { get; set; }
    public int AppSetupPolicyCount { get; set; }

    // Calling Policies
    public string? CallingPoliciesJson { get; set; }
    public int CallingPolicyCount { get; set; }
    public bool DefaultAllowPrivateCalling { get; set; }
    public bool DefaultAllowVoicemail { get; set; }
    public bool DefaultAllowCallForwarding { get; set; }

    // Live Events Policies
    public string? LiveEventsPoliciesJson { get; set; }
    public int LiveEventsPolicyCount { get; set; }
    public bool DefaultAllowBroadcastScheduling { get; set; }
    public bool DefaultAllowBroadcastTranscription { get; set; }

    // Teams Summary
    public int TotalTeamsCount { get; set; }
    public int ActiveTeamsCount { get; set; }
    public int ArchivedTeamsCount { get; set; }
    public int TeamsWithGuests { get; set; }
    public int PrivateChannelCount { get; set; }
    public int SharedChannelCount { get; set; }

    // Navigation
    public virtual Tenant Tenant { get; set; } = null!;
    public virtual InventorySnapshot Snapshot { get; set; } = null!;
}
