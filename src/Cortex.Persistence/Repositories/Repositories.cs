using Cortex.Application.Interfaces.Repositories;
using Cortex.Domain.Entities;
using Cortex.SharedKernel;
using Cortex.SharedKernel.Enums;
using Microsoft.EntityFrameworkCore;
using Pgvector.EntityFrameworkCore;

namespace Cortex.Persistence.Repositories;

public class UserRepository : IUserRepository
{
    private readonly CortexDbContext _context;
    public UserRepository(CortexDbContext context) => _context = context;

    public async Task<User?> GetByIdAsync(Guid id, CancellationToken ct = default)
        => await _context.Users.Include(u => u.Subscription).FirstOrDefaultAsync(u => u.Id == id, ct);

    public async Task<User?> GetByEmailAsync(string email, CancellationToken ct = default)
        => await _context.Users.Include(u => u.Identities).Include(u => u.Subscription).FirstOrDefaultAsync(u => u.Email == email, ct);

    public async Task<User?> GetByIdentityAsync(AuthProvider provider, string providerId, CancellationToken ct = default)
        => await _context.Users
            .Include(u => u.Identities)
            .Include(u => u.Subscription)
            .FirstOrDefaultAsync(u => u.Identities.Any(i => i.AuthProvider == provider && i.ProviderId == providerId), ct);

    public async Task<User?> GetByRefreshTokenHashAsync(string refreshTokenHash, CancellationToken ct = default)
        => await _context.Users
            .Include(u => u.Subscription)
            .FirstOrDefaultAsync(u => u.RefreshTokenHash == refreshTokenHash && u.RefreshTokenExpiresAt > DateTime.UtcNow, ct);

    public async Task<User> AddAsync(User user, CancellationToken ct = default)
    {
        await _context.Users.AddAsync(user, ct);
        return user;
    }

    public Task UpdateAsync(User user, CancellationToken ct = default)
    {
        _context.Users.Update(user);
        return Task.CompletedTask;
    }

    public async Task<bool> ExistsAsync(string email, CancellationToken ct = default)
        => await _context.Users.AnyAsync(u => u.Email == email, ct);
}

public class ContentItemRepository : IContentItemRepository
{
    private readonly CortexDbContext _context;
    public ContentItemRepository(CortexDbContext context) => _context = context;

    public async Task<ContentItem?> GetByIdAsync(Guid id, CancellationToken ct = default)
        => await _context.ContentItems.FirstOrDefaultAsync(x => x.Id == id, ct);

    public async Task<ContentItem?> GetByIdWithPayloadAsync(Guid id, CancellationToken ct = default)
        => await _context.ContentItems
            .Include(x => x.Payload)
            .Include(x => x.ActionItems.OrderBy(a => a.SequenceOrder))
            .Include(x => x.ContentItemTags).ThenInclude(ct => ct.Tag)
            .FirstOrDefaultAsync(x => x.Id == id, ct);

    public async Task<PagedList<ContentItem>> GetFeedAsync(Guid userId, EnergyLevel? energyLevel, PlatformType? platformType, int page, int pageSize, CancellationToken ct = default)
    {
        var query = _context.ContentItems
            .Where(x => x.UserId == userId && !x.IsArchived && x.Status == ContentStatus.Ready);

        if (energyLevel.HasValue)
            query = query.Where(x => x.EnergyLevel == energyLevel.Value);

        if (platformType.HasValue)
            query = query.Where(x => x.PlatformType == platformType.Value);

        var totalCount = await query.CountAsync(ct);
        var items = await query
            .OrderByDescending(x => x.IsPinned)
            .ThenByDescending(x => x.CreatedAt)
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .Include(x => x.ContentItemTags).ThenInclude(t => t.Tag)
            .ToListAsync(ct);

        return new PagedList<ContentItem>(items, totalCount, page, pageSize);
    }

    public async Task<ContentItem> AddAsync(ContentItem item, CancellationToken ct = default)
    {
        await _context.ContentItems.AddAsync(item, ct);
        return item;
    }

    public Task UpdateAsync(ContentItem item, CancellationToken ct = default)
    {
        _context.ContentItems.Update(item);
        return Task.CompletedTask;
    }

    public Task DeleteAsync(ContentItem item, CancellationToken ct = default)
    {
        _context.ContentItems.Remove(item);
        return Task.CompletedTask;
    }

    public async Task<bool> ExistsByUrlAsync(Guid userId, string originalUrl, CancellationToken ct = default)
        => await _context.ContentItems.AnyAsync(x => x.UserId == userId && x.OriginalUrl == originalUrl, ct);

    public async Task<List<ContentItem>> GetDeclutterQueueAsync(Guid userId, int count, CancellationToken ct = default)
        => await _context.ContentItems
            .Where(x => x.UserId == userId && !x.IsArchived && !x.IsPinned && x.Status == ContentStatus.Ready)
            .OrderBy(x => x.LastReviewedAt ?? DateTime.MinValue)
            .Take(count)
            .ToListAsync(ct);
}

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

    public async Task<List<ContentPayload>> SearchByVectorAsync(Guid userId, Pgvector.Vector queryVector, int limit, CancellationToken ct = default)
    {
        // Uses pgvector cosine distance operator for similarity search
        return await _context.ContentPayloads
            .Include(p => p.ContentItem)
            .Where(p => p.ContentItem.UserId == userId && p.SemanticEmbedding != null)
            .OrderBy(p => p.SemanticEmbedding!.CosineDistance(queryVector))
            .Take(limit)
            .ToListAsync(ct);
    }
}
