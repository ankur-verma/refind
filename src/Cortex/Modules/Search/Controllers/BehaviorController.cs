using System;
using System.Linq;
using System.Security.Claims;
using System.Threading;
using System.Threading.Tasks;
using Cortex.Database;
using Cortex.Infrastructure.Messaging;
using Cortex.Infrastructure.Settings;
using Cortex.Modules.Search.Entities;
using Cortex.Modules.Search.Messaging;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;

namespace Cortex.Modules.Search.Controllers;

public record TrackBehaviorRequest(Guid? ContentItemId, string EventType, string? MetadataJson);

[ApiController]
[Route("api/v1/behavior")]
[Authorize]
public class BehaviorController : ControllerBase
{
    private readonly CortexDbContext _dbContext;
    private readonly Cortex.Modules.Search.Services.IInterestEngine _interestEngine;

    public BehaviorController(
        CortexDbContext dbContext, 
        Cortex.Modules.Search.Services.IInterestEngine interestEngine)
    {
        _dbContext = dbContext;
        _interestEngine = interestEngine;
    }

    [HttpPost("events")]
    public async Task<IActionResult> TrackEvent([FromBody] TrackBehaviorRequest[] requests, CancellationToken ct)
    {
        var userId = GetUserId();
        
        foreach (var request in requests)
        {
            if (!Enum.TryParse<BehaviorEventType>(request.EventType, true, out var eventType))
            {
                continue;
            }

            try 
            {
                await _interestEngine.ProcessBehaviorEventAsync(userId, request.ContentItemId, eventType, request.MetadataJson, ct);
            }
            catch (Exception) 
            {
                // Silently swallow errors here so we don't crash bulk telemetry
            }
        }

        return Accepted(new { success = true, message = "Events processed synchronously." });
    }

    [HttpGet("interests")]
    public async Task<IActionResult> GetInterests(CancellationToken ct)
    {
        var userId = GetUserId();
        var interests = await _dbContext.UserInterests
            .Where(i => i.UserId == userId && !i.IsDeleted)
            .OrderByDescending(i => i.Score)
            .Select(i => new
            {
                i.Id,
                i.Category,
                i.Score,
                i.Trend,
                i.LastCalculated
            })
            .ToListAsync(ct);

        return Ok(new { success = true, data = interests });
    }

    private Guid GetUserId()
    {
        var userIdClaim = User.Claims.FirstOrDefault(c => c.Type == "id" || c.Type == ClaimTypes.NameIdentifier)?.Value;
        if (Guid.TryParse(userIdClaim, out var userId)) return userId;
        throw new UnauthorizedAccessException("Invalid User ID claim");
    }
}
