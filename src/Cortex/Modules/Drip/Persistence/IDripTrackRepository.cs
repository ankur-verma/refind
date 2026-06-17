using Cortex.Modules.Drip.Entities;

namespace Cortex.Modules.Drip.Persistence;

public interface IDripTrackRepository
{
    Task<DripTrack?> GetByIdAsync(Guid id, CancellationToken ct = default);
    Task<DripTrack?> GetByIdWithStepsAsync(Guid id, CancellationToken ct = default);
    Task<List<DripTrack>> GetByUserIdAsync(Guid userId, CancellationToken ct = default);
    Task<DripTrack> AddAsync(DripTrack track, CancellationToken ct = default);
    Task UpdateAsync(DripTrack track, CancellationToken ct = default);
}
