using System;
using System.Linq;
using System.Threading.Tasks;
using Cortex.Database;
using Cortex.Modules.Content.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using System.Collections.Generic;

namespace Cortex.Modules.Content.Services;

public interface IMemoryService
{
    Task EvaluateUserMemoryTriggersAsync(Guid userId);
}

public class MemoryService : IMemoryService
{
    private readonly CortexDbContext _dbContext;
    private readonly IContextService _contextService;
    private readonly IReminderService _reminderService;
    private readonly ILogger<MemoryService> _logger;

    public MemoryService(
        CortexDbContext dbContext,
        IContextService contextService,
        IReminderService reminderService,
        ILogger<MemoryService> logger)
    {
        _dbContext = dbContext;
        _contextService = contextService;
        _reminderService = reminderService;
        _logger = logger;
    }

    public async Task EvaluateUserMemoryTriggersAsync(Guid userId)
    {
        var context = await _contextService.GetUserContextAsync(userId);
        if (context == null) return;

        // 1. Evaluate Location-based Memory
        // Example logic: Find saved locations near the user
        await EvaluateLocationMemoryAsync(userId, context);
        
        // 2. Evaluate Time-based Memory (e.g. "You saved this 6 months ago")
        // await EvaluateTimeMemoryAsync(userId);
    }

    private async Task EvaluateLocationMemoryAsync(Guid userId, UserContext context)
    {
        // Simple mock location evaluation using bounding box (for illustration)

        var savedItemsWithLocation = await _dbContext.ContentItems
            .Include(ci => ci.Insight)
                .ThenInclude(i => i!.Locations)
                    .ThenInclude(l => l.Location)
            .Where(ci => ci.UserId == userId && ci.Insight != null && ci.Insight.Locations.Any())
            .ToListAsync();

        foreach (var item in savedItemsWithLocation)
        {
            var locations = item.Insight!.Locations;
            foreach (var loc in locations)
            {
                // Simple mock location evaluation
                bool isNear = false;
                
                // MOCK LOGIC for "set delhi India"
                if (context.Latitude == 28.7041 && context.Longitude == 77.1025) // Coordinates for Delhi
                {
                    if (loc.Location.Name.Contains("Delhi", StringComparison.OrdinalIgnoreCase))
                    {
                        isNear = true;
                    }
                }
                // Mock for Malibu
                else if (context.Latitude == 34.0259 && context.Longitude == -118.7798)
                {
                    if (loc.Location.Name.Contains("Malibu", StringComparison.OrdinalIgnoreCase))
                    {
                        isNear = true;
                    }
                }

                if (isNear)
                {
                    // Check if we already notified them recently about this location
                    var recentMemory = await _dbContext.ProactiveMemories
                        .FirstOrDefaultAsync(m => m.UserId == userId && m.MemoryType == MemoryType.LocationBased && m.Title.Contains(loc.Location.Name));

                    if (recentMemory == null)
                    {
                        var title = $"You're near {loc.Location.Name}!";
                        var msg = $"You saved a video about {loc.Location.Name} recently. Want to check it out?";
                        
                        var newMemory = new ProactiveMemory
                        {
                            UserId = userId,
                            MemoryType = MemoryType.LocationBased,
                            Title = title,
                            Message = msg,
                            ActionUrl = $"/content/{item.Id}",
                            IsRead = false
                        };

                        _dbContext.ProactiveMemories.Add(newMemory);
                        await _dbContext.SaveChangesAsync();

                        await _reminderService.PushMemoryNotificationAsync(userId, title, msg, newMemory.ActionUrl);
                        _logger.LogInformation($"Created Proactive Memory for User {userId} near {loc.Location.Name}");
                    }
                }
            }
        }
    }
}
