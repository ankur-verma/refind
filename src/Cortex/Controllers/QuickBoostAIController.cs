using Microsoft.AspNetCore.Mvc;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Cortex.Models;
using Cortex.Modules.Search.UseCases;
using Cortex.Infrastructure.AI;
using Cortex.Database;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Configuration;
using Microsoft.AspNetCore.Authorization;
using Cortex.Shared;
using Cortex.Modules.Search.DTOs;

namespace Cortex.Controllers
{
    [ApiController]
    [Route("api/v1/quickboost")] // Base route for QuickBoost services
    [Authorize]
    public class QuickBoostAIController : ControllerBase
    {
        private readonly IPythonAIService _aiService;
        private readonly CortexDbContext _context;
        private readonly ILogger<QuickBoostAIController> _logger;
        private readonly SemanticSearchUseCase _semanticSearch;
        
        public QuickBoostAIController(IPythonAIService aiService, CortexDbContext context, ILogger<QuickBoostAIController> logger, SemanticSearchUseCase semanticSearch)
        {
            _aiService = aiService;
            _context = context;
            _logger = logger;
            _semanticSearch = semanticSearch;
        }

        // GET api/v1/quickboost/ai-recs?userId=...
        [HttpGet("ai-recs")]
        [AllowAnonymous]
        public async Task<IActionResult> GetAIRecommendations([FromQuery] Guid userId, CancellationToken ct)
        {
            try 
            {
                // 1. Fetch recommended content using Semantic Profiling from Python Service
                var recommendations = await _aiService.GetRecommendationsAsync(userId, 10, ct);
                
                if (recommendations == null || !recommendations.Any())
                {
                    return Ok(ApiResponse<List<QuickBoostClip>>.Ok(GetDemoClips()));
                }

                // 2. Fetch those specific content items from the Database to extract VideoSegments
                var recommendedIds = recommendations.Select(r => r.Id).ToList();
                
                var contentItems = await _context.ContentItems
                    .Include(c => c.VideoSegments)
                    .Where(c => recommendedIds.Contains(c.Id) && c.VideoSegments.Any())
                    .ToListAsync(ct);

                var generatedClips = new List<QuickBoostClip>();

                foreach (var content in contentItems)
                {
                    foreach (var segment in content.VideoSegments.Take(2)) // Take max 2 segments per video to keep reel diverse
                    {
                        var clip = new QuickBoostClip(
                            VideoUrl: content.OriginalUrl,
                            StartSec: segment.StartSeconds,
                            EndSec: segment.EndSeconds,
                            Title: segment.Title,
                            Thumbnail: content.HeroImageUrl ?? "https://images.unsplash.com/photo-1611162617474-5b21e879e113",
                            Emotion: "Engaging" // Mock emotion or derive from intent
                        );
                        generatedClips.Add(clip);
                    }
                }

                if (!generatedClips.Any())
                {
                    return Ok(ApiResponse<List<QuickBoostClip>>.Ok(GetDemoClips()));
                }

                return Ok(ApiResponse<List<QuickBoostClip>>.Ok(generatedClips));
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to generate AI QuickBoost clips.");
                return Ok(ApiResponse<List<QuickBoostClip>>.Ok(GetDemoClips())); // Graceful degradation
            }
        }

        // GET api/v1/quickboost/watch-plan?topic=...
        [HttpGet("watch-plan")]
        [AllowAnonymous]
        public async Task<IActionResult> GetWatchPlan([FromQuery] Guid userId, [FromQuery] string topic, CancellationToken ct)
        {
            try 
            {
                if (string.IsNullOrWhiteSpace(topic))
                {
                    return BadRequest(ApiResponse<List<QuickBoostClip>>.Fail("Topic is required to generate a watch plan."));
                }

                // 1. Semantic Search for the topic to find relevant content
                var searchResults = await _semanticSearch.ExecuteAsync(userId, new SemanticSearchRequest { Query = topic, Limit = 5 }, ct);
                
                if (searchResults.IsFailure || !searchResults.Value.Any())
                {
                    return Ok(ApiResponse<List<QuickBoostClip>>.Ok(GetDemoClips()));
                }

                // 2. Fetch ContentItems + VideoSegments
                var contentIds = searchResults.Value.Select(r => r.ContentItemId).ToList();
                var contentItems = await _context.ContentItems
                    .Include(c => c.VideoSegments)
                    .Where(c => contentIds.Contains(c.Id) && c.VideoSegments.Any())
                    .ToListAsync(ct);

                var generatedClips = new List<QuickBoostClip>();

                // 3. Extract diverse segments (1 per video to ensure variety) to stitch into a Reel
                foreach (var content in contentItems)
                {
                    var segment = content.VideoSegments.OrderBy(s => Guid.NewGuid()).FirstOrDefault();
                    if (segment != null)
                    {
                        var clip = new QuickBoostClip(
                            VideoUrl: content.OriginalUrl,
                            StartSec: segment.StartSeconds,
                            EndSec: segment.EndSeconds,
                            Title: segment.Title,
                            Thumbnail: content.HeroImageUrl ?? "https://images.unsplash.com/photo-1611162617474-5b21e879e113",
                            Emotion: "Curated" 
                        );
                        generatedClips.Add(clip);
                    }
                }

                // If not enough clips found, pad with demo clips
                if (!generatedClips.Any())
                {
                    return Ok(ApiResponse<List<QuickBoostClip>>.Ok(GetDemoClips()));
                }

                // Shuffle the final reel to make it dynamic
                var shuffledReel = generatedClips.OrderBy(c => Guid.NewGuid()).ToList();

                return Ok(ApiResponse<List<QuickBoostClip>>.Ok(shuffledReel));
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to generate AI Watch Plan clips for topic {Topic}", topic);
                return Ok(ApiResponse<List<QuickBoostClip>>.Ok(GetDemoClips()));
            }
        }

        private static List<QuickBoostClip> GetDemoClips()
        {
            return new List<QuickBoostClip>
            {
                new QuickBoostClip(
                    VideoUrl: "https://www.youtube.com/embed/dQw4w9WgXcQ",
                    StartSec: 30,
                    EndSec: 45,
                    Title: "Sample QuickBoost Clip",
                    Thumbnail: "https://img.youtube.com/vi/dQw4w9WgXcQ/hqdefault.jpg",
                    Emotion: "happy"
                ),
                new QuickBoostClip(
                    VideoUrl: "https://www.youtube.com/embed/9bZkp7q19f0",
                    StartSec: 15,
                    EndSec: 30,
                    Title: "Energetic Highlight",
                    Thumbnail: "https://img.youtube.com/vi/9bZkp7q19f0/hqdefault.jpg",
                    Emotion: "inspirational"
                )
            };
        }
    }
}
