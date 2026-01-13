using Cloudativ.Assessment.Application.DTOs;
using Cloudativ.Assessment.Application.Interfaces;
using Cloudativ.Assessment.Domain.Entities;
using Cloudativ.Assessment.Domain.Enums;
using Cloudativ.Assessment.Domain.Interfaces;
using Cloudativ.Assessment.Infrastructure.Graph;
using Microsoft.Extensions.Logging;

namespace Cloudativ.Assessment.Infrastructure.Services;

public class TenantService : ITenantService
{
    private readonly IUnitOfWork _unitOfWork;
    private readonly IGraphClientFactory _graphClientFactory;
    private readonly IEncryptionService _encryptionService;
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
        ILogger<TenantService> logger)
    {
        _unitOfWork = unitOfWork;
        _graphClientFactory = graphClientFactory;
        _encryptionService = encryptionService;
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
            OnboardingStatus = OnboardingStatus.Pending
        };

        if (!string.IsNullOrEmpty(dto.ClientId) && !string.IsNullOrEmpty(dto.ClientSecret))
        {
            tenant.ClientId = dto.ClientId;
            tenant.ClientSecretEncrypted = _encryptionService.Encrypt(dto.ClientSecret);
        }

        await _unitOfWork.Tenants.AddAsync(tenant, cancellationToken);

        // Create default settings
        var settings = new TenantSettings
        {
            TenantId = tenant.Id
        };
        await _unitOfWork.TenantSettings.AddAsync(settings, cancellationToken);

        await _unitOfWork.SaveChangesAsync(cancellationToken);

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
            TotalAssessments = totalRuns
        };
    }
}
