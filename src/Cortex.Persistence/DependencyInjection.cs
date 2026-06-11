using Cortex.Application.Interfaces.Repositories;
using Cortex.Persistence.Repositories;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace Cortex.Persistence;

/// <summary>
/// DI registration for the Persistence layer.
/// </summary>
public static class DependencyInjection
{
    public static IServiceCollection AddPersistence(this IServiceCollection services, IConfiguration configuration)
    {
        services.AddDbContext<CortexDbContext>(options =>
        {
            options.UseNpgsql(
                configuration.GetConnectionString("CortexDatabase"),
                npgsqlOptions =>
                {
                    npgsqlOptions.UseVector();
                    npgsqlOptions.MigrationsAssembly(typeof(CortexDbContext).Assembly.FullName);
                });
        });

        // Register repositories
        services.AddScoped<IUserRepository, UserRepository>();
        services.AddScoped<IContentItemRepository, ContentItemRepository>();
        services.AddScoped<IContentPayloadRepository, ContentPayloadRepository>();
        services.AddScoped<IActionItemRepository, ActionItemRepository>();
        services.AddScoped<IDripTrackRepository, DripTrackRepository>();
        services.AddScoped<IDripStepRepository, DripStepRepository>();
        services.AddScoped<ITagRepository, TagRepository>();
        services.AddScoped<ISubscriptionRepository, SubscriptionRepository>();
        services.AddScoped<IUnitOfWork, UnitOfWork>();

        return services;
    }
}
