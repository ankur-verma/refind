using Cortex.Domain.Entities;
using Cortex.SharedKernel;
using Cortex.SharedKernel.Enums;

namespace Cortex.Application.Interfaces.Repositories;

/// <summary>
/// Repository for ContentItem aggregate operations including feed queries.
/// </summary>
public interface IContentItemRepository
{
    Task<ContentItem?> GetByIdAsync(Guid id, CancellationToken ct = default);
    Task<ContentItem?> GetByIdWithPayloadAsync(Guid id, CancellationToken ct = default);
    Task<PagedList<ContentItem>> GetFeedAsync(Guid userId, EnergyLevel? energyLevel, PlatformType? platformType, int page, int pageSize, CancellationToken ct = default);
    Task<ContentItem> AddAsync(ContentItem item, CancellationToken ct = default);
    Task UpdateAsync(ContentItem item, CancellationToken ct = default);
    Task DeleteAsync(ContentItem item, CancellationToken ct = default);
    Task<bool> ExistsByUrlAsync(Guid userId, string originalUrl, CancellationToken ct = default);
    Task<List<ContentItem>> GetDeclutterQueueAsync(Guid userId, int count, CancellationToken ct = default);
}
