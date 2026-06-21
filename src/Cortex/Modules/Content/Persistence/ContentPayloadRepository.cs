using Cortex.Modules.Content.Entities;
using Cortex.Database;
using Microsoft.EntityFrameworkCore;
using Pgvector;
using Pgvector.EntityFrameworkCore;

namespace Cortex.Modules.Content.Persistence;

public class ContentPayloadRepository : IContentPayloadRepository
{
    private readonly CortexDbContext _context;
    public ContentPayloadRepository(CortexDbContext context) => _context = context;

    public async Task<ContentPayload?> GetByContentItemIdAsync(Guid contentItemId, CancellationToken ct = default)
        => await _context.ContentPayloads.FirstOrDefaultAsync(x => x.ContentItemId == contentItemId, ct);

    public async Task AddAsync(ContentPayload payload, CancellationToken ct = default)
        => await _context.ContentPayloads.AddAsync(payload, ct);

    public Task UpdateAsync(ContentPayload payload, CancellationToken ct = default)
    {
        _context.ContentPayloads.Update(payload);
        return Task.CompletedTask;
    }

    public async Task<List<(ContentPayload Payload, double Distance)>> SearchByVectorAsync(Guid userId, Pgvector.Vector queryVector, string activeProvider, int limit, CancellationToken ct = default)
    {
        // Uses pgvector cosine distance operator for similarity search
        if (activeProvider == "LocalOllama")
        {
            var results = await _context.ContentPayloads
                .Include(p => p.ContentItem)
                .Where(p => p.ContentItem.UserId == userId && p.OllamaEmbedding != null)
                .Select(p => new { Payload = p, Distance = p.OllamaEmbedding!.CosineDistance(queryVector) })
                .OrderBy(x => x.Distance)
                .Take(limit)
                .ToListAsync(ct);
            return results.Select(x => (x.Payload, x.Distance)).ToList();
        }
        else
        {
            var results = await _context.ContentPayloads
                .Include(p => p.ContentItem)
                .Where(p => p.ContentItem.UserId == userId && p.GeminiEmbedding != null)
                .Select(p => new { Payload = p, Distance = p.GeminiEmbedding!.CosineDistance(queryVector) })
                .OrderBy(x => x.Distance)
                .Take(limit)
                .ToListAsync(ct);
            return results.Select(x => (x.Payload, x.Distance)).ToList();
        }
    }
}
