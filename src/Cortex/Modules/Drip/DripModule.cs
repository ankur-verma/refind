using Cortex.Modules.Drip.Persistence;
using Cortex.Modules.Drip.UseCases;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace Cortex.Modules.Drip;

public static class DripModule
{
    public static IServiceCollection AddDripModule(this IServiceCollection services, IConfiguration configuration)
    {
        // Use cases
        services.AddScoped<CreateDripTrackUseCase>();
        services.AddScoped<AdvanceDripStepUseCase>();
        services.AddScoped<GetDripTracksUseCase>();

        // Repositories
        services.AddScoped<IDripTrackRepository, DripTrackRepository>();
        services.AddScoped<IDripStepRepository, DripStepRepository>();

        return services;
    }
}
