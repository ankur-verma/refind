using Microsoft.EntityFrameworkCore;

namespace Cortex.Database;

public class UnitOfWork : IUnitOfWork
{
    private readonly CortexDbContext _context;
    public UnitOfWork(CortexDbContext context) => _context = context;

    public async Task<int> SaveChangesAsync(CancellationToken ct = default)
        => await _context.SaveChangesAsync(ct);

    public void Dispose() => _context.Dispose();
}
