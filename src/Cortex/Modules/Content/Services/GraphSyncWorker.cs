using System;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.EntityFrameworkCore;
using Cortex.Database;
using Cortex.Infrastructure.Graph;

namespace Cortex.Modules.Content.Services;

public class GraphSyncWorker : BackgroundService
{
    private readonly IServiceProvider _serviceProvider;
    private readonly ILogger<GraphSyncWorker> _logger;

    public GraphSyncWorker(IServiceProvider serviceProvider, ILogger<GraphSyncWorker> logger)
    {
        _serviceProvider = serviceProvider;
        _logger = logger;
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        _logger.LogInformation("GraphSyncWorker running.");

        // Small delay to allow DB migration to finish
        await Task.Delay(TimeSpan.FromSeconds(10), stoppingToken);

        while (!stoppingToken.IsCancellationRequested)
        {
            try
            {
                await SyncGraphDataAsync(stoppingToken);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error occurred executing GraphSyncWorker.");
            }

            // Sync every hour
            await Task.Delay(TimeSpan.FromHours(1), stoppingToken);
        }
    }

    private async Task SyncGraphDataAsync(CancellationToken stoppingToken)
    {
        using var scope = _serviceProvider.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<CortexDbContext>();
        var graph = scope.ServiceProvider.GetRequiredService<IGraphService>();

        _logger.LogInformation("Starting Knowledge Graph Synchronization...");

        // 1. Sync Users
        var users = await db.Users.ToListAsync(stoppingToken);
        foreach (var user in users)
        {
            await graph.UpsertUserAsync(user.Id.ToString(), user.Email ?? "", stoppingToken);
        }

        // 2. Sync Topics and Locations from Insights
        var insights = await db.ContentInsights
            .Include(i => i.Locations)
                .ThenInclude(l => l.Location)
            .ToListAsync(stoppingToken);

        foreach (var insight in insights)
        {
            foreach (var loc in insight.Locations)
            {
                if (loc.Location != null && !string.IsNullOrWhiteSpace(loc.Location.Name))
                {
                    await graph.UpsertLocationAsync(loc.Location.Name, stoppingToken);
                }
            }
            
            // Note: Topics changed to List<string> recently
            if (insight.Topics != null)
            {
                foreach (var topic in insight.Topics)
                {
                    if (!string.IsNullOrWhiteSpace(topic))
                    {
                        await graph.UpsertTopicAsync(topic, stoppingToken);
                    }
                }
            }
        }

        // 3. Sync ContentItems and Relationships
        var contentItems = await db.ContentItems
            .Include(c => c.Insight)
            .Where(c => !c.IsDeleted)
            .ToListAsync(stoppingToken);

        foreach (var item in contentItems)
        {
            await graph.UpsertContentItemAsync(item.Id.ToString(), item.Title ?? "", item.UserId.ToString(), stoppingToken);

            if (item.Insight != null)
            {
                // Note: Topics is now List<string>
                if (item.Insight.Topics != null)
                {
                    foreach (var topic in item.Insight.Topics)
                    {
                        if (!string.IsNullOrWhiteSpace(topic))
                        {
                            await graph.LinkContentToTopicAsync(item.Id.ToString(), topic, stoppingToken);
                        }
                    }
                }

                // Locations
                var insightLocs = await db.Set<Cortex.Modules.Content.Entities.ContentInsightLocation>()
                    .Include(il => il.Location)
                    .Where(il => il.ContentInsightId == item.Insight.Id)
                    .ToListAsync(stoppingToken);
                
                foreach (var loc in insightLocs)
                {
                    if (loc.Location != null && !string.IsNullOrWhiteSpace(loc.Location.Name))
                    {
                        await graph.LinkContentToLocationAsync(item.Id.ToString(), loc.Location.Name, stoppingToken);
                    }
                }
            }
        }

        // 4. Sync User Interests
        var userInterests = await db.UserInterests.Where(i => !i.IsDeleted).ToListAsync(stoppingToken);
        foreach (var interest in userInterests)
        {
            if (!string.IsNullOrWhiteSpace(interest.Category))
            {
                await graph.UpsertTopicAsync(interest.Category, stoppingToken);
                await graph.LinkUserToTopicAsync(interest.UserId.ToString(), interest.Category, "INTERESTED_IN", interest.Score, stoppingToken);
            }
        }

        _logger.LogInformation("Knowledge Graph Synchronization completed successfully.");
    }
}
