using Cortex.Modules.Auth.Entities;
using Cortex.Modules.Content.Entities;
using Cortex.Modules.Drip.Entities;
using Pgvector;

namespace Cortex.Modules.Content.Persistence;

/// <summary>
/// Repository for ContentPayload operations including vector search.
/// </summary>
public interface IContentPayloadRepository
{
    Task<ContentPayload?> GetByContentItemIdAsync(Guid contentItemId, CancellationToken ct = default);
    Task AddAsync(ContentPayload payload, CancellationToken ct = default);
    Task UpdateAsync(ContentPayload payload, CancellationToken ct = default);
    Task<List<(ContentPayload Payload, double Distance)>> SearchByVectorAsync(Guid userId, Vector queryVector, string activeProvider, int limit, CancellationToken ct = default);
}
