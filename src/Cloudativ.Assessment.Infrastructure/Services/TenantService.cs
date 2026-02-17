using Cloudativ.Assessment.Application.DTOs;
using Cloudativ.Assessment.Application.Interfaces;
using Cloudativ.Assessment.Domain.Entities;
using Cloudativ.Assessment.Domain.Enums;
using Cloudativ.Assessment.Domain.Interfaces;
using Cloudativ.Assessment.Infrastructure.Graph;
using Microsoft.Extensions.Logging;
using Microsoft.Graph.Models.ODataErrors;

namespace Cloudativ.Assessment.Infrastructure.Services;

public class TenantService : ITenantService
{
    private readonly IUnitOfWork _unitOfWork;
    private readonly IGraphClientFactory _graphClientFactory;
    private readonly IEncryptionService _encryptionService;
    private readonly ISubscriptionService _subscriptionService;
    private readonly ILogger<TenantService> _logger;

    // Required Graph permissions for the assessment app
    private static readonly List<string> RequiredPermissions = new()
    {
        "User.Read.All",
        "Directory.Read.All",
        "RoleManagement.Read.Directory",
        "Policy.Read.All",
        "AuditLog.Read.All",
        "SecurityEvents.Read.All",
        "IdentityRiskyUser.Read.All",
        "DeviceManagementConfiguration.Read.All",
        "DeviceManagementManagedDevices.Read.All",
        "Mail.Read",
        "MailboxSettings.Read",
        "Organization.Read.All",
        "Application.Read.All",
        "DelegatedPermissionGrant.Read.All",
        "InformationProtectionPolicy.Read.All",
        "Sites.Read.All",
        "Team.ReadBasic.All"
    };

    public TenantService(
        IUnitOfWork unitOfWork,
        IGraphClientFactory graphClientFactory,
        IEncryptionService encryptionService,
        ISubscriptionService subscriptionService,
        ILogger<TenantService> logger)
    {
        _unitOfWork = unitOfWork;
        _graphClientFactory = graphClientFactory;
        _encryptionService = encryptionService;
        _subscriptionService = subscriptionService;
        _logger = logger;
    }

    public async Task<TenantDto?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default)
    {
        var tenant = await _unitOfWork.Tenants.GetByIdAsync(id, cancellationToken);
        if (tenant == null)
            return null;

        var latestRun = await _unitOfWork.AssessmentRuns.GetLatestByTenantAsync(id, cancellationToken);
        var totalRuns = await _unitOfWork.AssessmentRuns.CountAsync(r => r.TenantId == id, cancellationToken);

        return MapToDto(tenant, latestRun, totalRuns);
    }

    public async Task<IReadOnlyList<TenantDto>> GetAllAsync(CancellationToken cancellationToken = default)
    {
        var tenants = await _unitOfWork.Tenants.GetAllAsync(cancellationToken);
        var result = new List<TenantDto>();

        foreach (var tenant in tenants)
        {
            var latestRun = await _unitOfWork.AssessmentRuns.GetLatestByTenantAsync(tenant.Id, cancellationToken);
            var totalRuns = await _unitOfWork.AssessmentRuns.CountAsync(r => r.TenantId == tenant.Id, cancellationToken);
            result.Add(MapToDto(tenant, latestRun, totalRuns));
        }

        return result;
    }

    public async Task<TenantDto> CreateAsync(TenantCreateDto dto, CancellationToken cancellationToken = default)
    {
        var existing = await _unitOfWork.Tenants.GetByDomainAsync(dto.Domain, cancellationToken);
        if (existing != null)
            throw new InvalidOperationException($"A tenant with domain '{dto.Domain}' already exists");

        var tenant = new Tenant
        {
            Name = dto.Name,
            Domain = dto.Domain,
            AzureTenantId = dto.AzureTenantId,
            Industry = dto.Industry,
            ContactEmail = dto.ContactEmail,
            OnboardingStatus = OnboardingStatus.Validated, // Set as validated since connection was tested
            AuthenticationType = dto.AuthenticationType ?? "AppRegistration",
            SelectedComplianceStandards = dto.ComplianceStandards.Any()
                ? string.Join(",", dto.ComplianceStandards)
                : null
        };

        if (dto.AuthenticationType == "AppRegistration" && !string.IsNullOrEmpty(dto.ClientId) && !string.IsNullOrEmpty(dto.SecretValue))
        {
            tenant.ClientId = dto.ClientId;
            tenant.SecretId = dto.SecretId;
            tenant.ClientSecretEncrypted = _encryptionService.Encrypt(dto.SecretValue);
        }

        await _unitOfWork.Tenants.AddAsync(tenant, cancellationToken);

        // Create default settings
        var settings = new TenantSettings
        {
            TenantId = tenant.Id
        };
        await _unitOfWork.TenantSettings.AddAsync(settings, cancellationToken);

        await _unitOfWork.SaveChangesAsync(cancellationToken);

        // Create trial subscription for the new tenant
        try
        {
            await _subscriptionService.CreateTrialAsync(tenant.Id, cancellationToken);
            _logger.LogInformation("Created trial subscription for tenant: {TenantName} ({TenantId})", tenant.Name, tenant.Id);
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Failed to create trial subscription for tenant {TenantId}", tenant.Id);
        }

        _logger.LogInformation("Created new tenant: {TenantName} ({TenantId})", tenant.Name, tenant.Id);

        return MapToDto(tenant, null, 0);
    }

    public async Task<TenantDto> UpdateAsync(Guid id, TenantUpdateDto dto, CancellationToken cancellationToken = default)
    {
        var tenant = await _unitOfWork.Tenants.GetByIdAsync(id, cancellationToken);
        if (tenant == null)
            throw new InvalidOperationException($"Tenant with ID '{id}' not found");

        tenant.Name = dto.Name;
        tenant.Industry = dto.Industry;
        tenant.ContactEmail = dto.ContactEmail;
        tenant.Notes = dto.Notes;

        await _unitOfWork.Tenants.UpdateAsync(tenant, cancellationToken);
        await _unitOfWork.SaveChangesAsync(cancellationToken);

        _logger.LogInformation("Updated tenant: {TenantName} ({TenantId})", tenant.Name, tenant.Id);

        var latestRun = await _unitOfWork.AssessmentRuns.GetLatestByTenantAsync(id, cancellationToken);
        var totalRuns = await _unitOfWork.AssessmentRuns.CountAsync(r => r.TenantId == id, cancellationToken);

        return MapToDto(tenant, latestRun, totalRuns);
    }

    public async Task DeleteAsync(Guid id, CancellationToken cancellationToken = default)
    {
        var tenant = await _unitOfWork.Tenants.GetByIdAsync(id, cancellationToken);
        if (tenant == null)
            throw new InvalidOperationException($"Tenant with ID '{id}' not found");

        await _unitOfWork.Tenants.DeleteAsync(tenant, cancellationToken);
        await _unitOfWork.SaveChangesAsync(cancellationToken);

        _logger.LogInformation("Deleted tenant: {TenantName} ({TenantId})", tenant.Name, tenant.Id);
    }

    public async Task<TenantOnboardingDto> ValidateConnectionAsync(Guid tenantId, string clientId, string clientSecret, CancellationToken cancellationToken = default)
    {
        var tenant = await _unitOfWork.Tenants.GetByIdAsync(tenantId, cancellationToken);
        if (tenant == null)
            throw new InvalidOperationException($"Tenant with ID '{tenantId}' not found");

        if (!tenant.AzureTenantId.HasValue)
        {
            return new TenantOnboardingDto
            {
                TenantId = tenantId,
                ClientId = clientId,
                RequiredPermissions = RequiredPermissions,
                ConnectionValid = false,
                ValidationMessage = "Azure Tenant ID is not configured. Please provide the Azure AD tenant ID."
            };
        }

        try
        {
            var graphClient = await _graphClientFactory.CreateClientAsync(
                clientId,
                clientSecret,
                tenant.AzureTenantId.Value.ToString(),
                cancellationToken);

            var isConnected = await graphClient.TestConnectionAsync(cancellationToken);
            if (!isConnected)
            {
                return new TenantOnboardingDto
                {
                    TenantId = tenantId,
                    ClientId = clientId,
                    RequiredPermissions = RequiredPermissions,
                    ConnectionValid = false,
                    ValidationMessage = "Failed to connect to Microsoft Graph. Please verify the credentials."
                };
            }

            var grantedPermissions = await graphClient.GetGrantedPermissionsAsync(cancellationToken);
            var missingPermissions = RequiredPermissions
                .Where(p => !grantedPermissions.Contains(p, StringComparer.OrdinalIgnoreCase))
                .ToList();

            return new TenantOnboardingDto
            {
                TenantId = tenantId,
                ClientId = clientId,
                RequiredPermissions = RequiredPermissions,
                GrantedPermissions = grantedPermissions,
                MissingPermissions = missingPermissions,
                ConnectionValid = true,
                ValidationMessage = missingPermissions.Any()
                    ? $"Connection valid but {missingPermissions.Count} permissions are missing. Some assessments may be limited."
                    : "All permissions granted. Ready for assessment."
            };
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to validate connection for tenant {TenantId}", tenantId);
            return new TenantOnboardingDto
            {
                TenantId = tenantId,
                ClientId = clientId,
                RequiredPermissions = RequiredPermissions,
                ConnectionValid = false,
                ValidationMessage = $"Connection failed: {ex.Message}"
            };
        }
    }

    public async Task<TenantOnboardingDto> CompleteOnboardingAsync(Guid tenantId, string clientId, string clientSecret, CancellationToken cancellationToken = default)
    {
        var validation = await ValidateConnectionAsync(tenantId, clientId, clientSecret, cancellationToken);

        if (!validation.ConnectionValid)
            return validation;

        var tenant = await _unitOfWork.Tenants.GetByIdAsync(tenantId, cancellationToken);
        if (tenant == null)
            throw new InvalidOperationException($"Tenant with ID '{tenantId}' not found");

        tenant.ClientId = clientId;
        tenant.ClientSecretEncrypted = _encryptionService.Encrypt(clientSecret);
        tenant.OnboardingStatus = validation.MissingPermissions.Any()
            ? OnboardingStatus.PermissionsGranted
            : OnboardingStatus.Validated;

        await _unitOfWork.Tenants.UpdateAsync(tenant, cancellationToken);
        await _unitOfWork.SaveChangesAsync(cancellationToken);

        _logger.LogInformation("Completed onboarding for tenant: {TenantName} ({TenantId})", tenant.Name, tenant.Id);

        return validation;
    }

    public async Task UpdateOnboardingStatusAsync(Guid id, OnboardingStatus status, CancellationToken cancellationToken = default)
    {
        var tenant = await _unitOfWork.Tenants.GetByIdAsync(id, cancellationToken);
        if (tenant == null)
            throw new InvalidOperationException($"Tenant with ID '{id}' not found");

        tenant.OnboardingStatus = status;
        await _unitOfWork.Tenants.UpdateAsync(tenant, cancellationToken);
        await _unitOfWork.SaveChangesAsync(cancellationToken);
    }

    public async Task<ConnectionTestResult> TestConnectionAsync(string azureTenantId, string clientId, string clientSecret, CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(azureTenantId))
        {
            return new ConnectionTestResult
            {
                Success = false,
                ErrorCode = "MISSING_TENANT_ID",
                ErrorMessage = "Azure Tenant ID is required. You can find this in Azure Portal > Azure Active Directory > Overview.",
                RequiredPermissions = RequiredPermissions
            };
        }

        if (string.IsNullOrWhiteSpace(clientId))
        {
            return new ConnectionTestResult
            {
                Success = false,
                ErrorCode = "MISSING_CLIENT_ID",
                ErrorMessage = "Application (Client) ID is required. You can find this in Azure Portal > App registrations > Your app > Overview.",
                RequiredPermissions = RequiredPermissions
            };
        }

        if (string.IsNullOrWhiteSpace(clientSecret))
        {
            return new ConnectionTestResult
            {
                Success = false,
                ErrorCode = "MISSING_CLIENT_SECRET",
                ErrorMessage = "Client Secret is required. Create one in Azure Portal > App registrations > Your app > Certificates & secrets.",
                RequiredPermissions = RequiredPermissions
            };
        }

        // Validate GUID format
        if (!Guid.TryParse(azureTenantId.Trim(), out _))
        {
            return new ConnectionTestResult
            {
                Success = false,
                ErrorCode = "INVALID_TENANT_ID",
                ErrorMessage = "Invalid Azure Tenant ID format. It should be a GUID (e.g., xxxxxxxx-xxxx-xxxx-xxxx-xxxxxxxxxxxx).",
                RequiredPermissions = RequiredPermissions
            };
        }

        if (!Guid.TryParse(clientId.Trim(), out _))
        {
            return new ConnectionTestResult
            {
                Success = false,
                ErrorCode = "INVALID_CLIENT_ID",
                ErrorMessage = "Invalid Application (Client) ID format. It should be a GUID (e.g., xxxxxxxx-xxxx-xxxx-xxxx-xxxxxxxxxxxx).",
                RequiredPermissions = RequiredPermissions
            };
        }

        try
        {
            _logger.LogInformation("Testing connection for Azure tenant: {TenantId}", azureTenantId);

            var graphClient = await _graphClientFactory.CreateClientAsync(
                clientId.Trim(),
                clientSecret,
                azureTenantId.Trim(),
                cancellationToken);

            // Test basic connection by getting organization info
            var isConnected = await graphClient.TestConnectionAsync(cancellationToken);
            if (!isConnected)
            {
                return new ConnectionTestResult
                {
                    Success = false,
                    ErrorCode = "CONNECTION_FAILED",
                    ErrorMessage = "Failed to connect to Microsoft Graph API. Please verify:\n• The Azure Tenant ID matches your Azure AD directory\n• The Application (Client) ID is correct\n• The Client Secret is valid and not expired\n• The app registration exists in this tenant",
                    RequiredPermissions = RequiredPermissions
                };
            }

            // Get organization details for confirmation
            string? orgName = null;
            string? orgId = null;
            try
            {
                var orgJson = await graphClient.GetRawJsonAsync("organization", cancellationToken);
                if (!string.IsNullOrEmpty(orgJson))
                {
                    using var doc = System.Text.Json.JsonDocument.Parse(orgJson);
                    if (doc.RootElement.TryGetProperty("value", out var valueElement) && valueElement.GetArrayLength() > 0)
                    {
                        var firstOrg = valueElement[0];
                        if (firstOrg.TryGetProperty("displayName", out var nameElement))
                            orgName = nameElement.GetString();
                        if (firstOrg.TryGetProperty("id", out var idElement))
                            orgId = idElement.GetString();
                    }
                }
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "Could not retrieve organization details");
            }

            // Get granted permissions
            var grantedPermissions = await graphClient.GetGrantedPermissionsAsync(cancellationToken);
            var missingPermissions = RequiredPermissions
                .Where(p => !grantedPermissions.Contains(p, StringComparer.OrdinalIgnoreCase))
                .ToList();

            return new ConnectionTestResult
            {
                Success = true,
                OrganizationName = orgName,
                OrganizationId = orgId,
                RequiredPermissions = RequiredPermissions,
                GrantedPermissions = grantedPermissions,
                MissingPermissions = missingPermissions,
                ErrorMessage = missingPermissions.Any()
                    ? $"Connection successful but {missingPermissions.Count} permission(s) are missing. Some assessment features may be limited."
                    : null
            };
        }
        catch (Azure.Identity.AuthenticationFailedException ex)
        {
            _logger.LogError(ex, "Authentication failed for tenant {TenantId}", azureTenantId);

            var errorMessage = "Authentication failed. ";
            if (ex.Message.Contains("AADSTS700016"))
            {
                errorMessage += "The Application (Client) ID was not found in this tenant. Verify the app is registered in the correct Azure AD tenant.";
            }
            else if (ex.Message.Contains("AADSTS7000215"))
            {
                errorMessage += "Invalid Client Secret. The secret may be incorrect, expired, or deleted. Create a new secret in Azure Portal.";
            }
            else if (ex.Message.Contains("AADSTS90002"))
            {
                errorMessage += "The Azure Tenant ID was not found. Verify you're using the correct tenant ID from Azure Portal.";
            }
            else if (ex.Message.Contains("AADSTS50034"))
            {
                errorMessage += "The user account does not exist in this tenant.";
            }
            else
            {
                errorMessage += ex.Message;
            }

            return new ConnectionTestResult
            {
                Success = false,
                ErrorCode = "AUTH_FAILED",
                ErrorMessage = errorMessage,
                RequiredPermissions = RequiredPermissions
            };
        }
        catch (ODataError ex)
        {
            _logger.LogError(ex, "Graph API error during connection test for tenant {TenantId}", azureTenantId);

            var errorMessage = ex.ResponseStatusCode switch
            {
                401 => "Authentication failed. The credentials may be invalid or the app registration may not exist in this tenant.",
                403 => "Insufficient privileges. The app registration is missing required permissions. Please grant admin consent for the required permissions in Azure Portal > App registrations > API permissions.",
                _ => $"Microsoft Graph API error ({ex.ResponseStatusCode}): {ex.Message}"
            };

            return new ConnectionTestResult
            {
                Success = false,
                ErrorCode = $"GRAPH_{ex.ResponseStatusCode}",
                ErrorMessage = errorMessage,
                RequiredPermissions = RequiredPermissions
            };
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Connection test failed for tenant {TenantId}", azureTenantId);
            return new ConnectionTestResult
            {
                Success = false,
                ErrorCode = "UNKNOWN_ERROR",
                ErrorMessage = $"Connection test failed: {ex.Message}",
                RequiredPermissions = RequiredPermissions
            };
        }
    }

    public async Task<ConnectionTestResult> TestConnectionWithDelegatedAuthAsync(string azureTenantId, CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(azureTenantId))
        {
            return new ConnectionTestResult
            {
                Success = false,
                ErrorCode = "MISSING_TENANT_ID",
                ErrorMessage = "Azure Tenant ID is required. You can find this in Azure Portal > Azure Active Directory > Overview.",
                RequiredPermissions = RequiredPermissions
            };
        }

        // Validate GUID format
        if (!Guid.TryParse(azureTenantId.Trim(), out _))
        {
            return new ConnectionTestResult
            {
                Success = false,
                ErrorCode = "INVALID_TENANT_ID",
                ErrorMessage = "Invalid Azure Tenant ID format. It should be a GUID (e.g., xxxxxxxx-xxxx-xxxx-xxxx-xxxxxxxxxxxx).",
                RequiredPermissions = RequiredPermissions
            };
        }

        try
        {
            _logger.LogInformation("Testing delegated auth connection for Azure tenant: {TenantId}", azureTenantId);

            var graphClient = await _graphClientFactory.CreateDelegatedClientAsync(
                azureTenantId.Trim(),
                cancellationToken);

            // Test basic connection by getting organization info
            var isConnected = await graphClient.TestConnectionAsync(cancellationToken);
            if (!isConnected)
            {
                return new ConnectionTestResult
                {
                    Success = false,
                    ErrorCode = "CONNECTION_FAILED",
                    ErrorMessage = "Failed to connect to Microsoft Graph API. Please verify:\n• The Azure Tenant ID matches your Azure AD directory\n• You have the required administrator permissions\n• You completed the sign-in process successfully",
                    RequiredPermissions = RequiredPermissions
                };
            }

            // Get organization details for confirmation
            string? orgName = null;
            string? orgId = null;
            try
            {
                var orgJson = await graphClient.GetRawJsonAsync("organization", cancellationToken);
                if (!string.IsNullOrEmpty(orgJson))
                {
                    using var doc = System.Text.Json.JsonDocument.Parse(orgJson);
                    if (doc.RootElement.TryGetProperty("value", out var valueElement) && valueElement.GetArrayLength() > 0)
                    {
                        var firstOrg = valueElement[0];
                        if (firstOrg.TryGetProperty("displayName", out var nameElement))
                            orgName = nameElement.GetString();
                        if (firstOrg.TryGetProperty("id", out var idElement))
                            orgId = idElement.GetString();
                    }
                }
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "Could not retrieve organization details");
            }

            // Get granted permissions
            var grantedPermissions = await graphClient.GetGrantedPermissionsAsync(cancellationToken);
            var missingPermissions = RequiredPermissions
                .Where(p => !grantedPermissions.Contains(p, StringComparer.OrdinalIgnoreCase))
                .ToList();

            return new ConnectionTestResult
            {
                Success = true,
                OrganizationName = orgName,
                OrganizationId = orgId,
                RequiredPermissions = RequiredPermissions,
                GrantedPermissions = grantedPermissions,
                MissingPermissions = missingPermissions,
                ErrorMessage = missingPermissions.Any()
                    ? $"Connection successful but {missingPermissions.Count} permission(s) are missing. Some assessment features may be limited."
                    : null
            };
        }
        catch (Azure.Identity.AuthenticationFailedException ex)
        {
            _logger.LogError(ex, "Delegated authentication failed for tenant {TenantId}", azureTenantId);

            var errorMessage = "Authentication failed. ";
            if (ex.Message.Contains("AADSTS90002"))
            {
                errorMessage += "The Azure Tenant ID was not found. Verify you're using the correct tenant ID from Azure Portal.";
            }
            else if (ex.Message.Contains("AADSTS50034"))
            {
                errorMessage += "The user account does not exist in this tenant.";
            }
            else if (ex.Message.Contains("AADSTS65001"))
            {
                errorMessage += "The user or administrator has not consented to use the application. Please sign in again and grant the required permissions.";
            }
            else
            {
                errorMessage += ex.Message;
            }

            return new ConnectionTestResult
            {
                Success = false,
                ErrorCode = "AUTH_FAILED",
                ErrorMessage = errorMessage,
                RequiredPermissions = RequiredPermissions
            };
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Delegated auth connection test failed for tenant {TenantId}", azureTenantId);
            return new ConnectionTestResult
            {
                Success = false,
                ErrorCode = "UNKNOWN_ERROR",
                ErrorMessage = $"Connection test failed: {ex.Message}",
                RequiredPermissions = RequiredPermissions
            };
        }
    }

    private static TenantDto MapToDto(Tenant tenant, AssessmentRun? latestRun, int totalRuns)
    {
        return new TenantDto
        {
            Id = tenant.Id,
            Name = tenant.Name,
            Domain = tenant.Domain,
            AzureTenantId = tenant.AzureTenantId,
            OnboardingStatus = tenant.OnboardingStatus,
            Industry = tenant.Industry,
            ContactEmail = tenant.ContactEmail,
            CreatedAt = tenant.CreatedAt,
            LastAssessmentScore = latestRun?.OverallScore,
            LastAssessmentDate = latestRun?.CompletedAt ?? latestRun?.StartedAt,
            TotalAssessments = totalRuns,
            SelectedComplianceStandards = string.IsNullOrEmpty(tenant.SelectedComplianceStandards)
                ? new List<string>()
                : tenant.SelectedComplianceStandards.Split(',', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries).ToList()
        };
    }
}
