using System.Security.Claims;
using Cloudativ.Assessment.Application.Interfaces;
using Cloudativ.Assessment.Domain.Entities;
using Cloudativ.Assessment.Domain.Enums;
using Cloudativ.Assessment.Domain.Interfaces;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.Cookies;

namespace Cloudativ.Assessment.Web.Services;

public class AuthenticationStateService
{
    private readonly IHttpContextAccessor _httpContextAccessor;
    private readonly IUnitOfWork _unitOfWork;
    private readonly IEncryptionService _encryptionService;

    public AuthenticationStateService(
        IHttpContextAccessor httpContextAccessor,
        IUnitOfWork unitOfWork,
        IEncryptionService encryptionService)
    {
        _httpContextAccessor = httpContextAccessor;
        _unitOfWork = unitOfWork;
        _encryptionService = encryptionService;
    }

    public async Task<(bool Success, string? ErrorMessage)> LoginAsync(string email, string password)
    {
        var user = await _unitOfWork.AppUsers.GetByEmailAsync(email);

        if (user == null)
            return (false, "Invalid email or password");

        if (!user.IsActive)
            return (false, "Account is disabled");

        if (user.IsExternalAuth)
            return (false, "Please use external authentication provider");

        if (string.IsNullOrEmpty(user.PasswordHash) || !_encryptionService.VerifyPassword(password, user.PasswordHash))
            return (false, "Invalid email or password");

        // Update last login
        user.LastLoginAt = DateTime.UtcNow;
        await _unitOfWork.AppUsers.UpdateAsync(user);
        await _unitOfWork.SaveChangesAsync();

        // Create claims
        var claims = new List<Claim>
        {
            new(ClaimTypes.NameIdentifier, user.Id.ToString()),
            new(ClaimTypes.Email, user.Email),
            new(ClaimTypes.Name, user.DisplayName),
            new(ClaimTypes.Role, user.Role.ToString())
        };

        var identity = new ClaimsIdentity(claims, CookieAuthenticationDefaults.AuthenticationScheme);
        var principal = new ClaimsPrincipal(identity);

        var httpContext = _httpContextAccessor.HttpContext;
        if (httpContext != null)
        {
            await httpContext.SignInAsync(
                CookieAuthenticationDefaults.AuthenticationScheme,
                principal,
                new AuthenticationProperties
                {
                    IsPersistent = true,
                    ExpiresUtc = DateTimeOffset.UtcNow.AddHours(8)
                });
        }

        return (true, null);
    }

    public async Task LogoutAsync()
    {
        var httpContext = _httpContextAccessor.HttpContext;
        if (httpContext != null)
        {
            await httpContext.SignOutAsync(CookieAuthenticationDefaults.AuthenticationScheme);
        }
    }

    public async Task<AppUser?> GetCurrentUserAsync()
    {
        var httpContext = _httpContextAccessor.HttpContext;
        if (httpContext?.User.Identity?.IsAuthenticated != true)
            return null;

        var userIdClaim = httpContext.User.FindFirst(ClaimTypes.NameIdentifier);
        if (userIdClaim == null || !Guid.TryParse(userIdClaim.Value, out var userId))
            return null;

        return await _unitOfWork.AppUsers.GetWithTenantAccessAsync(userId);
    }

    public async Task<AppUser?> GetCurrentUserWithDomainAccessAsync()
    {
        var httpContext = _httpContextAccessor.HttpContext;
        if (httpContext?.User.Identity?.IsAuthenticated != true)
            return null;

        var userIdClaim = httpContext.User.FindFirst(ClaimTypes.NameIdentifier);
        if (userIdClaim == null || !Guid.TryParse(userIdClaim.Value, out var userId))
            return null;

        return await _unitOfWork.AppUsers.GetWithAllAccessAsync(userId);
    }

    /// <summary>
    /// Check if the current user can access a specific assessment domain.
    /// SuperAdmin, TenantAdmin, and Auditor can access all domains.
    /// DomainAdmin can only access their assigned domains.
    /// </summary>
    public async Task<bool> CanAccessDomainAsync(AssessmentDomain domain)
    {
        var role = GetCurrentUserRole();
        if (!role.HasValue)
            return false;

        // SuperAdmin, TenantAdmin, and Auditor can access all domains
        if (role.Value != AppRole.DomainAdmin)
            return true;

        // DomainAdmin - check allowed domains
        var user = await GetCurrentUserWithDomainAccessAsync();
        if (user == null)
            return false;

        return user.DomainAccess.Any(da => da.Domain == domain);
    }

    /// <summary>
    /// Get the list of assessment domains the current user can access.
    /// Returns all domains for SuperAdmin, TenantAdmin, and Auditor.
    /// Returns only assigned domains for DomainAdmin.
    /// </summary>
    public async Task<List<AssessmentDomain>> GetAllowedDomainsAsync()
    {
        var role = GetCurrentUserRole();
        if (!role.HasValue)
            return new List<AssessmentDomain>();

        // SuperAdmin, TenantAdmin, and Auditor can access all domains
        if (role.Value != AppRole.DomainAdmin)
            return Enum.GetValues<AssessmentDomain>().ToList();

        // DomainAdmin - return only assigned domains
        var user = await GetCurrentUserWithDomainAccessAsync();
        if (user == null)
            return new List<AssessmentDomain>();

        return user.DomainAccess.Select(da => da.Domain).ToList();
    }

    public Guid? GetCurrentUserId()
    {
        var httpContext = _httpContextAccessor.HttpContext;
        var userIdClaim = httpContext?.User.FindFirst(ClaimTypes.NameIdentifier);
        if (userIdClaim != null && Guid.TryParse(userIdClaim.Value, out var userId))
            return userId;
        return null;
    }

    public AppRole? GetCurrentUserRole()
    {
        var httpContext = _httpContextAccessor.HttpContext;
        var roleClaim = httpContext?.User.FindFirst(ClaimTypes.Role);
        if (roleClaim != null && Enum.TryParse<AppRole>(roleClaim.Value, out var role))
            return role;
        return null;
    }

    public bool IsInRole(params AppRole[] roles)
    {
        var currentRole = GetCurrentUserRole();
        return currentRole.HasValue && roles.Contains(currentRole.Value);
    }
}
