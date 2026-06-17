using Cortex.Modules.Content.Entities;

namespace Cortex.Modules.Content.Persistence;

public interface IActionItemRepository
{
    Task<List<ActionItem>> GetByContentItemIdAsync(Guid contentItemId, CancellationToken ct = default);
    Task AddRangeAsync(IEnumerable<ActionItem> items, CancellationToken ct = default);
    Task UpdateAsync(ActionItem item, CancellationToken ct = default);
}
