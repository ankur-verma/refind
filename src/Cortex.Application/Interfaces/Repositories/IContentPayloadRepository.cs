using Cortex.Domain.Entities;
using Pgvector;

namespace Cortex.Application.Interfaces.Repositories;

/// <summary>
/// Repository for ContentPayload operations including vector search.
/// </summary>
public interface IContentPayloadRepository
{
    Task<ContentPayload?> GetByContentItemIdAsync(Guid contentItemId, CancellationToken ct = default);
    Task AddAsync(ContentPayload payload, CancellationToken ct = default);
    Task UpdateAsync(ContentPayload payload, CancellationToken ct = default);
    Task<List<ContentPayload>> SearchByVectorAsync(Guid userId, Vector queryVector, int limit, CancellationToken ct = default);
}
