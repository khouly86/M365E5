using Cloudativ.Assessment.Application.Services;
using FluentValidation;
using Microsoft.Extensions.DependencyInjection;

namespace Cloudativ.Assessment.Application;

public static class DependencyInjection
{
    public static IServiceCollection AddApplicationServices(this IServiceCollection services)
    {
        // Register validators
        services.AddValidatorsFromAssembly(typeof(DependencyInjection).Assembly);

        // Register services
        services.AddScoped<IScoringService, ScoringService>();

        return services;
    }
}
