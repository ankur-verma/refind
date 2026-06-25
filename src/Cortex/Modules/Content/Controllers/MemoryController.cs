using Cortex.Database;
using Cortex.Modules.Content.Entities;
using Cortex.Shared;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System.Security.Claims;
using System.Text.Json;
using System.Net.Http;

namespace Cortex.Modules.Content.Controllers;

[ApiController]
[Route("api/v1/memory")]
[Authorize]
public class MemoryController : ControllerBase
{
    private readonly CortexDbContext _dbContext;
    private readonly IHttpClientFactory _httpClientFactory;

    public MemoryController(CortexDbContext dbContext, IHttpClientFactory httpClientFactory)
    {
        _dbContext = dbContext;
        _httpClientFactory = httpClientFactory;
    }

    [HttpGet("timeline")]
    public async Task<IActionResult> GetTimeline(CancellationToken ct)
    {
        var userId = Guid.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier)!);

        var savesCount = await _dbContext.ContentItems.CountAsync(c => c.UserId == userId, ct);
        if (savesCount < 10)
        {
            return Ok(IntelligenceResponse<object>.InsufficientData("insufficient_data"));
        }

        var events = await _dbContext.MemoryTimelineEvents
            .Where(e => e.UserId == userId)
            .OrderByDescending(e => e.CreatedAt)
            .ToListAsync(ct);

        if (!events.Any())
        {
            return Ok(IntelligenceResponse<object>.InsufficientData("insufficient_data"));
        }

        var generatedFrom = events.Select(e => e.Id.ToString()).ToList();
        return Ok(IntelligenceResponse<object>.Success(events, 0.90, savesCount, generatedFrom));
    }

    [HttpGet("recommendations")]
    public async Task<IActionResult> GetRecommendations(CancellationToken ct)
    {
        var userId = Guid.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier)!);

        var savesCount = await _dbContext.ContentItems.CountAsync(c => c.UserId == userId, ct);
        if (savesCount < 10)
        {
            return Ok(IntelligenceResponse<object>.InsufficientData("insufficient_data"));
        }

        var recommendations = await _dbContext.MemoryRecommendations
            .Include(r => r.RelatedContent)
            .Include(r => r.RelatedCollection)
            .Where(r => r.UserId == userId && !r.IsDismissed)
            .OrderByDescending(r => r.Score)
            .Take(5)
            .ToListAsync(ct);

        if (!recommendations.Any())
        {
            return Ok(IntelligenceResponse<object>.InsufficientData("insufficient_data"));
        }

        var result = recommendations.Select(r => new
        {
            Id = r.Id,
            Type = r.Type.ToString(),
            Title = r.Title,
            Reason = r.Reason,
            Content = r.RelatedContent != null ? new { r.RelatedContent.Id, r.RelatedContent.Title, ThumbnailUrl = r.RelatedContent.HeroImageUrl } : null,
            Collection = r.RelatedCollection != null ? new { r.RelatedCollection.Id, r.RelatedCollection.Title } : null
        }).ToList();

        var generatedFrom = recommendations.Select(r => r.Id.ToString()).ToList();
        var confidence = recommendations.First().Score;
        return Ok(IntelligenceResponse<object>.Success(result, confidence, savesCount, generatedFrom));
    }

    [HttpPost("timeline/generate")]
    public async Task<IActionResult> GenerateTimeline(CancellationToken ct)
    {
        var userId = Guid.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier)!);
        var recentItems = await _dbContext.ContentItems
            .Where(c => c.UserId == userId)
            .OrderByDescending(c => c.CreatedAt)
            .Take(100)
            .ToListAsync(ct);

        var payload = new
        {
            userId = userId.ToString(),
            recentSaves = recentItems.Select(i => new { id = i.Id.ToString(), title = i.Title, category = i.PlatformType.ToString(), tags = new List<string>() })
        };

        var client = _httpClientFactory.CreateClient();
        var aiServiceUrl = Environment.GetEnvironmentVariable("PERSONALIZATION_SERVICE_URL") ?? "http://localhost:8001";
        
        var response = await client.PostAsJsonAsync($"{aiServiceUrl}/api/v1/personalization/memory/infer", payload, ct);
        if (!response.IsSuccessStatusCode) return BadRequest(ApiResponse<object>.Fail("AI service failed"));

        var result = await response.Content.ReadFromJsonAsync<JsonElement>(cancellationToken: ct);
        
        var generated = new List<MemoryTimelineEvent>();
        if (result.TryGetProperty("timelineEvents", out var events))
        {
            foreach(var e in events.EnumerateArray())
            {
                var month = e.GetProperty("monthYear").GetString() ?? "";
                if (await _dbContext.MemoryTimelineEvents.AnyAsync(x => x.UserId == userId && x.MonthYear == month, ct)) continue;

                var newEvent = new MemoryTimelineEvent
                {
                    UserId = userId,
                    MonthYear = month,
                    Theme = e.GetProperty("theme").GetString() ?? "",
                    Summary = e.GetProperty("summary").GetString() ?? "",
                    ItemCount = 10 // Mock derived count
                };
                
                _dbContext.MemoryTimelineEvents.Add(newEvent);
                generated.Add(newEvent);
            }
            await _dbContext.SaveChangesAsync(ct);
        }

        return Ok(ApiResponse<object>.Ok(generated));
    }
}
