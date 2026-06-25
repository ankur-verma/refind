using System;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Cortex.Database;
using Cortex.Modules.Content.Entities;
using Cortex.Modules.Search.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace Cortex.Modules.Search.Services;

public interface IInterestEngine
{
    Task ProcessBehaviorEventAsync(Guid userId, Guid? contentItemId, BehaviorEventType eventType, string? metadataJson = null, CancellationToken ct = default);
}

public class InterestEngine : IInterestEngine
{
    private readonly CortexDbContext _dbContext;
    private readonly IInterestScoringService _scoringService;
    private readonly IInterestTrendAnalyzer _trendAnalyzer;
    private readonly ILogger<InterestEngine> _logger;

    public InterestEngine(
        CortexDbContext dbContext,
        IInterestScoringService scoringService,
        IInterestTrendAnalyzer trendAnalyzer,
        ILogger<InterestEngine> logger)
    {
        _dbContext = dbContext;
        _scoringService = scoringService;
        _trendAnalyzer = trendAnalyzer;
        _logger = logger;
    }

    public async Task ProcessBehaviorEventAsync(Guid userId, Guid? contentItemId, BehaviorEventType eventType, string? metadataJson = null, CancellationToken ct = default)
    {
        // 1. Log the event
        var behaviorEvent = new UserBehaviorEvent
        {
            UserId = userId,
            ContentItemId = contentItemId,
            EventType = eventType,
            MetadataJson = metadataJson
        };
        _dbContext.UserBehaviorEvents.Add(behaviorEvent);

        // 2. Identify Categories to adjust
        string? categoryToAdjust = null;

        if (contentItemId.HasValue)
        {
            var contentInsight = await _dbContext.ContentInsights
                .FirstOrDefaultAsync(i => i.ContentItemId == contentItemId.Value, ct);

            if (contentInsight != null && !string.IsNullOrWhiteSpace(contentInsight.Category))
            {
                categoryToAdjust = contentInsight.Category;
            }
        }
        else if (eventType == BehaviorEventType.Searched && !string.IsNullOrWhiteSpace(metadataJson))
        {
            // If it's a search, we might infer category from metadata if the frontend provides it.
            // For now, we'll just log the event if no category is tied directly.
        }

        if (!string.IsNullOrWhiteSpace(categoryToAdjust))
        {
            var delta = _scoringService.GetScoreDelta(eventType);
            if (delta != 0)
            {
                await AdjustInterestScoreAsync(userId, categoryToAdjust, delta, eventType.ToString(), ct);
            }
        }

        await _dbContext.SaveChangesAsync(ct);
        _logger.LogInformation("Processed behavior event {EventType} for user {UserId}", eventType, userId);
    }

    private async Task AdjustInterestScoreAsync(Guid userId, string category, int delta, string reason, CancellationToken ct)
    {
        var normalizedCat = category.Trim();
        var interest = await _dbContext.UserInterests
            .FirstOrDefaultAsync(i => i.UserId == userId && i.Category.ToLower() == normalizedCat.ToLower(), ct);

        if (interest == null)
        {
            interest = new UserInterest
            {
                UserId = userId,
                Category = normalizedCat,
                Score = 50 + delta, // Starting base score is 50
                Trend = "Emerging",
                LastCalculated = DateTime.UtcNow
            };
            _dbContext.UserInterests.Add(interest);
        }
        else
        {
            var oldScore = interest.Score;
            interest.Score = Math.Clamp(interest.Score + delta, 0, 100);
            interest.LastCalculated = DateTime.UtcNow;

            _dbContext.InterestHistories.Add(new InterestHistory
            {
                UserInterest = interest,
                PreviousScore = oldScore,
                NewScore = interest.Score,
                Delta = delta,
                Reason = reason
            });
            
            _dbContext.UserInterests.Update(interest);
        }
        
        // We will save changes here so TrendAnalyzer can read the new history
        await _dbContext.SaveChangesAsync(ct);

        // Recalculate Trend
        interest.Trend = await _trendAnalyzer.AnalyzeTrendAsync(interest.Id, ct);
        _dbContext.UserInterests.Update(interest);
    }
}
