using Cortex.Modules.Content.Entities;

namespace Cortex.Modules.Content.Persistence;

public interface ITagRepository
{
    Task<Tag?> GetByNameAsync(string name, CancellationToken ct = default);
    Task<List<Tag>> GetByContentItemIdAsync(Guid contentItemId, CancellationToken ct = default);
    Task<Tag> AddAsync(Tag tag, CancellationToken ct = default);
    Task AddContentItemTagAsync(Guid contentItemId, Guid tagId, CancellationToken ct = default);
}
