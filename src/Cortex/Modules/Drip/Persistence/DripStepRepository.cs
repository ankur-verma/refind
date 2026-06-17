using Cortex.Modules.Drip.Entities;
using Cortex.Database;
using Microsoft.EntityFrameworkCore;

namespace Cortex.Modules.Drip.Persistence;

public class DripStepRepository : IDripStepRepository
{
    private readonly CortexDbContext _context;
    public DripStepRepository(CortexDbContext context) => _context = context;

    public async Task<DripStep?> GetByIdAsync(Guid id, CancellationToken ct = default)
        => await _context.DripSteps.FirstOrDefaultAsync(x => x.Id == id, ct);

    public async Task AddRangeAsync(IEnumerable<DripStep> steps, CancellationToken ct = default)
        => await _context.DripSteps.AddRangeAsync(steps, ct);

    public Task UpdateAsync(DripStep step, CancellationToken ct = default)
    {
        _context.DripSteps.Update(step);
        return Task.CompletedTask;
    }
}
