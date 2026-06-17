using Cortex.Modules.Auth.Entities;

namespace Cortex.Modules.Auth.Persistence;

public interface IUserActivityLogRepository
{
    Task AddAsync(UserActivityLog log, CancellationToken ct = default);
    Task<List<UserActivityLog>> GetByUserIdAsync(Guid userId, CancellationToken ct = default);
}
