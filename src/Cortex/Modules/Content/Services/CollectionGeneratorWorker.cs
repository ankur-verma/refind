using System;
using System.Threading;
using System.Threading.Tasks;
using Cortex.Database;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;

namespace Cortex.Modules.Content.Services;

public class CollectionGeneratorWorker : BackgroundService
{
    private readonly IServiceProvider _services;
    private readonly ILogger<CollectionGeneratorWorker> _logger;

    public CollectionGeneratorWorker(IServiceProvider services, ILogger<CollectionGeneratorWorker> logger)
    {
        _services = services;
        _logger = logger;
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        _logger.LogInformation("Collection Generator Worker started.");

        while (!stoppingToken.IsCancellationRequested)
        {
            try
            {
                // Run once a day
                await Task.Delay(TimeSpan.FromHours(24), stoppingToken);
                
                using var scope = _services.CreateScope();
                var dbContext = scope.ServiceProvider.GetRequiredService<CortexDbContext>();
                var generator = scope.ServiceProvider.GetRequiredService<ICollectionGenerator>();

                // Get all active users
                var users = await dbContext.Users.Select(u => u.Id).ToListAsync(stoppingToken);
                
                foreach (var userId in users)
                {
                    await generator.GenerateForUserAsync(userId, stoppingToken);
                }
            }
            catch (TaskCanceledException)
            {
                // Ignore
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error occurred during scheduled collection generation.");
            }
        }
    }
}
