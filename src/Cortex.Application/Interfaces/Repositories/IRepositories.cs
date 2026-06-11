using Cortex.Domain.Entities;

namespace Cortex.Application.Interfaces.Repositories;

public interface IActionItemRepository
{
    Task<List<ActionItem>> GetByContentItemIdAsync(Guid contentItemId, CancellationToken ct = default);
    Task AddRangeAsync(IEnumerable<ActionItem> items, CancellationToken ct = default);
    Task UpdateAsync(ActionItem item, CancellationToken ct = default);
}

public interface IDripTrackRepository
{
    Task<DripTrack?> GetByIdAsync(Guid id, CancellationToken ct = default);
    Task<DripTrack?> GetByIdWithStepsAsync(Guid id, CancellationToken ct = default);
    Task<List<DripTrack>> GetByUserIdAsync(Guid userId, CancellationToken ct = default);
    Task<DripTrack> AddAsync(DripTrack track, CancellationToken ct = default);
    Task UpdateAsync(DripTrack track, CancellationToken ct = default);
}

public interface IDripStepRepository
{
    Task<DripStep?> GetByIdAsync(Guid id, CancellationToken ct = default);
    Task AddRangeAsync(IEnumerable<DripStep> steps, CancellationToken ct = default);
    Task UpdateAsync(DripStep step, CancellationToken ct = default);
}

public interface ITagRepository
{
    Task<Tag?> GetByNameAsync(string name, CancellationToken ct = default);
    Task<List<Tag>> GetByContentItemIdAsync(Guid contentItemId, CancellationToken ct = default);
    Task<Tag> AddAsync(Tag tag, CancellationToken ct = default);
    Task AddContentItemTagAsync(Guid contentItemId, Guid tagId, CancellationToken ct = default);
}

public interface ISubscriptionRepository
{
    Task<Subscription?> GetByUserIdAsync(Guid userId, CancellationToken ct = default);
    Task<Subscription> AddAsync(Subscription subscription, CancellationToken ct = default);
    Task UpdateAsync(Subscription subscription, CancellationToken ct = default);
}
