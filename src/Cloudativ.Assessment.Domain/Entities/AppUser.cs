using Cloudativ.Assessment.Domain.Enums;

namespace Cloudativ.Assessment.Domain.Entities;

public class AppUser : BaseEntity
{
    public string Email { get; set; } = string.Empty;
    public string DisplayName { get; set; } = string.Empty;
    public string? PasswordHash { get; set; }
    public AppRole Role { get; set; } = AppRole.Auditor;
    public bool IsActive { get; set; } = true;
    public bool IsExternalAuth { get; set; } = false;
    public string? ExternalAuthProvider { get; set; }
    public string? ExternalAuthId { get; set; }
    public DateTime? LastLoginAt { get; set; }
    public string? RefreshToken { get; set; }
    public DateTime? RefreshTokenExpiry { get; set; }

    // For multi-tenant access control
    public virtual ICollection<TenantUserAccess> TenantAccess { get; set; } = new List<TenantUserAccess>();

    // For domain-level admin access control
    public virtual ICollection<UserDomainAccess> DomainAccess { get; set; } = new List<UserDomainAccess>();
}

public class TenantUserAccess : BaseEntity
{
    public Guid AppUserId { get; set; }
    public Guid TenantId { get; set; }
    public AppRole TenantRole { get; set; } = AppRole.Auditor;
    public bool IsDefault { get; set; } = false;

    // Navigation properties
    public virtual AppUser AppUser { get; set; } = null!;
    public virtual Tenant Tenant { get; set; } = null!;
}

/// <summary>
/// Stores which assessment domains a Domain-level admin user has access to
/// </summary>
public class UserDomainAccess : BaseEntity
{
    public Guid AppUserId { get; set; }
    public AssessmentDomain Domain { get; set; }

    // Navigation property
    public virtual AppUser AppUser { get; set; } = null!;
}
