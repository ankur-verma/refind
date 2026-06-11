using Cortex.AI.Workers;
using Microsoft.Extensions.DependencyInjection;

namespace Cortex.AI;

/// <summary>
/// DI registration for the AI layer.
/// </summary>
public static class DependencyInjection
{
    public static IServiceCollection AddAIWorker(this IServiceCollection services)
    {
        services.AddHostedService<AIExtractionWorker>();
        return services;
    }
}
