using Cortex.Modules.Drip.Entities;

namespace Cortex.Modules.Drip.Persistence;

public interface IDripStepRepository
{
    Task<DripStep?> GetByIdAsync(Guid id, CancellationToken ct = default);
    Task AddRangeAsync(IEnumerable<DripStep> steps, CancellationToken ct = default);
    Task UpdateAsync(DripStep step, CancellationToken ct = default);
}
