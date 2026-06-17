using Cortex.Modules.Auth.Entities;
using Cortex.Database;
using Microsoft.EntityFrameworkCore;

namespace Cortex.Modules.Auth.Persistence;

public class UserActivityLogRepository : IUserActivityLogRepository
{
    private readonly CortexDbContext _context;

    public UserActivityLogRepository(CortexDbContext context)
    {
        _context = context;
    }

    public async Task AddAsync(UserActivityLog log, CancellationToken ct = default)
    {
        await _context.UserActivityLogs.AddAsync(log, ct);
    }

    public async Task<List<UserActivityLog>> GetByUserIdAsync(Guid userId, CancellationToken ct = default)
    {
        return await _context.UserActivityLogs
            .Where(x => x.UserId == userId)
            .OrderByDescending(x => x.CreatedAt)
            .ToListAsync(ct);
    }
}
