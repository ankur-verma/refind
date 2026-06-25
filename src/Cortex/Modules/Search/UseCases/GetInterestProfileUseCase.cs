using Cortex.Database;
using Cortex.Shared;
using Microsoft.EntityFrameworkCore;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace Cortex.Modules.Search.UseCases;

public record InterestCategoryResponse(string Category, int Score, string Trend, DateTime LastCalculated);
public record UserIntentResponse(Guid Id, string GoalDescription, double Confidence, bool IsResolved, DateTime CreatedAt);
public record AutoCollectionResponse(Guid Id, string Name, string Description, Guid? UserIntentId);

public record UserInterestProfileResponse(
    List<InterestCategoryResponse> Categories,
    List<UserIntentResponse> Intents,
    List<AutoCollectionResponse> AutoCollections
);

public class GetInterestProfileUseCase
{
    private readonly CortexDbContext _context;

    public GetInterestProfileUseCase(CortexDbContext context)
    {
        _context = context;
    }

    public async Task<Result<UserInterestProfileResponse>> ExecuteAsync(Guid userId, CancellationToken ct = default)
    {
        var categories = await _context.UserInterests
            .Where(x => x.UserId == userId && !x.IsDeleted)
            .OrderByDescending(x => x.Score)
            .Select(x => new InterestCategoryResponse(x.Category, x.Score, x.Trend, x.LastCalculated))
            .ToListAsync(ct);

        var intents = await _context.UserIntents
            .Where(x => x.UserId == userId && !x.IsDeleted)
            .OrderByDescending(x => x.Confidence)
            .Select(x => new UserIntentResponse(x.Id, x.GoalDescription, x.Confidence, x.IsResolved, x.CreatedAt))
            .ToListAsync(ct);

        var collections = await _context.AutoCollections
            .Where(x => x.UserId == userId && !x.IsDeleted)
            .OrderByDescending(x => x.CreatedAt)
            .Select(x => new AutoCollectionResponse(x.Id, x.Name, x.Description, x.UserIntentId))
            .ToListAsync(ct);

        var response = new UserInterestProfileResponse(categories, intents, collections);
        return Result.Success(response);
    }
}
