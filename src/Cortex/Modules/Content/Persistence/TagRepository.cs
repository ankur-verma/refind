using Cortex.Modules.Content.Entities;
using Cortex.Database;
using Microsoft.EntityFrameworkCore;

namespace Cortex.Modules.Content.Persistence;

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
