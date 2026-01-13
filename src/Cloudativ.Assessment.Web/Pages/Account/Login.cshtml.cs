using System.Security.Claims;
using Cloudativ.Assessment.Application.Interfaces;
using Cloudativ.Assessment.Domain.Interfaces;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;

namespace Cloudativ.Assessment.Web.Pages.Account;

public class LoginModel : PageModel
{
    private readonly IUnitOfWork _unitOfWork;
    private readonly IEncryptionService _encryptionService;
    private readonly ILogger<LoginModel> _logger;

    public LoginModel(IUnitOfWork unitOfWork, IEncryptionService encryptionService, ILogger<LoginModel> logger)
    {
        _unitOfWork = unitOfWork;
        _encryptionService = encryptionService;
        _logger = logger;
    }

    [BindProperty]
    public string Email { get; set; } = string.Empty;

    [BindProperty]
    public string Password { get; set; } = string.Empty;

    public string? ErrorMessage { get; set; }

    public string? ReturnUrl { get; set; }

    public void OnGet(string? returnUrl = null)
    {
        ReturnUrl = returnUrl ?? "/";
    }

    public async Task<IActionResult> OnPostAsync(string? returnUrl = null)
    {
        returnUrl ??= "/";

        var user = await _unitOfWork.AppUsers.GetByEmailAsync(Email);

        if (user == null)
        {
            ErrorMessage = "Invalid email or password";
            return Page();
        }

        if (!user.IsActive)
        {
            ErrorMessage = "Account is disabled";
            return Page();
        }

        if (user.IsExternalAuth)
        {
            ErrorMessage = "Please use external authentication provider";
            return Page();
        }

        if (string.IsNullOrEmpty(user.PasswordHash) || !_encryptionService.VerifyPassword(Password, user.PasswordHash))
        {
            ErrorMessage = "Invalid email or password";
            return Page();
        }

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

        await HttpContext.SignInAsync(
            CookieAuthenticationDefaults.AuthenticationScheme,
            principal,
            new AuthenticationProperties
            {
                IsPersistent = true,
                ExpiresUtc = DateTimeOffset.UtcNow.AddHours(8)
            });

        _logger.LogInformation("User {Email} logged in successfully, redirecting to {ReturnUrl}", Email, returnUrl);

        // Validate returnUrl - only allow local redirects
        if (string.IsNullOrEmpty(returnUrl) || !Url.IsLocalUrl(returnUrl))
        {
            returnUrl = "/";
        }

        return Redirect(returnUrl);
    }
}
