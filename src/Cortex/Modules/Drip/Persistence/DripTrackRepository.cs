using Cortex.Modules.Drip.Entities;
using Cortex.Database;
using Microsoft.EntityFrameworkCore;

namespace Cortex.Modules.Drip.Persistence;

public class DripTrackRepository : IDripTrackRepository
{
    private readonly CortexDbContext _context;
    public DripTrackRepository(CortexDbContext context) => _context = context;

    public async Task<DripTrack?> GetByIdAsync(Guid id, CancellationToken ct = default)
        => await _context.DripTracks.FirstOrDefaultAsync(x => x.Id == id, ct);

    public async Task<DripTrack?> GetByIdWithStepsAsync(Guid id, CancellationToken ct = default)
        => await _context.DripTracks.Include(x => x.Steps.OrderBy(s => s.DayNumber)).FirstOrDefaultAsync(x => x.Id == id, ct);

    public async Task<List<DripTrack>> GetByUserIdAsync(Guid userId, CancellationToken ct = default)
        => await _context.DripTracks
            .Include(x => x.ContentItem)
            .Include(x => x.Steps.OrderBy(s => s.DayNumber))
            .Where(x => x.UserId == userId)
            .OrderByDescending(x => x.CreatedAt)
            .ToListAsync(ct);

    public async Task<DripTrack> AddAsync(DripTrack track, CancellationToken ct = default)
    {
        await _context.DripTracks.AddAsync(track, ct);
        return track;
    }

    public Task UpdateAsync(DripTrack track, CancellationToken ct = default)
    {
        _context.DripTracks.Update(track);
        return Task.CompletedTask;
    }
}
