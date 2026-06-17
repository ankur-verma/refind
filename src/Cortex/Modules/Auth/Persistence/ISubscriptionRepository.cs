using Cortex.Modules.Auth.Entities;

namespace Cortex.Modules.Auth.Persistence;

public interface ISubscriptionRepository
{
    Task<Subscription?> GetByUserIdAsync(Guid userId, CancellationToken ct = default);
    Task<Subscription> AddAsync(Subscription subscription, CancellationToken ct = default);
    Task UpdateAsync(Subscription subscription, CancellationToken ct = default);
}
