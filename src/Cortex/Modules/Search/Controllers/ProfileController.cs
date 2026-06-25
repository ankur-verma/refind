using System;
using System.Linq;
using System.Security.Claims;
using System.Threading;
using System.Threading.Tasks;
using Cortex.Database;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Cortex.Shared;

namespace Cortex.Modules.Search.Controllers;

[ApiController]
[Route("api/v1/profile")]
[Authorize]
public class ProfileController : ControllerBase
{
    private readonly CortexDbContext _dbContext;

    public ProfileController(CortexDbContext dbContext)
    {
        _dbContext = dbContext;
    }

    [HttpGet("overview")]
    public async Task<IActionResult> GetOverview(CancellationToken ct)
    {
        var userId = GetUserId();
        
        // This is a proxy for now since we don't have dedicated aggregates
        // You'd normally use UserActivityLogs or pre-calculated aggregates
        var savesCount = await _dbContext.UserBehaviorEvents.CountAsync(e => e.UserId == userId && e.EventType == Search.Entities.BehaviorEventType.Saved, ct);
        var aiQueries = await _dbContext.UserBehaviorEvents.CountAsync(e => e.UserId == userId && e.EventType == Search.Entities.BehaviorEventType.Searched, ct);
        var deepDives = await _dbContext.UserBehaviorEvents.CountAsync(e => e.UserId == userId && e.EventType == Search.Entities.BehaviorEventType.Searched, ct);

        // Fallback to defaults if there's no data yet to keep UI looking nice,
        // or just return real numbers. We will return real numbers.
        return Ok(new {
            success = true,
            data = new {
                totalSaves = savesCount,
                aiQueries = aiQueries,
                deepDives = deepDives,
                activeSince = DateTime.UtcNow.Year
            }
        });
    }

    [HttpGet("activity-graph")]
    public async Task<IActionResult> GetActivityGraph(CancellationToken ct)
    {
        var userId = GetUserId();
        var cutoff = DateTime.UtcNow.AddDays(-14);

        var events = await _dbContext.UserBehaviorEvents
            .Where(e => e.UserId == userId && e.CreatedAt >= cutoff)
            .GroupBy(e => new { e.CreatedAt.Date, e.EventType })
            .Select(g => new { Date = g.Key.Date, EventType = g.Key.EventType, Count = g.Count() })
            .ToListAsync(ct);

        // Fill in empty days
        var result = new List<object>();
        for (int i = 13; i >= 0; i--)
        {
            var day = DateTime.UtcNow.Date.AddDays(-i);
            var dayEvents = events.Where(e => e.Date == day).ToList();
            
            result.Add(new {
                date = day.ToString("MMM dd"),
                views = dayEvents.Where(e => e.EventType == Search.Entities.BehaviorEventType.Viewed || e.EventType == Search.Entities.BehaviorEventType.Opened).Sum(e => e.Count),
                saves = dayEvents.Where(e => e.EventType == Search.Entities.BehaviorEventType.Saved || e.EventType == Search.Entities.BehaviorEventType.Bookmarked).Sum(e => e.Count),
                searches = dayEvents.Where(e => e.EventType == Search.Entities.BehaviorEventType.Searched).Sum(e => e.Count),
            });
        }

        return Ok(IntelligenceResponse<object>.Success(result, 1.0, result.Count, null));
    }

    [HttpGet("interests")]
    public async Task<IActionResult> GetInterests(CancellationToken ct)
    {
        var userId = GetUserId();

        var interactionCount = await _dbContext.UserBehaviorEvents.CountAsync(e => e.UserId == userId, ct);

        var interests = await _dbContext.UserInterests
            .Where(i => i.UserId == userId && !i.IsDeleted)
            .OrderByDescending(i => i.Score)
            .Take(10)
            .Select(i => new
            {
                topic = i.Category,
                momentum = i.Trend == "Emerging" || i.Trend == "Stable" ? "up" : "down",
                percentage = i.Score,
                color = i.Trend == "Declining" ? "text-red-500" : "text-green-500",
                bg = i.Trend == "Declining" ? "bg-red-500/10" : "bg-green-500/10"
            })
            .ToListAsync(ct);

        var generatedFrom = interests.Select(i => i.topic).ToList();
        return Ok(IntelligenceResponse<object>.Success(interests, 0.92, interactionCount, generatedFrom));
    }

    [HttpGet("categories")]
    public async Task<IActionResult> GetCategories(CancellationToken ct)
    {
        var userId = GetUserId();
        
        // Aggregate saved content insights by category
        var categories = await _dbContext.ContentInsights
            .Where(i => i.Category != null && i.Category != "")
            .GroupBy(i => i.Category)
            .Select(g => new { name = g.Key, count = g.Count() })
            .OrderByDescending(x => x.count)
            .Take(5)
            .ToListAsync(ct);

        if (!categories.Any())
        {
            return Ok(IntelligenceResponse<object>.InsufficientData("insufficient_data"));
        }

        var total = categories.Sum(c => c.count);
        var result = categories.Select(c => new {
            name = c.name,
            count = c.count,
            percentage = total > 0 ? (int)((double)c.count / total * 100) : 0
        }).ToList();

        var generatedFrom = result.Select(r => r.name).ToList();
        return Ok(IntelligenceResponse<object>.Success(result, 0.95, total, generatedFrom));
    }

    [HttpGet("upcoming-plans")]
    public async Task<IActionResult> GetUpcomingPlans(CancellationToken ct)
    {
        var userId = GetUserId();
        
        var savesCount = await _dbContext.ContentItems.CountAsync(c => c.UserId == userId, ct);

        var intents = await _dbContext.UserIntents
            .Where(i => i.UserId == userId && !i.IsResolved)
            .OrderByDescending(i => i.Confidence)
            .Take(4)
            .Select(i => new {
                id = i.Id,
                title = i.GoalDescription,
                description = i.GoalDescription.Contains("Trip") ? "Based on recent travel saves" : "High Intent",
                icon = i.GoalDescription.Contains("Trip") ? "✈️" : "📸",
                confidence = i.Confidence
            })
            .ToListAsync(ct);

        if (!intents.Any())
        {
            return Ok(IntelligenceResponse<object>.InsufficientData("insufficient_data"));
        }

        var generatedFrom = intents.Select(i => i.id.ToString()).ToList();
        return Ok(IntelligenceResponse<object>.Success(intents, intents.First().confidence, savesCount, generatedFrom));
    }

    [HttpGet("revisited")]
    public async Task<IActionResult> GetRevisited(CancellationToken ct)
    {
        var userId = GetUserId();
        
        var behaviorCount = await _dbContext.UserBehaviorEvents.CountAsync(e => e.UserId == userId, ct);

        // Get content items that have the most 'Viewed' or 'Revisited' events
        var revisitedContentIds = await _dbContext.UserBehaviorEvents
            .Where(e => e.UserId == userId && e.ContentItemId != null && (e.EventType == Search.Entities.BehaviorEventType.Viewed || e.EventType == Search.Entities.BehaviorEventType.Searched))
            .GroupBy(e => e.ContentItemId)
            .Select(g => new { ContentItemId = g.Key, Count = g.Count() })
            .OrderByDescending(x => x.Count)
            .Take(6)
            .Select(x => x.ContentItemId)
            .ToListAsync(ct);

        if (!revisitedContentIds.Any())
        {
            return Ok(IntelligenceResponse<object>.InsufficientData("insufficient_data"));
        }

        var contentItems = await _dbContext.ContentItems
            .Include(c => c.Insight)
            .Where(c => revisitedContentIds.Contains(c.Id))
            .Select(c => new {
                id = c.Id,
                title = c.Title,
                url = c.OriginalUrl,
                platformType = c.PlatformType.ToString(),
                summary = c.Payload != null ? c.Payload.QuickSparkSummary : "",
                thumbnailUrl = c.HeroImageUrl,
                aiInsights = new { intent = c.Insight != null ? c.Insight.Category : "General", confidence = 0.95 }
            })
            .ToListAsync(ct);

        var generatedFrom = contentItems.Select(c => c.id.ToString()).ToList();
        return Ok(IntelligenceResponse<object>.Success(contentItems, 0.99, behaviorCount, generatedFrom));
    }

    private Guid GetUserId()
    {
        var userIdClaim = User.Claims.FirstOrDefault(c => c.Type == "id" || c.Type == ClaimTypes.NameIdentifier)?.Value;
        if (Guid.TryParse(userIdClaim, out var userId)) return userId;
        throw new UnauthorizedAccessException("Invalid User ID claim");
    }
}
