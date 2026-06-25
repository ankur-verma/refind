using System;
using System.Threading;
using System.Threading.Tasks;
using Cortex.Database;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;

namespace Cortex.Modules.Content.Services;

public class MemoryLifecycleWorker : BackgroundService
{
    private readonly IServiceProvider _serviceProvider;
    private readonly ILogger<MemoryLifecycleWorker> _logger;

    public MemoryLifecycleWorker(IServiceProvider serviceProvider, ILogger<MemoryLifecycleWorker> logger)
    {
        _serviceProvider = serviceProvider;
        _logger = logger;
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        _logger.LogInformation("MemoryLifecycleWorker running.");

        while (!stoppingToken.IsCancellationRequested)
        {
            try
            {
                using var scope = _serviceProvider.CreateScope();
                var memoryService = scope.ServiceProvider.GetRequiredService<IMemoryService>();
                var dbContext = scope.ServiceProvider.GetRequiredService<CortexDbContext>();

                // In a real app, we'd only evaluate active users or users with recent context updates
                var userIds = await dbContext.Users.Select(u => u.Id).ToListAsync(stoppingToken);

                foreach (var userId in userIds)
                {
                    await memoryService.EvaluateUserMemoryTriggersAsync(userId);
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error occurred executing MemoryLifecycleWorker.");
            }

            await Task.Delay(TimeSpan.FromMinutes(1), stoppingToken); // Run every 1 minute for demo
        }
    }
}
