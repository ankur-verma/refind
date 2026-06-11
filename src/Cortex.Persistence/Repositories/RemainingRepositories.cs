using Cortex.Application.Interfaces.Repositories;
using Cortex.Domain.Entities;
using Microsoft.EntityFrameworkCore;

namespace Cortex.Persistence.Repositories;

public class ActionItemRepository : IActionItemRepository
{
    private readonly CortexDbContext _context;
    public ActionItemRepository(CortexDbContext context) => _context = context;

    public async Task<List<ActionItem>> GetByContentItemIdAsync(Guid contentItemId, CancellationToken ct = default)
        => await _context.ActionItems.Where(x => x.ContentItemId == contentItemId).OrderBy(x => x.SequenceOrder).ToListAsync(ct);

    public async Task AddRangeAsync(IEnumerable<ActionItem> items, CancellationToken ct = default)
        => await _context.ActionItems.AddRangeAsync(items, ct);

    public Task UpdateAsync(ActionItem item, CancellationToken ct = default)
    {
        _context.ActionItems.Update(item);
        return Task.CompletedTask;
    }
}

public class DripTrackRepository : IDripTrackRepository
{
    private readonly CortexDbContext _context;
    public DripTrackRepository(CortexDbContext context) => _context = context;

    public async Task<DripTrack?> GetByIdAsync(Guid id, CancellationToken ct = default)
        => await _context.DripTracks.FirstOrDefaultAsync(x => x.Id == id, ct);

    public async Task<DripTrack?> GetByIdWithStepsAsync(Guid id, CancellationToken ct = default)
        => await _context.DripTracks.Include(x => x.Steps.OrderBy(s => s.DayNumber)).FirstOrDefaultAsync(x => x.Id == id, ct);

    public async Task<List<DripTrack>> GetByUserIdAsync(Guid userId, CancellationToken ct = default)
        => await _context.DripTracks
            .Include(x => x.ContentItem)
            .Include(x => x.Steps.OrderBy(s => s.DayNumber))
            .Where(x => x.UserId == userId)
            .OrderByDescending(x => x.CreatedAt)
            .ToListAsync(ct);

    public async Task<DripTrack> AddAsync(DripTrack track, CancellationToken ct = default)
    {
        await _context.DripTracks.AddAsync(track, ct);
        return track;
    }

    public Task UpdateAsync(DripTrack track, CancellationToken ct = default)
    {
        _context.DripTracks.Update(track);
        return Task.CompletedTask;
    }
}

public class DripStepRepository : IDripStepRepository
{
    private readonly CortexDbContext _context;
    public DripStepRepository(CortexDbContext context) => _context = context;

    public async Task<DripStep?> GetByIdAsync(Guid id, CancellationToken ct = default)
        => await _context.DripSteps.FirstOrDefaultAsync(x => x.Id == id, ct);

    public async Task AddRangeAsync(IEnumerable<DripStep> steps, CancellationToken ct = default)
        => await _context.DripSteps.AddRangeAsync(steps, ct);

    public Task UpdateAsync(DripStep step, CancellationToken ct = default)
    {
        _context.DripSteps.Update(step);
        return Task.CompletedTask;
    }
}

public class TagRepository : ITagRepository
{
    private readonly CortexDbContext _context;
    public TagRepository(CortexDbContext context) => _context = context;

    public async Task<Tag?> GetByNameAsync(string name, CancellationToken ct = default)
        => await _context.Tags.FirstOrDefaultAsync(x => x.Name == name.ToLowerInvariant(), ct);

    public async Task<List<Tag>> GetByContentItemIdAsync(Guid contentItemId, CancellationToken ct = default)
        => await _context.ContentItemTags.Where(x => x.ContentItemId == contentItemId).Select(x => x.Tag).ToListAsync(ct);

    public async Task<Tag> AddAsync(Tag tag, CancellationToken ct = default)
    {
        tag.Name = tag.Name.ToLowerInvariant();
        await _context.Tags.AddAsync(tag, ct);
        return tag;
    }

    public async Task AddContentItemTagAsync(Guid contentItemId, Guid tagId, CancellationToken ct = default)
        => await _context.ContentItemTags.AddAsync(new ContentItemTag { ContentItemId = contentItemId, TagId = tagId }, ct);
}

public class SubscriptionRepository : ISubscriptionRepository
{
    private readonly CortexDbContext _context;
    public SubscriptionRepository(CortexDbContext context) => _context = context;

    public async Task<Subscription?> GetByUserIdAsync(Guid userId, CancellationToken ct = default)
        => await _context.Subscriptions.FirstOrDefaultAsync(x => x.UserId == userId && x.IsActive, ct);

    public async Task<Subscription> AddAsync(Subscription subscription, CancellationToken ct = default)
    {
        await _context.Subscriptions.AddAsync(subscription, ct);
        return subscription;
    }

    public Task UpdateAsync(Subscription subscription, CancellationToken ct = default)
    {
        _context.Subscriptions.Update(subscription);
        return Task.CompletedTask;
    }
}

public class UnitOfWork : IUnitOfWork
{
    private readonly CortexDbContext _context;
    public UnitOfWork(CortexDbContext context) => _context = context;

    public async Task<int> SaveChangesAsync(CancellationToken ct = default)
        => await _context.SaveChangesAsync(ct);

    public void Dispose() => _context.Dispose();
}
