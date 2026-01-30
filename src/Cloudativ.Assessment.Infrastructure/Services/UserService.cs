using Cloudativ.Assessment.Application.DTOs;
using Cloudativ.Assessment.Application.Interfaces;
using Cloudativ.Assessment.Domain.Entities;
using Cloudativ.Assessment.Domain.Enums;
using Cloudativ.Assessment.Domain.Interfaces;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace Cloudativ.Assessment.Infrastructure.Services;

public class UserService : IUserService
{
    private readonly IUnitOfWork _unitOfWork;
    private readonly IEncryptionService _encryptionService;
    private readonly ILogger<UserService> _logger;

    public UserService(
        IUnitOfWork unitOfWork,
        IEncryptionService encryptionService,
        ILogger<UserService> logger)
    {
        _unitOfWork = unitOfWork;
        _encryptionService = encryptionService;
        _logger = logger;
    }

    public async Task<IReadOnlyList<AppUserDto>> GetAllUsersAsync(CancellationToken cancellationToken = default)
    {
        var users = await _unitOfWork.AppUsers
            .Query()
            .Include(u => u.TenantAccess)
                .ThenInclude(ta => ta.Tenant)
            .Include(u => u.DomainAccess)
            .OrderBy(u => u.DisplayName)
            .ToListAsync(cancellationToken);

        return users.Select(MapToDto).ToList();
    }

    public async Task<AppUserDto?> GetUserByIdAsync(Guid id, CancellationToken cancellationToken = default)
    {
        var user = await _unitOfWork.AppUsers
            .Query()
            .Include(u => u.TenantAccess)
                .ThenInclude(ta => ta.Tenant)
            .Include(u => u.DomainAccess)
            .FirstOrDefaultAsync(u => u.Id == id, cancellationToken);

        return user == null ? null : MapToDto(user);
    }

    public async Task<AppUserDto> CreateUserAsync(CreateUserDto dto, CancellationToken cancellationToken = default)
    {
        // Check if email already exists
        var existingUser = await _unitOfWork.AppUsers.GetByEmailAsync(dto.Email.ToLowerInvariant(), cancellationToken);
        if (existingUser != null)
        {
            throw new InvalidOperationException($"A user with email '{dto.Email}' already exists.");
        }

        var user = new AppUser
        {
            Email = dto.Email.ToLowerInvariant().Trim(),
            DisplayName = dto.DisplayName.Trim(),
            PasswordHash = _encryptionService.HashPassword(dto.Password),
            Role = dto.Role,
            IsActive = true,
            IsExternalAuth = false
        };

        await _unitOfWork.AppUsers.AddAsync(user, cancellationToken);

        // Add tenant access if specified
        if (dto.TenantIds.Any())
        {
            foreach (var tenantId in dto.TenantIds)
            {
                var tenant = await _unitOfWork.Tenants.GetByIdAsync(tenantId, cancellationToken);
                if (tenant != null)
                {
                    var tenantAccess = new TenantUserAccess
                    {
                        AppUserId = user.Id,
                        TenantId = tenantId,
                        TenantRole = dto.Role,
                        IsDefault = dto.TenantIds.First() == tenantId
                    };
                    await _unitOfWork.TenantUserAccess.AddAsync(tenantAccess, cancellationToken);
                }
            }
        }

        // Add domain access for Domain-level admins
        if (dto.Role == AppRole.DomainAdmin && dto.AllowedDomains.Any())
        {
            foreach (var domain in dto.AllowedDomains)
            {
                var domainAccess = new UserDomainAccess
                {
                    AppUserId = user.Id,
                    Domain = domain
                };
                await _unitOfWork.UserDomainAccess.AddAsync(domainAccess, cancellationToken);
            }
        }

        await _unitOfWork.SaveChangesAsync(cancellationToken);

        _logger.LogInformation("Created new user: {Email} ({UserId}) with role {Role}",
            user.Email, user.Id, user.Role);

        return await GetUserByIdAsync(user.Id, cancellationToken) ?? throw new InvalidOperationException("Failed to retrieve created user");
    }

    public async Task<AppUserDto> UpdateUserAsync(UpdateUserDto dto, Guid currentUserId, CancellationToken cancellationToken = default)
    {
        var user = await _unitOfWork.AppUsers
            .Query()
            .Include(u => u.TenantAccess)
            .Include(u => u.DomainAccess)
            .FirstOrDefaultAsync(u => u.Id == dto.Id, cancellationToken);

        if (user == null)
        {
            throw new InvalidOperationException($"User with ID '{dto.Id}' not found.");
        }

        // Prevent self-modification of role
        if (dto.Id == currentUserId && user.Role != dto.Role)
        {
            throw new InvalidOperationException("You cannot change your own role.");
        }

        // Prevent self-deactivation
        if (dto.Id == currentUserId && !dto.IsActive)
        {
            throw new InvalidOperationException("You cannot deactivate your own account.");
        }

        user.DisplayName = dto.DisplayName.Trim();
        user.Role = dto.Role;
        user.IsActive = dto.IsActive;

        await _unitOfWork.AppUsers.UpdateAsync(user, cancellationToken);

        // Update tenant access
        // Remove existing access
        var existingAccess = user.TenantAccess.ToList();
        foreach (var access in existingAccess)
        {
            await _unitOfWork.TenantUserAccess.DeleteAsync(access, cancellationToken);
        }

        // Add new access
        if (dto.TenantIds.Any())
        {
            foreach (var tenantId in dto.TenantIds)
            {
                var tenant = await _unitOfWork.Tenants.GetByIdAsync(tenantId, cancellationToken);
                if (tenant != null)
                {
                    var tenantAccess = new TenantUserAccess
                    {
                        AppUserId = user.Id,
                        TenantId = tenantId,
                        TenantRole = dto.Role,
                        IsDefault = dto.TenantIds.First() == tenantId
                    };
                    await _unitOfWork.TenantUserAccess.AddAsync(tenantAccess, cancellationToken);
                }
            }
        }

        // Update domain access for Domain-level admins
        // Remove existing domain access
        var existingDomainAccess = user.DomainAccess.ToList();
        foreach (var access in existingDomainAccess)
        {
            await _unitOfWork.UserDomainAccess.DeleteAsync(access, cancellationToken);
        }

        // Add new domain access if the role is DomainAdmin
        if (dto.Role == AppRole.DomainAdmin && dto.AllowedDomains.Any())
        {
            foreach (var domain in dto.AllowedDomains)
            {
                var domainAccess = new UserDomainAccess
                {
                    AppUserId = user.Id,
                    Domain = domain
                };
                await _unitOfWork.UserDomainAccess.AddAsync(domainAccess, cancellationToken);
            }
        }

        await _unitOfWork.SaveChangesAsync(cancellationToken);

        _logger.LogInformation("Updated user: {Email} ({UserId})", user.Email, user.Id);

        return await GetUserByIdAsync(user.Id, cancellationToken) ?? throw new InvalidOperationException("Failed to retrieve updated user");
    }

    public async Task<bool> ResetPasswordAsync(ResetPasswordDto dto, CancellationToken cancellationToken = default)
    {
        var user = await _unitOfWork.AppUsers.GetByIdAsync(dto.UserId, cancellationToken);
        if (user == null)
        {
            throw new InvalidOperationException($"User with ID '{dto.UserId}' not found.");
        }

        if (user.IsExternalAuth)
        {
            throw new InvalidOperationException("Cannot reset password for users with external authentication.");
        }

        user.PasswordHash = _encryptionService.HashPassword(dto.NewPassword);
        await _unitOfWork.AppUsers.UpdateAsync(user, cancellationToken);
        await _unitOfWork.SaveChangesAsync(cancellationToken);

        _logger.LogInformation("Password reset for user: {Email} ({UserId})", user.Email, user.Id);

        return true;
    }

    public async Task<bool> DeleteUserAsync(Guid id, Guid currentUserId, CancellationToken cancellationToken = default)
    {
        if (id == currentUserId)
        {
            throw new InvalidOperationException("You cannot delete your own account.");
        }

        var user = await _unitOfWork.AppUsers
            .Query()
            .Include(u => u.TenantAccess)
            .Include(u => u.DomainAccess)
            .FirstOrDefaultAsync(u => u.Id == id, cancellationToken);

        if (user == null)
        {
            throw new InvalidOperationException($"User with ID '{id}' not found.");
        }

        // Delete tenant access first
        foreach (var access in user.TenantAccess.ToList())
        {
            await _unitOfWork.TenantUserAccess.DeleteAsync(access, cancellationToken);
        }

        // Delete domain access
        foreach (var access in user.DomainAccess.ToList())
        {
            await _unitOfWork.UserDomainAccess.DeleteAsync(access, cancellationToken);
        }

        await _unitOfWork.AppUsers.DeleteAsync(user, cancellationToken);
        await _unitOfWork.SaveChangesAsync(cancellationToken);

        _logger.LogInformation("Deleted user: {Email} ({UserId})", user.Email, user.Id);

        return true;
    }

    public async Task<bool> ToggleUserStatusAsync(Guid id, Guid currentUserId, CancellationToken cancellationToken = default)
    {
        if (id == currentUserId)
        {
            throw new InvalidOperationException("You cannot change your own account status.");
        }

        var user = await _unitOfWork.AppUsers.GetByIdAsync(id, cancellationToken);
        if (user == null)
        {
            throw new InvalidOperationException($"User with ID '{id}' not found.");
        }

        user.IsActive = !user.IsActive;
        await _unitOfWork.AppUsers.UpdateAsync(user, cancellationToken);
        await _unitOfWork.SaveChangesAsync(cancellationToken);

        _logger.LogInformation("Toggled status for user: {Email} ({UserId}) - Now {Status}",
            user.Email, user.Id, user.IsActive ? "Active" : "Inactive");

        return true;
    }

    private static AppUserDto MapToDto(AppUser user)
    {
        return new AppUserDto
        {
            Id = user.Id,
            Email = user.Email,
            DisplayName = user.DisplayName,
            Role = user.Role,
            IsActive = user.IsActive,
            IsExternalAuth = user.IsExternalAuth,
            ExternalAuthProvider = user.ExternalAuthProvider,
            LastLoginAt = user.LastLoginAt,
            CreatedAt = user.CreatedAt,
            TenantAccess = user.TenantAccess.Select(ta => new TenantAccessDto
            {
                TenantId = ta.TenantId,
                TenantName = ta.Tenant?.Name ?? "Unknown"
            }).ToList(),
            AllowedDomains = user.DomainAccess.Select(da => da.Domain).ToList()
        };
    }
}
