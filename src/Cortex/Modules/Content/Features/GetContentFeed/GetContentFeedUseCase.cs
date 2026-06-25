using Cortex.Modules.Content.DTOs;
using Cortex.Modules.Content.Services;
using Cortex.Modules.Auth.Persistence;
using Cortex.Modules.Content.Persistence;
using Cortex.Modules.Drip.Persistence;
using Cortex.Database;
using Cortex.Modules.Auth.Services;
using Cortex.Infrastructure.AI;
using Cortex.Infrastructure.Caching;
using Cortex.Infrastructure.Messaging;
using Cortex.Modules.Auth.Entities;
using Cortex.Modules.Content.Entities;
using Cortex.Modules.Drip.Entities;
using Cortex.Shared;
using Cortex.Shared.Enums;
using Cortex.Shared.Exceptions;

namespace Cortex.Modules.Content.UseCases;

/// <summary>
/// Returns mood-filtered content feed for the Cognitive Dashboard.
/// </summary>
public class GetContentFeedUseCase
{
    private readonly IContentItemRepository _contentRepo;
    private readonly ICacheService _cache;

    public GetContentFeedUseCase(IContentItemRepository contentRepo, ICacheService cache)
    {
        _contentRepo = contentRepo;
        _cache = cache;
    }

    public async Task<PagedList<ContentItem>> ExecuteAsync(Guid userId, ContentFeedQuery query, CancellationToken ct = default)
    {
        var cacheKey = $"feed:{userId}:{query.EnergyLevel}:{query.PlatformType}:{query.Page}";
        var cached = await _cache.GetAsync<PagedList<ContentItem>>(cacheKey, ct);
        if (cached is not null)
            return cached;

        var result = await _contentRepo.GetFeedAsync(
            userId, query.EnergyLevel, query.PlatformType, query.Page, query.PageSize, ct);

        await _cache.SetAsync(cacheKey, result, TimeSpan.FromMinutes(5), ct);
        return result;
    }
}
