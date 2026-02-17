using Azure.Identity;
using Cloudativ.Assessment.Application.DTOs;
using Cloudativ.Assessment.Application.Interfaces;
using Cloudativ.Assessment.Infrastructure.Graph;
using Microsoft.Extensions.Logging;
using Microsoft.Graph;
using Microsoft.Graph.Applications.Item.AddPassword;
using Microsoft.Graph.Models;
using GraphApplication = Microsoft.Graph.Models.Application;

namespace Cloudativ.Assessment.Infrastructure.Services;

public class AzureAdSetupService : IAzureAdSetupService
{
    private readonly IGraphClientFactory _graphClientFactory;
    private readonly ILogger<AzureAdSetupService> _logger;

    // Well-known Microsoft Graph Application ID (same across all Azure AD tenants)
    private const string MicrosoftGraphAppId = "00000003-0000-0000-c000-000000000000";

    // Well-known Application Permission GUIDs for Microsoft Graph
    // Reference: https://learn.microsoft.com/en-us/graph/permissions-reference
    private static readonly Dictionary<string, Guid> RequiredAppPermissions = new()
    {
        ["User.Read.All"] = Guid.Parse("df021288-bdef-4463-88db-98f22de89214"),
        ["Directory.Read.All"] = Guid.Parse("7ab1d382-f21e-4acd-a863-ba3e13f7da61"),
        ["RoleManagement.Read.Directory"] = Guid.Parse("483bed4a-2ad3-4361-a73b-c83ccdbdc53c"),
        ["Policy.Read.All"] = Guid.Parse("246dd0d5-5bd0-4def-940b-0421030a5b68"),
        ["AuditLog.Read.All"] = Guid.Parse("b0afded3-3588-46d8-8b3d-9842eff778da"),
        ["SecurityEvents.Read.All"] = Guid.Parse("bf394140-e372-4bf9-a898-299cfc7564e5"),
        ["IdentityRiskyUser.Read.All"] = Guid.Parse("dc5007c0-2d7d-4c42-879c-2dab87571379"),
        ["DeviceManagementConfiguration.Read.All"] = Guid.Parse("dc377aa6-52d8-4e23-b271-2a7ae04cedf3"),
        ["DeviceManagementManagedDevices.Read.All"] = Guid.Parse("2f51be20-0bb4-4fed-bf7b-db946066c75e"),
        ["Mail.Read"] = Guid.Parse("810c84a8-4a9e-49e6-bf7d-12d183f40d01"),
        ["MailboxSettings.Read"] = Guid.Parse("40f97065-369a-49f4-947c-6a90f8a0ece1"),
        ["Organization.Read.All"] = Guid.Parse("498476ce-e0fe-48b0-b801-37ba7e2685c6"),
        ["Application.Read.All"] = Guid.Parse("9a5d68dd-52b0-4cc2-bd40-abcf44ac3a30"),
        ["DelegatedPermissionGrant.Read.All"] = Guid.Parse("e3f15560-0397-4a50-8e93-c7892e80d77c"),
        ["InformationProtectionPolicy.Read.All"] = Guid.Parse("19da66cb-0571-4ce4-b2e7-3bf4e1046f31"),
        ["Sites.Read.All"] = Guid.Parse("332a536c-c7ef-4017-ab91-336970924f0d"),
        ["Team.ReadBasic.All"] = Guid.Parse("2280dda6-0bfd-44ee-a2f4-cb867cfc4c1e"),
    };

    public AzureAdSetupService(
        IGraphClientFactory graphClientFactory,
        ILogger<AzureAdSetupService> logger)
    {
        _graphClientFactory = graphClientFactory;
        _logger = logger;
    }

    public async Task<AzureAdSetupResult> SetupAppRegistrationAsync(
        string tenantId,
        string domain,
        Action<AzureAdSetupProgress>? progressCallback = null,
        CancellationToken cancellationToken = default)
    {
        if (!Guid.TryParse(tenantId, out _))
        {
            return new AzureAdSetupResult
            {
                Success = false,
                ErrorCode = "INVALID_TENANT_ID",
                ErrorMessage = "The Azure Tenant ID is not a valid GUID."
            };
        }

        string? createdAppObjectId = null;
        string? createdSpId = null;
        GraphServiceClient? graphClient = null;

        try
        {
            // ── Step 1: Authenticate with delegated admin credentials ──
            ReportProgress(progressCallback, "Authenticating with Microsoft...", 1);

            var wrapper = await _graphClientFactory.CreateDelegatedSetupClientAsync(tenantId, cancellationToken);
            graphClient = ((GraphClientWrapper)wrapper).Client;

            // Test connection and get organization info
            var orgResponse = await graphClient.Organization.GetAsync(cancellationToken: cancellationToken);
            var org = orgResponse?.Value?.FirstOrDefault();
            var orgName = org?.DisplayName ?? "Unknown Organization";
            var orgId = org?.Id;

            _logger.LogInformation("Authenticated to tenant {TenantId} ({OrgName}) for app setup", tenantId, orgName);

            // ── Step 2: Create App Registration ──
            var appDisplayName = $"Cloudativ Assessment Tool - {domain}";
            ReportProgress(progressCallback, $"Creating App Registration \"{appDisplayName}\"...", 2);

            var application = new GraphApplication
            {
                DisplayName = appDisplayName,
                SignInAudience = "AzureADMyOrg",
                RequiredResourceAccess = new List<RequiredResourceAccess>
                {
                    new RequiredResourceAccess
                    {
                        ResourceAppId = MicrosoftGraphAppId,
                        ResourceAccess = RequiredAppPermissions.Select(p => new ResourceAccess
                        {
                            Id = p.Value,
                            Type = "Role" // Application permission
                        }).ToList()
                    }
                }
            };

            var createdApp = await graphClient.Applications.PostAsync(application, cancellationToken: cancellationToken);
            if (createdApp?.AppId == null || createdApp.Id == null)
            {
                return new AzureAdSetupResult
                {
                    Success = false,
                    ErrorCode = "APP_CREATION_FAILED",
                    ErrorMessage = "Failed to create the App Registration. The response was empty.",
                    OrganizationName = orgName,
                    OrganizationId = orgId
                };
            }

            createdAppObjectId = createdApp.Id;
            _logger.LogInformation("Created App Registration: AppId={AppId}, ObjectId={ObjectId}", createdApp.AppId, createdApp.Id);

            // ── Step 3: Create Client Secret ──
            ReportProgress(progressCallback, "Creating client secret...", 3);

            var passwordCredential = new PasswordCredential
            {
                DisplayName = "Cloudativ Assessment - Auto Generated",
                EndDateTime = DateTimeOffset.UtcNow.AddYears(2)
            };

            var secretResult = await graphClient.Applications[createdApp.Id]
                .AddPassword.PostAsync(new AddPasswordPostRequestBody
                {
                    PasswordCredential = passwordCredential
                }, cancellationToken: cancellationToken);

            if (secretResult?.SecretText == null || secretResult.KeyId == null)
            {
                throw new InvalidOperationException("Failed to create client secret. The response was empty.");
            }

            var secretValue = secretResult.SecretText;
            var secretId = secretResult.KeyId.Value.ToString();
            var secretExpiry = secretResult.EndDateTime?.DateTime;

            _logger.LogInformation("Created client secret for app {AppId}, expires {Expiry}", createdApp.AppId, secretExpiry);

            // ── Step 4: Create Service Principal ──
            ReportProgress(progressCallback, "Creating Service Principal...", 4);

            // Small delay to allow Azure AD replication
            await Task.Delay(2000, cancellationToken);

            var servicePrincipal = new ServicePrincipal
            {
                AppId = createdApp.AppId
            };

            var createdSp = await graphClient.ServicePrincipals.PostAsync(servicePrincipal, cancellationToken: cancellationToken);
            if (createdSp?.Id == null)
            {
                throw new InvalidOperationException("Failed to create Service Principal. The response was empty.");
            }

            createdSpId = createdSp.Id;
            _logger.LogInformation("Created Service Principal: {SpId}", createdSp.Id);

            // ── Step 5: Find Microsoft Graph Service Principal ──
            ReportProgress(progressCallback, "Looking up Microsoft Graph Service Principal...", 5);

            var graphSpResponse = await graphClient.ServicePrincipals.GetAsync(config =>
            {
                config.QueryParameters.Filter = $"appId eq '{MicrosoftGraphAppId}'";
            }, cancellationToken: cancellationToken);

            var graphSp = graphSpResponse?.Value?.FirstOrDefault();
            if (graphSp?.Id == null)
            {
                throw new InvalidOperationException("Could not find Microsoft Graph Service Principal in the tenant.");
            }

            _logger.LogInformation("Found Microsoft Graph SP: {GraphSpId}", graphSp.Id);

            // ── Step 6: Grant Admin Consent (App Role Assignments) ──
            ReportProgress(progressCallback, "Granting admin consent for all permissions...", 6);

            // Wait for SP to replicate
            await Task.Delay(3000, cancellationToken);

            var grantedPermissions = new List<string>();
            var failedPermissions = new List<string>();

            foreach (var (permName, permId) in RequiredAppPermissions)
            {
                try
                {
                    var assignment = new AppRoleAssignment
                    {
                        PrincipalId = Guid.Parse(createdSp.Id),
                        ResourceId = Guid.Parse(graphSp.Id),
                        AppRoleId = permId
                    };

                    await graphClient.ServicePrincipals[createdSp.Id]
                        .AppRoleAssignments.PostAsync(assignment, cancellationToken: cancellationToken);

                    grantedPermissions.Add(permName);
                    _logger.LogDebug("Granted permission: {Permission}", permName);
                }
                catch (Exception ex)
                {
                    _logger.LogWarning(ex, "Failed to grant permission {Permission} - it may not exist as an application permission", permName);
                    failedPermissions.Add(permName);
                }
            }

            _logger.LogInformation(
                "Admin consent complete: {Granted} granted, {Failed} failed out of {Total}",
                grantedPermissions.Count, failedPermissions.Count, RequiredAppPermissions.Count);

            // ── Done ──
            ReportProgress(progressCallback, "Setup complete!", 6, isComplete: true);

            return new AzureAdSetupResult
            {
                Success = true,
                ClientId = createdApp.AppId,
                ObjectId = createdApp.Id,
                SecretId = secretId,
                SecretValue = secretValue,
                SecretExpiry = secretExpiry,
                AppDisplayName = appDisplayName,
                ServicePrincipalId = createdSp.Id,
                TotalPermissionsRequested = RequiredAppPermissions.Count,
                PermissionsGranted = grantedPermissions.Count,
                GrantedPermissions = grantedPermissions,
                FailedPermissions = failedPermissions,
                OrganizationName = orgName,
                OrganizationId = orgId
            };
        }
        catch (AuthenticationFailedException authEx)
        {
            _logger.LogError(authEx, "Authentication failed during Azure AD setup");

            return new AzureAdSetupResult
            {
                Success = false,
                ErrorCode = "AUTH_FAILED",
                ErrorMessage = $"Authentication failed. Ensure you sign in with a Global Administrator account.\n\n{authEx.Message}"
            };
        }
        catch (ServiceException graphEx)
        {
            _logger.LogError(graphEx, "Graph API error during Azure AD setup");

            // Attempt cleanup if app was created
            await CleanupOnFailure(graphClient, createdAppObjectId, cancellationToken);

            return new AzureAdSetupResult
            {
                Success = false,
                ErrorCode = graphEx.ResponseStatusCode.ToString(),
                ErrorMessage = $"Microsoft Graph API error: {graphEx.Message}"
            };
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Unexpected error during Azure AD setup");

            // Attempt cleanup if app was created
            await CleanupOnFailure(graphClient, createdAppObjectId, cancellationToken);

            return new AzureAdSetupResult
            {
                Success = false,
                ErrorCode = "UNEXPECTED_ERROR",
                ErrorMessage = $"An unexpected error occurred: {ex.Message}"
            };
        }
    }

    private async Task CleanupOnFailure(GraphServiceClient? graphClient, string? appObjectId, CancellationToken ct)
    {
        if (graphClient == null || string.IsNullOrEmpty(appObjectId))
            return;

        try
        {
            _logger.LogInformation("Cleaning up partially created app registration {ObjectId}...", appObjectId);
            await graphClient.Applications[appObjectId].DeleteAsync(cancellationToken: ct);
            _logger.LogInformation("Successfully cleaned up app registration {ObjectId}", appObjectId);
        }
        catch (Exception cleanupEx)
        {
            _logger.LogWarning(cleanupEx, "Failed to clean up app registration {ObjectId}. It may need manual deletion.", appObjectId);
        }
    }

    private static void ReportProgress(
        Action<AzureAdSetupProgress>? callback,
        string currentStep,
        int stepNumber,
        bool isComplete = false)
    {
        callback?.Invoke(new AzureAdSetupProgress
        {
            CurrentStep = currentStep,
            StepNumber = stepNumber,
            TotalSteps = 6,
            IsComplete = isComplete
        });
    }
}
