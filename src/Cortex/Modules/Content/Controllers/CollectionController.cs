using System;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Cortex.Database;
using Cortex.Modules.Content.Entities;
using Cortex.Modules.Content.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace Cortex.Modules.Content.Controllers;

[ApiController]
[Route("api/collections")]
[Authorize]
public class CollectionController : ControllerBase
{
    private readonly CortexDbContext _dbContext;
    private readonly ICollectionGenerator _collectionGenerator;

    public CollectionController(CortexDbContext dbContext, ICollectionGenerator collectionGenerator)
    {
        _dbContext = dbContext;
        _collectionGenerator = collectionGenerator;
    }

    [HttpGet]
    public async Task<IActionResult> GetCollections(CancellationToken ct)
    {
        var userId = GetUserId();
        var collections = await _dbContext.AutoCollections
            .Where(c => c.UserId == userId)
            .Select(c => new
            {
                c.Id,
                c.Name,
                c.Description,
                ItemCount = c.Memberships.Count,
                CreatedAt = c.CreatedAt
            })
            .ToListAsync(ct);

        return Ok(new { success = true, data = collections });
    }

    [HttpGet("{id}/items")]
    public async Task<IActionResult> GetCollectionItems(Guid id, [FromQuery] int page = 1, [FromQuery] int pageSize = 50, CancellationToken ct = default)
    {
        var userId = GetUserId();
        var collection = await _dbContext.AutoCollections
            .FirstOrDefaultAsync(c => c.Id == id && c.UserId == userId, ct);

        if (collection == null) return NotFound();

        var query = _dbContext.CollectionMemberships
            .Where(m => m.AutoCollectionId == id)
            .OrderByDescending(m => m.AddedAt)
            .Select(m => m.ContentItem);

        var totalCount = await query.CountAsync(ct);
        var items = await query
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .Select(c => new
            {
                c.Id,
                c.OriginalUrl,
                c.Title,
                c.HeroImageUrl,
                c.PlatformType,
                c.ConsumeTimeMins,
                c.CreatedAt
            })
            .ToListAsync(ct);

        return Ok(new
        {
            success = true,
            data = new
            {
                collection = new { collection.Id, collection.Name, collection.Description },
                items,
                pagination = new { page, pageSize, totalCount }
            }
        });
    }

    [HttpPost("generate")]
    public async Task<IActionResult> GenerateCollections(CancellationToken ct)
    {
        var userId = GetUserId();
        await _collectionGenerator.GenerateForUserAsync(userId, ct);
        return Ok(new { success = true, message = "Collection generation triggered successfully." });
    }

    private Guid GetUserId()
    {
        var userIdClaim = User.Claims.FirstOrDefault(c => c.Type == "id")?.Value;
        if (Guid.TryParse(userIdClaim, out var userId)) return userId;
        throw new UnauthorizedAccessException("Invalid User ID claim");
    }
}
