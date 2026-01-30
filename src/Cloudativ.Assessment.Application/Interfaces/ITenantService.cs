using Cloudativ.Assessment.Application.DTOs;
using Cloudativ.Assessment.Domain.Enums;

namespace Cloudativ.Assessment.Application.Interfaces;

public interface ITenantService
{
    Task<TenantDto?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default);
    Task<IReadOnlyList<TenantDto>> GetAllAsync(CancellationToken cancellationToken = default);
    Task<TenantDto> CreateAsync(TenantCreateDto dto, CancellationToken cancellationToken = default);
    Task<TenantDto> UpdateAsync(Guid id, TenantUpdateDto dto, CancellationToken cancellationToken = default);
    Task DeleteAsync(Guid id, CancellationToken cancellationToken = default);
    Task<TenantOnboardingDto> ValidateConnectionAsync(Guid tenantId, string clientId, string clientSecret, CancellationToken cancellationToken = default);
    Task<TenantOnboardingDto> CompleteOnboardingAsync(Guid tenantId, string clientId, string clientSecret, CancellationToken cancellationToken = default);
    Task UpdateOnboardingStatusAsync(Guid id, OnboardingStatus status, CancellationToken cancellationToken = default);
    Task<ConnectionTestResult> TestConnectionAsync(string azureTenantId, string clientId, string clientSecret, CancellationToken cancellationToken = default);
    Task<ConnectionTestResult> TestConnectionWithDelegatedAuthAsync(string azureTenantId, CancellationToken cancellationToken = default);
}
