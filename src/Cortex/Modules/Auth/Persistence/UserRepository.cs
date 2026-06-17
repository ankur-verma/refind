using Cortex.Modules.Auth.Entities;
using Cortex.Shared.Enums;
using Cortex.Database;
using Microsoft.EntityFrameworkCore;

namespace Cortex.Modules.Auth.Persistence;

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
