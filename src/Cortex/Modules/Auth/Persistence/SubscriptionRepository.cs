using Cortex.Modules.Auth.Entities;
using Cortex.Database;
using Microsoft.EntityFrameworkCore;

namespace Cortex.Modules.Auth.Persistence;

public class SubscriptionRepository : ISubscriptionRepository
{
    private readonly CortexDbContext _context;
    public SubscriptionRepository(CortexDbContext context) => _context = context;

    public async Task<Subscription?> GetByUserIdAsync(Guid userId, CancellationToken ct = default)
        => await _context.Subscriptions.FirstOrDefaultAsync(x => x.UserId == userId && x.IsActive, ct);

    public async Task<Subscription> AddAsync(Subscription subscription, CancellationToken ct = default)
    {
        await _context.Subscriptions.AddAsync(subscription, ct);
        return subscription;
    }

    public Task UpdateAsync(Subscription subscription, CancellationToken ct = default)
    {
        _context.Subscriptions.Update(subscription);
        return Task.CompletedTask;
    }
}
