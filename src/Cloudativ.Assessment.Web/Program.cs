using Cloudativ.Assessment.Application;
using Cloudativ.Assessment.Infrastructure;
using Cloudativ.Assessment.Infrastructure.Data;
using Cloudativ.Assessment.Web.Services;
using Hangfire;
using Hangfire.Dashboard;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.EntityFrameworkCore;
using MudBlazor.Services;
using Serilog;

var builder = WebApplication.CreateBuilder(args);

// Configure Serilog
builder.Services.AddSerilogLogging();
builder.Host.UseSerilog();

// Add services to the container
builder.Services.AddRazorPages();
builder.Services.AddServerSideBlazor();
builder.Services.AddControllers();
builder.Services.AddMudServices(config =>
{
    config.PopoverOptions.ThrowOnDuplicateProvider = false;
});

// Add Application and Infrastructure services
builder.Services.AddApplicationServices();
builder.Services.AddInfrastructureServices(builder.Configuration);

// Add authentication
builder.Services.AddAuthentication(CookieAuthenticationDefaults.AuthenticationScheme)
    .AddCookie(options =>
    {
        options.LoginPath = "/Account/Login";
        options.LogoutPath = "/Account/Logout";
        options.AccessDeniedPath = "/access-denied";
        options.ExpireTimeSpan = TimeSpan.FromHours(8);
        options.SlidingExpiration = true;
        options.Cookie.Name = ".Cloudativ.Auth";
        options.Cookie.HttpOnly = true;
        options.Cookie.SameSite = SameSiteMode.Lax;
        options.Cookie.SecurePolicy = CookieSecurePolicy.SameAsRequest;
    });

builder.Services.AddAuthorization(options =>
{
    options.AddPolicy("SuperAdmin", policy => policy.RequireRole("SuperAdmin"));
    options.AddPolicy("TenantAdmin", policy => policy.RequireRole("SuperAdmin", "TenantAdmin"));
    options.AddPolicy("Auditor", policy => policy.RequireRole("SuperAdmin", "TenantAdmin", "Auditor"));
});

// Add HttpContextAccessor for auth service
builder.Services.AddHttpContextAccessor();

// Add custom services
builder.Services.AddScoped<AuthenticationStateService>();
builder.Services.AddScoped<AssessmentHubService>();

// Add health checks
builder.Services.AddHealthChecks()
    .AddDbContextCheck<ApplicationDbContext>();

var app = builder.Build();

// Configure the HTTP request pipeline
if (!app.Environment.IsDevelopment())
{
    app.UseExceptionHandler("/Error");
    app.UseHsts();
}

app.UseHttpsRedirection();
app.UseStaticFiles();
app.UseRouting();

app.UseAuthentication();
app.UseAuthorization();

// Hangfire Dashboard (only for SuperAdmin)
app.UseHangfireDashboard("/hangfire", new DashboardOptions
{
    Authorization = new[] { new HangfireAuthorizationFilter() }
});

app.MapControllers();
app.MapBlazorHub();
app.MapFallbackToPage("/_Host");
app.MapHealthChecks("/health");

// Apply migrations and seed data
using (var scope = app.Services.CreateScope())
{
    var dbContext = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();

    try
    {
        // Check if migrations exist, otherwise use EnsureCreated
        var pendingMigrations = await dbContext.Database.GetPendingMigrationsAsync();
        var appliedMigrations = await dbContext.Database.GetAppliedMigrationsAsync();

        if (pendingMigrations.Any() || appliedMigrations.Any())
        {
            await dbContext.Database.MigrateAsync();
            Log.Information("Database migrations applied successfully");
        }
        else
        {
            // No migrations exist, use EnsureCreated for development
            await dbContext.Database.EnsureCreatedAsync();
            Log.Information("Database created using EnsureCreated (no migrations found)");
        }
    }
    catch (Exception ex)
    {
        Log.Warning(ex, "Could not apply migrations, ensuring database is created");
        await dbContext.Database.EnsureCreatedAsync();
    }

    // Seed admin user if not exists
    await SeedDataAsync(scope.ServiceProvider);
}

Log.Information("Cloudativ Assessment started on {Urls}", string.Join(", ", app.Urls));

app.Run();

static async Task SeedDataAsync(IServiceProvider services)
{
    var dbContext = services.GetRequiredService<ApplicationDbContext>();
    var encryptionService = services.GetRequiredService<Cloudativ.Assessment.Application.Interfaces.IEncryptionService>();

    // Check if admin user exists
    if (!await dbContext.AppUsers.AnyAsync())
    {
        var adminUser = new Cloudativ.Assessment.Domain.Entities.AppUser
        {
            Email = "admin@cloudativ.local",
            DisplayName = "System Administrator",
            PasswordHash = encryptionService.HashPassword("Admin@123!"),
            Role = Cloudativ.Assessment.Domain.Enums.AppRole.SuperAdmin,
            IsActive = true
        };

        dbContext.AppUsers.Add(adminUser);
        await dbContext.SaveChangesAsync();
        Log.Information("Seeded default admin user: admin@cloudativ.local");
    }
}

// Hangfire Authorization Filter
public class HangfireAuthorizationFilter : IDashboardAuthorizationFilter
{
    public bool Authorize(DashboardContext context)
    {
        var httpContext = context.GetHttpContext();
        return httpContext.User.Identity?.IsAuthenticated == true &&
               httpContext.User.IsInRole("SuperAdmin");
    }
}
