using Cortex.Modules.Content.Entities;
using Cortex.Shared;
using Cortex.Shared.Enums;
using Cortex.Database;
using Microsoft.EntityFrameworkCore;

namespace Cortex.Modules.Content.Persistence;

public class ContentItemRepository : IContentItemRepository
{
    private readonly CortexDbContext _context;
    public ContentItemRepository(CortexDbContext context) => _context = context;

    public async Task<ContentItem?> GetByIdAsync(Guid id, CancellationToken ct = default)
        => await _context.ContentItems.FirstOrDefaultAsync(x => x.Id == id && !x.IsDeleted, ct);

    public async Task<ContentItem?> GetByIdWithPayloadAsync(Guid id, CancellationToken ct = default)
        => await _context.ContentItems
            .Include(x => x.Payload)
            .Include(x => x.ActionItems.OrderBy(a => a.SequenceOrder))
            .Include(x => x.ContentItemTags).ThenInclude(ct => ct.Tag)
            .Include(x => x.VideoSegments.OrderBy(vs => vs.StartSeconds))
            .FirstOrDefaultAsync(x => x.Id == id && !x.IsDeleted, ct);

    public async Task<PagedList<ContentItem>> GetFeedAsync(Guid userId, EnergyLevel? energyLevel, PlatformType? platformType, int page, int pageSize, CancellationToken ct = default)
    {
        var query = _context.ContentItems
            .Where(x => x.UserId == userId && !x.IsArchived && !x.IsDeleted);

        if (energyLevel.HasValue)
            query = query.Where(x => x.EnergyLevel == energyLevel.Value);

        if (platformType.HasValue)
            query = query.Where(x => x.PlatformType == platformType.Value);

        var totalCount = await query.CountAsync(ct);
        var items = await query
            .OrderByDescending(x => x.IsPinned)
            .ThenByDescending(x => x.CreatedAt)
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .Include(x => x.ContentItemTags).ThenInclude(t => t.Tag)
            .ToListAsync(ct);

        return new PagedList<ContentItem>(items, totalCount, page, pageSize);
    }

    public async Task<ContentItem> AddAsync(ContentItem item, CancellationToken ct = default)
    {
        await _context.ContentItems.AddAsync(item, ct);
        return item;
    }

    public Task UpdateAsync(ContentItem item, CancellationToken ct = default)
    {
        _context.ContentItems.Update(item);
        return Task.CompletedTask;
    }

    public Task DeleteAsync(ContentItem item, CancellationToken ct = default)
    {
        item.IsDeleted = true;
        _context.ContentItems.Update(item);
        return Task.CompletedTask;
    }

    public async Task<bool> ExistsByUrlAsync(Guid userId, string originalUrl, CancellationToken ct = default)
        => await _context.ContentItems.AnyAsync(x => x.UserId == userId && x.OriginalUrl == originalUrl && !x.IsDeleted, ct);

    public async Task<List<ContentItem>> GetDeclutterQueueAsync(Guid userId, int count, CancellationToken ct = default)
        => await _context.ContentItems
            .Where(x => x.UserId == userId && !x.IsArchived && !x.IsPinned && x.Status == ContentStatus.Ready && !x.IsDeleted)
            .OrderBy(x => x.LastReviewedAt ?? DateTime.MinValue)
            .Take(count)
            .ToListAsync(ct);
}
