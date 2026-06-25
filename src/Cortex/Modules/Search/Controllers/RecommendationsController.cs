using Cortex.Modules.Search.Entities;
using Cortex.Modules.Search.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Cortex.Modules.Search.Controllers;

[ApiController]
[Route("api/recommendations")]
public class RecommendationsController : ControllerBase
{
    private readonly IRecommendationService _recommendationService;

    public RecommendationsController(IRecommendationService recommendationService)
    {
        _recommendationService = recommendationService;
    }

    [HttpGet("personalized")]
    // [Authorize] - Assume user id is passed directly or from claims for simplicity
    public async Task<IActionResult> GetPersonalizedRecommendations([FromQuery] Guid userId, CancellationToken ct)
    {
        // In a real app, userId would be extracted from the JWT token via User.Identity
        if (userId == Guid.Empty)
        {
            return BadRequest("UserId is required.");
        }

        var recs = await _recommendationService.GetPersonalizedRecommendationsAsync(userId, ct);
        return Ok(recs);
    }
    
    [HttpPost("trigger")]
    public async Task<IActionResult> TriggerRecommendationsGeneration([FromQuery] Guid userId, CancellationToken ct)
    {
        // Endpoint to manually test recommendation generation
        if (userId == Guid.Empty)
        {
            return BadRequest("UserId is required.");
        }

        await _recommendationService.GenerateAndStoreRecommendationsAsync(userId, ct);
        return Ok(new { message = "Recommendations generated successfully." });
    }

    [HttpGet("graph")]
    public async Task<IActionResult> GetGraphRecommendations([FromServices] Cortex.Infrastructure.Graph.IGraphService graphService, [FromQuery] string userId, CancellationToken ct)
    {
        if (string.IsNullOrEmpty(userId))
        {
            return BadRequest("UserId is required.");
        }

        var recs = await graphService.GetRecommendationsForUserAsync(userId, ct);
        return Ok(new { recommendations = recs });
    }
}
