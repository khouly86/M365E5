using Cloudativ.Assessment.Application.DTOs;

namespace Cloudativ.Assessment.Application.Interfaces;

/// <summary>
/// Service for automated Azure AD App Registration setup via delegated admin login.
/// Uses interactive browser authentication with Global Admin credentials to create
/// and configure the required app registration in the target tenant.
/// </summary>
public interface IAzureAdSetupService
{
    /// <summary>
    /// Creates an Azure AD App Registration with all required permissions,
    /// client secret, service principal, and admin consent grants.
    /// </summary>
    /// <param name="tenantId">The Azure AD Tenant ID (GUID string).</param>
    /// <param name="domain">The tenant domain (used in the app display name).</param>
    /// <param name="progressCallback">Optional callback for step-by-step progress updates.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    Task<AzureAdSetupResult> SetupAppRegistrationAsync(
        string tenantId,
        string domain,
        Action<AzureAdSetupProgress>? progressCallback = null,
        CancellationToken cancellationToken = default);
}
