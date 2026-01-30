using System.ComponentModel.DataAnnotations;
using Cloudativ.Assessment.Domain.Enums;

namespace Cloudativ.Assessment.Application.DTOs;

public record AppUserDto
{
    public Guid Id { get; init; }
    public string Email { get; init; } = string.Empty;
    public string DisplayName { get; init; } = string.Empty;
    public AppRole Role { get; init; }
    public bool IsActive { get; init; }
    public bool IsExternalAuth { get; init; }
    public string? ExternalAuthProvider { get; init; }
    public DateTime? LastLoginAt { get; init; }
    public DateTime CreatedAt { get; init; }
    public List<TenantAccessDto> TenantAccess { get; init; } = new();
    public List<AssessmentDomain> AllowedDomains { get; init; } = new();
}

public record CreateUserDto
{
    [Required(ErrorMessage = "Email is required")]
    [EmailAddress(ErrorMessage = "Invalid email address")]
    [StringLength(256, ErrorMessage = "Email cannot exceed 256 characters")]
    public string Email { get; init; } = string.Empty;

    [Required(ErrorMessage = "Display name is required")]
    [StringLength(100, MinimumLength = 2, ErrorMessage = "Display name must be between 2 and 100 characters")]
    public string DisplayName { get; init; } = string.Empty;

    [Required(ErrorMessage = "Password is required")]
    [StringLength(100, MinimumLength = 8, ErrorMessage = "Password must be at least 8 characters")]
    [RegularExpression(@"^(?=.*[a-z])(?=.*[A-Z])(?=.*\d)(?=.*[@$!%*?&])[A-Za-z\d@$!%*?&]{8,}$",
        ErrorMessage = "Password must contain at least one uppercase, one lowercase, one number, and one special character")]
    public string Password { get; init; } = string.Empty;

    [Required(ErrorMessage = "Role is required")]
    public AppRole Role { get; init; } = AppRole.Auditor;

    public List<Guid> TenantIds { get; init; } = new();

    /// <summary>
    /// Assessment domains that a Domain-level admin has access to.
    /// Only applicable when Role is DomainAdmin.
    /// </summary>
    public List<AssessmentDomain> AllowedDomains { get; init; } = new();
}

public record UpdateUserDto
{
    public Guid Id { get; init; }

    [Required(ErrorMessage = "Display name is required")]
    [StringLength(100, MinimumLength = 2, ErrorMessage = "Display name must be between 2 and 100 characters")]
    public string DisplayName { get; init; } = string.Empty;

    [Required(ErrorMessage = "Role is required")]
    public AppRole Role { get; init; }

    public bool IsActive { get; init; }

    public List<Guid> TenantIds { get; init; } = new();

    /// <summary>
    /// Assessment domains that a Domain-level admin has access to.
    /// Only applicable when Role is DomainAdmin.
    /// </summary>
    public List<AssessmentDomain> AllowedDomains { get; init; } = new();
}

public record ResetPasswordDto
{
    public Guid UserId { get; init; }

    [Required(ErrorMessage = "Password is required")]
    [StringLength(100, MinimumLength = 8, ErrorMessage = "Password must be at least 8 characters")]
    [RegularExpression(@"^(?=.*[a-z])(?=.*[A-Z])(?=.*\d)(?=.*[@$!%*?&])[A-Za-z\d@$!%*?&]{8,}$",
        ErrorMessage = "Password must contain at least one uppercase, one lowercase, one number, and one special character")]
    public string NewPassword { get; init; } = string.Empty;
}

public record TenantAccessDto
{
    public Guid TenantId { get; init; }
    public string TenantName { get; init; } = string.Empty;
}
