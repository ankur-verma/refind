using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Cortex.Database;
using Cortex.Infrastructure.AI;
using Cortex.Modules.Search.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace Cortex.Modules.Search.Services;

public class RecommendationService : IRecommendationService
{
    private readonly CortexDbContext _dbContext;
    private readonly IPythonAIService _pythonAiService;
    private readonly ILogger<RecommendationService> _logger;

    public RecommendationService(
        CortexDbContext dbContext,
        IPythonAIService pythonAiService,
        ILogger<RecommendationService> logger)
    {
        _dbContext = dbContext;
        _pythonAiService = pythonAiService;
        _logger = logger;
    }

    public async Task GenerateAndStoreRecommendationsAsync(Guid userId, CancellationToken ct = default)
    {
        _logger.LogInformation("Generating hybrid recommendations for user {UserId}", userId);
        
        var response = await _pythonAiService.GetHybridRecommendationsAsync(userId, ct);
        if (response == null || response.Recommendations == null)
        {
            _logger.LogWarning("Failed to generate recommendations for user {UserId} from Python service", userId);
            return;
        }

        using var transaction = await _dbContext.Database.BeginTransactionAsync(ct);
        try
        {
            // Clear old recommendations for this user
            var oldRecs = await _dbContext.UserRecommendations
                .Where(x => x.UserId == userId)
                .ToListAsync(ct);
                
            _dbContext.UserRecommendations.RemoveRange(oldRecs);

            // Map and add new ones
            var newRecs = new List<UserRecommendation>();
            
            foreach (var group in response.Recommendations)
            {
                var category = group.Key;
                foreach (var item in group.Value)
                {
                    newRecs.Add(new UserRecommendation
                    {
                        UserId = userId,
                        RecommendationType = category,
                        TargetId = item.TargetId,
                        Title = item.Title,
                        Score = item.Score,
                        Reason = item.Reason,
                        CreatedAt = DateTime.UtcNow
                    });
                }
            }

            _dbContext.UserRecommendations.AddRange(newRecs);
            await _dbContext.SaveChangesAsync(ct);
            await transaction.CommitAsync(ct);
            
            _logger.LogInformation("Successfully stored {Count} new recommendations for user {UserId}", newRecs.Count, userId);
        }
        catch (Exception ex)
        {
            await transaction.RollbackAsync(ct);
            _logger.LogError(ex, "Failed to store recommendations for user {UserId}", userId);
            throw;
        }
    }

    public async Task<Dictionary<string, List<UserRecommendation>>> GetPersonalizedRecommendationsAsync(Guid userId, CancellationToken ct = default)
    {
        var recs = await _dbContext.UserRecommendations
            .Where(x => x.UserId == userId)
            .OrderByDescending(x => x.Score)
            .ToListAsync(ct);

        var grouped = recs
            .GroupBy(x => x.RecommendationType)
            .ToDictionary(g => g.Key, g => g.ToList());

        return grouped;
    }
}
