using Cortex.Database;
using Cortex.Shared;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System.Security.Claims;

namespace Cortex.Modules.Auth.Controllers;

public class UserStatsResponse
{
    public int MemoriesSaved { get; set; }
    public int TimeSavedMins { get; set; }
    public int DeepDives { get; set; }
    public int ActiveStreak { get; set; }
}

[ApiController]
[Route("api/v1/user/stats")]
[Authorize]
public class UserStatsController : ControllerBase
{
    private readonly CortexDbContext _dbContext;

    public UserStatsController(CortexDbContext dbContext)
    {
        _dbContext = dbContext;
    }

    private Guid GetUserId() => Guid.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier)!);

    [HttpGet]
    public async Task<IActionResult> GetUserStats(CancellationToken ct)
    {
        var userId = GetUserId();

        var memoriesSaved = await _dbContext.ContentItems
            .Where(c => c.UserId == userId && !c.IsDeleted)
            .CountAsync(ct);

        var timeSavedMins = await _dbContext.ContentItems
            .Where(c => c.UserId == userId && !c.IsDeleted)
            .SumAsync(c => c.ConsumeTimeMins, ct);

        var deepDives = await _dbContext.ChatSessions
            .Where(c => c.UserId == userId)
            .CountAsync(ct);

        var uniqueDays = await _dbContext.UserBehaviorEvents
            .Where(e => e.UserId == userId)
            .Select(e => e.CreatedAt.Date)
            .Distinct()
            .CountAsync(ct);

        var stats = new UserStatsResponse
        {
            MemoriesSaved = memoriesSaved,
            TimeSavedMins = timeSavedMins,
            DeepDives = deepDives,
            ActiveStreak = uniqueDays > 0 ? uniqueDays : 1 // just show at least 1 for the UI if they have no events yet but just logged in
        };

        return Ok(ApiResponse<UserStatsResponse>.Ok(stats));
    }
}
