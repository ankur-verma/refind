using System;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Cortex.Database;
using Cortex.Modules.Search.Entities;
using Microsoft.EntityFrameworkCore;

namespace Cortex.Modules.Search.Services;

public interface IInterestTrendAnalyzer
{
    Task<string> AnalyzeTrendAsync(Guid userInterestId, CancellationToken ct = default);
}

public class InterestTrendAnalyzer : IInterestTrendAnalyzer
{
    private readonly CortexDbContext _dbContext;

    public InterestTrendAnalyzer(CortexDbContext dbContext)
    {
        _dbContext = dbContext;
    }

    public async Task<string> AnalyzeTrendAsync(Guid userInterestId, CancellationToken ct = default)
    {
        var recentHistory = await _dbContext.InterestHistories
            .Where(h => h.UserInterestId == userInterestId && h.CreatedAt >= DateTime.UtcNow.AddDays(-7))
            .ToListAsync(ct);

        if (!recentHistory.Any())
        {
            return "Declining";
        }

        var totalDelta = recentHistory.Sum(h => h.Delta);

        if (totalDelta >= 15) return "Emerging";
        if (totalDelta > 0) return "Stable";
        if (totalDelta < -10) return "Declining";
        
        return "Stable";
    }
}
