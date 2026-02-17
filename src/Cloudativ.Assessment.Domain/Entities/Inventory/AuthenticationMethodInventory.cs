namespace Cloudativ.Assessment.Domain.Entities.Inventory;

/// <summary>
/// Authentication methods policy and registration summary.
/// </summary>
public class AuthenticationMethodInventory : BaseEntity
{
    public Guid TenantId { get; set; }
    public Guid SnapshotId { get; set; }

    // Policy Status
    public bool MicrosoftAuthenticatorEnabled { get; set; }
    public bool Fido2Enabled { get; set; }
    public bool SmsEnabled { get; set; }
    public bool VoiceEnabled { get; set; }
    public bool EmailOtpEnabled { get; set; }
    public bool SoftwareOathEnabled { get; set; }
    public bool TemporaryAccessPassEnabled { get; set; }
    public bool CertificateBasedAuthEnabled { get; set; }

    // Target settings (JSON)
    public string? MicrosoftAuthenticatorTargetJson { get; set; }
    public string? Fido2TargetJson { get; set; }
    public string? SmsTargetJson { get; set; }
    public string? PolicySettingsJson { get; set; }

    // Registration Statistics
    public int TotalUsers { get; set; }
    public int MfaRegisteredUsers { get; set; }
    public int MfaCapableUsers { get; set; }
    public int PasswordlessCapableUsers { get; set; }
    public int AuthenticatorAppUsers { get; set; }
    public int Fido2Users { get; set; }
    public int PhoneAuthUsers { get; set; }
    public int SoftwareOathUsers { get; set; }

    // Legacy MFA
    public int PerUserMfaEnabledCount { get; set; }
    public int PerUserMfaEnforcedCount { get; set; }

    // SSPR
    public bool SsprEnabled { get; set; }
    public string? SsprScope { get; set; }
    public int SsprRegisteredUsers { get; set; }

    // Password Policy
    public int PasswordExpirationDays { get; set; }
    public bool PasswordNeverExpires { get; set; }
    public bool SmartLockoutEnabled { get; set; }
    public int SmartLockoutThreshold { get; set; }
    public int SmartLockoutDurationSeconds { get; set; }
    public bool BannedPasswordsEnabled { get; set; }
    public bool EnforceCustomBannedPasswords { get; set; }

    // Navigation
    public virtual Tenant Tenant { get; set; } = null!;
    public virtual InventorySnapshot Snapshot { get; set; } = null!;
}
