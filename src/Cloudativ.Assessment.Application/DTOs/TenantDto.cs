using Cloudativ.Assessment.Domain.Enums;

namespace Cloudativ.Assessment.Application.DTOs;

public record TenantDto
{
    public Guid Id { get; init; }
    public string Name { get; init; } = string.Empty;
    public string Domain { get; init; } = string.Empty;
    public Guid? AzureTenantId { get; init; }
    public OnboardingStatus OnboardingStatus { get; init; }
    public string? Industry { get; init; }
    public string? ContactEmail { get; init; }
    public DateTime CreatedAt { get; init; }
    public int? LastAssessmentScore { get; init; }
    public DateTime? LastAssessmentDate { get; init; }
    public int TotalAssessments { get; init; }
    public List<string> SelectedComplianceStandards { get; init; } = new();
}

public record TenantCreateDto
{
    public string Name { get; init; } = string.Empty;
    public string Domain { get; init; } = string.Empty;
    public Guid? AzureTenantId { get; init; }
    public string? Industry { get; init; }
    public string? ContactEmail { get; init; }
    public string AuthenticationType { get; init; } = "AppRegistration"; // "AppRegistration" or "PortalAdmin"
    public string? ClientId { get; init; }
    public string? SecretId { get; init; }
    public string? SecretValue { get; init; }
    public List<string> ComplianceStandards { get; init; } = new();
}

public record TenantUpdateDto
{
    public string Name { get; init; } = string.Empty;
    public string? Industry { get; init; }
    public string? ContactEmail { get; init; }
    public string? Notes { get; init; }
}

public record TenantOnboardingDto
{
    public Guid TenantId { get; init; }
    public string ClientId { get; init; } = string.Empty;
    public string ClientSecret { get; init; } = string.Empty;
    public List<string> RequiredPermissions { get; init; } = new();
    public List<string> GrantedPermissions { get; init; } = new();
    public List<string> MissingPermissions { get; init; } = new();
    public bool ConnectionValid { get; init; }
    public string? ValidationMessage { get; init; }
}

public record ConnectionTestResult
{
    public bool Success { get; init; }
    public string? ErrorMessage { get; init; }
    public string? ErrorCode { get; init; }
    public List<string> RequiredPermissions { get; init; } = new();
    public List<string> GrantedPermissions { get; init; } = new();
    public List<string> MissingPermissions { get; init; } = new();
    public string? OrganizationName { get; init; }
    public string? OrganizationId { get; init; }
}
