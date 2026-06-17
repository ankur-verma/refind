using Cortex.Modules.Content.Entities;
using Cortex.Database;
using Microsoft.EntityFrameworkCore;

namespace Cortex.Modules.Content.Persistence;

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
