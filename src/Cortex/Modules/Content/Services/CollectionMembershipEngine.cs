using System;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Cortex.Database;
using Cortex.Modules.Content.Domain.Rules;
using Cortex.Modules.Content.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace Cortex.Modules.Content.Services;

public class CollectionMembershipEngine : ICollectionMembershipEngine
{
    private readonly CortexDbContext _dbContext;
    private readonly IRulesEngine _rulesEngine;
    private readonly ILogger<CollectionMembershipEngine> _logger;

    public CollectionMembershipEngine(
        CortexDbContext dbContext,
        IRulesEngine rulesEngine,
        ILogger<CollectionMembershipEngine> logger)
    {
        _dbContext = dbContext;
        _rulesEngine = rulesEngine;
        _logger = logger;
    }

    public async Task EvaluateItemAsync(Guid contentItemId, CancellationToken ct = default)
    {
        var item = await _dbContext.ContentItems
            .Include(c => c.Insight)
                .ThenInclude(i => i!.Entities)
                    .ThenInclude(e => e.SemanticEntity)
            .Include(c => c.Insight)
                .ThenInclude(i => i!.Locations)
                    .ThenInclude(l => l.Location)
            .Include(c => c.Insight)
                .ThenInclude(i => i!.Products)
                    .ThenInclude(p => p.Product)
            .Include(c => c.Insight)
                .ThenInclude(i => i!.Brands)
                    .ThenInclude(b => b.Brand)
            .FirstOrDefaultAsync(c => c.Id == contentItemId, ct);

        if (item == null || item.Insight == null) return;

        var collections = await _dbContext.AutoCollections
            .Where(c => c.UserId == item.UserId && !string.IsNullOrWhiteSpace(c.RuleDefinition))
            .ToListAsync(ct);

        foreach (var collection in collections)
        {
            var isMatch = _rulesEngine.Evaluate(item, collection.RuleDefinition);
            var membership = await _dbContext.CollectionMemberships
                .FirstOrDefaultAsync(m => m.AutoCollectionId == collection.Id && m.ContentItemId == item.Id, ct);

            if (isMatch && membership == null)
            {
                _dbContext.CollectionMemberships.Add(new CollectionMembership
                {
                    AutoCollectionId = collection.Id,
                    ContentItemId = item.Id
                });
            }
            else if (!isMatch && membership != null)
            {
                _dbContext.CollectionMemberships.Remove(membership);
            }
        }

        await _dbContext.SaveChangesAsync(ct);
        _logger.LogInformation("Evaluated ContentItem {ContentItemId} against {Count} collections.", contentItemId, collections.Count);
    }

    public async Task EvaluateCollectionAsync(Guid collectionId, CancellationToken ct = default)
    {
        var collection = await _dbContext.AutoCollections.FindAsync(new object[] { collectionId }, ct);
        if (collection == null || string.IsNullOrWhiteSpace(collection.RuleDefinition)) return;

        var items = await _dbContext.ContentItems
            .Include(c => c.Insight)
                .ThenInclude(i => i!.Entities)
                    .ThenInclude(e => e.SemanticEntity)
            .Include(c => c.Insight)
                .ThenInclude(i => i!.Locations)
                    .ThenInclude(l => l.Location)
            .Include(c => c.Insight)
                .ThenInclude(i => i!.Products)
                    .ThenInclude(p => p.Product)
            .Include(c => c.Insight)
                .ThenInclude(i => i!.Brands)
                    .ThenInclude(b => b.Brand)
            .Where(c => c.UserId == collection.UserId && c.Insight != null)
            .ToListAsync(ct);

        foreach (var item in items)
        {
            var isMatch = _rulesEngine.Evaluate(item, collection.RuleDefinition);
            var membership = await _dbContext.CollectionMemberships
                .FirstOrDefaultAsync(m => m.AutoCollectionId == collection.Id && m.ContentItemId == item.Id, ct);

            if (isMatch && membership == null)
            {
                _dbContext.CollectionMemberships.Add(new CollectionMembership
                {
                    AutoCollectionId = collection.Id,
                    ContentItemId = item.Id
                });
            }
            else if (!isMatch && membership != null)
            {
                _dbContext.CollectionMemberships.Remove(membership);
            }
        }

        await _dbContext.SaveChangesAsync(ct);
        _logger.LogInformation("Evaluated AutoCollection {CollectionId} against {Count} content items.", collectionId, items.Count);
    }
}
