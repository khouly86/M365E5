namespace Cloudativ.Assessment.Application.DTOs;

/// <summary>
/// Result of the automated Azure AD App Registration setup.
/// </summary>
public record AzureAdSetupResult
{
    public bool Success { get; init; }
    public string? ErrorMessage { get; init; }
    public string? ErrorCode { get; init; }

    // Credentials
    public string? ClientId { get; init; }
    public string? ObjectId { get; init; }
    public string? SecretId { get; init; }
    public string? SecretValue { get; init; }
    public DateTime? SecretExpiry { get; init; }

    // App info
    public string? AppDisplayName { get; init; }
    public string? ServicePrincipalId { get; init; }

    // Permissions
    public int TotalPermissionsRequested { get; init; }
    public int PermissionsGranted { get; init; }
    public List<string> GrantedPermissions { get; init; } = new();
    public List<string> FailedPermissions { get; init; } = new();

    // Organization
    public string? OrganizationName { get; init; }
    public string? OrganizationId { get; init; }
}

/// <summary>
/// Progress update during the Azure AD setup process.
/// </summary>
public record AzureAdSetupProgress
{
    public string CurrentStep { get; init; } = string.Empty;
    public int StepNumber { get; init; }
    public int TotalSteps { get; init; } = 6;
    public bool IsComplete { get; init; }
    public string? ErrorMessage { get; init; }
}
