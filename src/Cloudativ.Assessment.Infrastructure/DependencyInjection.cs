using Cloudativ.Assessment.Application.Interfaces;
using Cloudativ.Assessment.Domain.Interfaces;
using Cloudativ.Assessment.Infrastructure.Data;
using Cloudativ.Assessment.Infrastructure.Graph;
using Cloudativ.Assessment.Infrastructure.Modules;
using Cloudativ.Assessment.Infrastructure.Services;
using Hangfire;
using Hangfire.MemoryStorage;
using Microsoft.AspNetCore.DataProtection;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Serilog;

namespace Cloudativ.Assessment.Infrastructure;

public static class DependencyInjection
{
    public static IServiceCollection AddInfrastructureServices(
        this IServiceCollection services,
        IConfiguration configuration)
    {
        // Configure database
        var databaseProvider = configuration.GetValue<string>("Database:Provider") ?? "SQLite";
        var connectionString = configuration.GetConnectionString("DefaultConnection")
            ?? "Data Source=cloudativ_assessment.db";

        services.AddDbContext<ApplicationDbContext>(options =>
        {
            switch (databaseProvider.ToLowerInvariant())
            {
                case "postgresql":
                case "postgres":
                    options.UseNpgsql(connectionString);
                    break;
                case "sqlserver":
                    options.UseSqlServer(connectionString);
                    break;
                default: // SQLite
                    options.UseSqlite(connectionString);
                    break;
            }
        });

        // Data Protection for encryption
        services.AddDataProtection()
            .SetApplicationName("Cloudativ.Assessment")
            .PersistKeysToDbContext<ApplicationDbContext>();

        // Register Unit of Work and Repositories
        services.AddScoped<IUnitOfWork, UnitOfWork>();

        // Register Memory Cache
        services.AddMemoryCache();

        // Register Services
        services.AddScoped<IEncryptionService, EncryptionService>();
        services.AddScoped<ITenantService, TenantService>();
        services.AddScoped<IDashboardService, DashboardService>();
        services.AddScoped<IAssessmentService, AssessmentService>();
        services.AddScoped<IGraphClientFactory, GraphClientFactory>();
        services.AddScoped<IAssessmentEngine, AssessmentEngine>();
        services.AddScoped<IReportService, ReportService>();
        services.AddScoped<IUserService, UserService>();
        services.AddScoped<IStripeService, StripeService>();
        services.AddScoped<ISubscriptionService, SubscriptionService>();

        // Register Governance Services
        services.AddHttpClient<IOpenAiService, OpenAiService>(client =>
        {
            client.Timeout = TimeSpan.FromMinutes(5); // OpenAI can take a while for large requests
        });
        services.AddHttpClient<IStandardDocumentService, StandardDocumentService>(client =>
        {
            client.Timeout = TimeSpan.FromMinutes(2);
        });
        services.AddScoped<IGovernanceService, GovernanceService>();
        services.AddScoped<IGovernanceExportService, GovernanceExportService>();

        // Register Assessment Modules
        services.AddScoped<IAssessmentModule, IamAssessmentModule>();
        services.AddScoped<IAssessmentModule, DlpAssessmentModule>();
        services.AddScoped<IAssessmentModule, PrivilegedAccessAssessmentModule>();
        services.AddScoped<IAssessmentModule, DeviceEndpointAssessmentModule>();
        services.AddScoped<IAssessmentModule, ExchangeEmailSecurityAssessmentModule>();
        services.AddScoped<IAssessmentModule, MicrosoftDefenderAssessmentModule>();
        services.AddScoped<IAssessmentModule, AuditLoggingAssessmentModule>();
        services.AddScoped<IAssessmentModule, AppGovernanceAssessmentModule>();
        services.AddScoped<IAssessmentModule, CollaborationSecurityAssessmentModule>();

        // Configure Hangfire
        services.AddHangfire(config =>
        {
            config.SetDataCompatibilityLevel(CompatibilityLevel.Version_180)
                .UseSimpleAssemblyNameTypeSerializer()
                .UseRecommendedSerializerSettings()
                .UseMemoryStorage();
        });

        services.AddHangfireServer(options =>
        {
            options.WorkerCount = 2;
            options.Queues = new[] { "assessment", "default" };
        });

        return services;
    }

    public static IServiceCollection AddSerilogLogging(this IServiceCollection services)
    {
        Log.Logger = new LoggerConfiguration()
            .MinimumLevel.Information()
            .MinimumLevel.Override("Microsoft", Serilog.Events.LogEventLevel.Warning)
            .MinimumLevel.Override("Microsoft.EntityFrameworkCore", Serilog.Events.LogEventLevel.Warning)
            .Enrich.FromLogContext()
            .WriteTo.Console(
                outputTemplate: "[{Timestamp:HH:mm:ss} {Level:u3}] {Message:lj}{NewLine}{Exception}")
            .WriteTo.File(
                path: "logs/cloudativ-.log",
                rollingInterval: RollingInterval.Day,
                retainedFileCountLimit: 30,
                outputTemplate: "{Timestamp:yyyy-MM-dd HH:mm:ss.fff zzz} [{Level:u3}] {Message:lj}{NewLine}{Exception}")
            .CreateLogger();

        services.AddLogging(loggingBuilder =>
            loggingBuilder.AddSerilog(dispose: true));

        return services;
    }
}
