using Cortex.Shared;
using Cortex.Modules.Search.UseCases;
using Cortex.Infrastructure.AI;
using Cortex.Database;
using Cortex.Modules.Search.Entities;
using Cortex.Modules.Content.Entities;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Security.Claims;
using System.Threading;
using System.Threading.Tasks;

namespace Cortex.Modules.Search.Controllers;

public record AddOrUpdateCategoryRequest(string Category, int Score, string Trend);
public record AddOrUpdateIntentRequest(Guid? Id, string GoalDescription, double Confidence, bool IsResolved);
public record AddOrUpdateCollectionRequest(Guid? Id, string Name, string Description, Guid? UserIntentId);

[ApiController]
[Route("api/v1/interaction")]
[Authorize]
public class InteractionController : ControllerBase
{
    private readonly TrackInteractionUseCase _trackUseCase;
    private readonly GetInterestProfileUseCase _getProfileUseCase;
    private readonly IPythonAIService _pythonAiService;
    private readonly CortexDbContext _context;

    public InteractionController(
        TrackInteractionUseCase trackUseCase,
        GetInterestProfileUseCase getProfileUseCase,
        IPythonAIService pythonAiService,
        CortexDbContext context)
    {
        _trackUseCase = trackUseCase;
        _getProfileUseCase = getProfileUseCase;
        _pythonAiService = pythonAiService;
        _context = context;
    }

    /// <summary>
    /// Log a user telemetry interaction (Save, View, Revisit, Search, Click, Session).
    /// </summary>
    [HttpPost("track")]
    public async Task<IActionResult> Track([FromBody] TrackInteractionRequest request, CancellationToken ct)
    {
        var userId = Guid.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier)!);
        var result = await _trackUseCase.ExecuteAsync(userId, request, ct);
        if (result.IsFailure) return BadRequest(ApiResponse<bool>.Fail(result.Error));
        return Ok(ApiResponse<bool>.Ok(result.Value));
    }

    /// <summary>
    /// Retrieve the user's AI interest profiles, goal intents, and smart collections.
    /// </summary>
    [HttpGet("profile")]
    public async Task<IActionResult> GetProfile(CancellationToken ct)
    {
        var userId = Guid.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier)!);
        var result = await _getProfileUseCase.ExecuteAsync(userId, ct);
        if (result.IsFailure) return BadRequest(ApiResponse<UserInterestProfileResponse>.Fail(result.Error));
        return Ok(ApiResponse<UserInterestProfileResponse>.Ok(result.Value));
    }

    /// <summary>
    /// Force refresh the user's interest profile synchronously via Python AI service and return the updated profile.
    /// </summary>
    [HttpPost("profile/refresh")]
    public async Task<IActionResult> RefreshProfile(CancellationToken ct)
    {
        var userId = Guid.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier)!);
        var success = await _pythonAiService.UpdateUserProfileAsync(userId, ct);
        if (!success)
        {
            return BadRequest(ApiResponse<UserInterestProfileResponse>.Fail("Failed to refresh profile via Python AI service."));
        }

        var result = await _getProfileUseCase.ExecuteAsync(userId, ct);
        if (result.IsFailure) return BadRequest(ApiResponse<UserInterestProfileResponse>.Fail(result.Error));
        return Ok(ApiResponse<UserInterestProfileResponse>.Ok(result.Value));
    }

    /// <summary>
    /// Fetch personalized AI recommendations based on user interest profiling.
    /// </summary>
    [HttpGet("recommend")]
    public async Task<IActionResult> GetRecommendations([FromQuery] int limit = 5, CancellationToken ct = default)
    {
        var userId = Guid.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier)!);
        var recommendations = await _pythonAiService.GetRecommendationsAsync(userId, limit, ct);
        return Ok(ApiResponse<List<RecommendedItemDto>>.Ok(recommendations));
    }

    /// <summary>
    /// Add or update an interest category manually.
    /// </summary>
    [HttpPost("profile/category")]
    public async Task<IActionResult> AddOrUpdateCategory([FromBody] AddOrUpdateCategoryRequest request, CancellationToken ct)
    {
        var userId = Guid.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier)!);
        var normalizedCat = request.Category.Trim();
        
        var profile = await _context.UserInterests
            .FirstOrDefaultAsync(p => p.UserId == userId && p.Category.ToLower() == normalizedCat.ToLower(), ct);
            
        if (profile is null)
        {
            profile = new UserInterest
            {
                UserId = userId,
                Category = normalizedCat,
                Score = request.Score,
                Trend = request.Trend,
                LastCalculated = DateTime.UtcNow,
                CreatedAt = DateTime.UtcNow,
                IsDeleted = false
            };
            await _context.UserInterests.AddAsync(profile, ct);
        }
        else
        {
            profile.Score = request.Score;
            profile.Trend = request.Trend;
            profile.LastCalculated = DateTime.UtcNow;
            profile.IsDeleted = false;
            _context.UserInterests.Update(profile);
        }
        
        await _context.SaveChangesAsync(ct);
        return Ok(ApiResponse<bool>.Ok(true));
    }

    /// <summary>
    /// Soft delete an interest category manually.
    /// </summary>
    [HttpDelete("profile/category/{category}")]
    public async Task<IActionResult> DeleteCategory(string category, CancellationToken ct)
    {
        var userId = Guid.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier)!);
        var profile = await _context.UserInterests
            .FirstOrDefaultAsync(p => p.UserId == userId && p.Category.ToLower() == category.ToLower() && !p.IsDeleted, ct);
            
        if (profile is not null)
        {
            profile.IsDeleted = true;
            _context.UserInterests.Update(profile);
            await _context.SaveChangesAsync(ct);
        }
        
        return Ok(ApiResponse<bool>.Ok(true));
    }

    /// <summary>
    /// Add or update a goal intent manually.
    /// </summary>
    [HttpPost("profile/intent")]
    public async Task<IActionResult> AddOrUpdateIntent([FromBody] AddOrUpdateIntentRequest request, CancellationToken ct)
    {
        var userId = Guid.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier)!);
        
        UserIntent? intent = null;
        if (request.Id.HasValue && request.Id.Value != Guid.Empty)
        {
            intent = await _context.UserIntents
                .FirstOrDefaultAsync(i => i.Id == request.Id.Value && i.UserId == userId, ct);
        }
        
        if (intent is null)
        {
            intent = new UserIntent
            {
                UserId = userId,
                GoalDescription = request.GoalDescription,
                Confidence = request.Confidence,
                IsResolved = request.IsResolved,
                CreatedAt = DateTime.UtcNow,
                IsDeleted = false
            };
            await _context.UserIntents.AddAsync(intent, ct);
        }
        else
        {
            intent.GoalDescription = request.GoalDescription;
            intent.Confidence = request.Confidence;
            intent.IsResolved = request.IsResolved;
            intent.IsDeleted = false;
            _context.UserIntents.Update(intent);
        }
        
        await _context.SaveChangesAsync(ct);
        return Ok(ApiResponse<bool>.Ok(true));
    }

    /// <summary>
    /// Soft delete a goal intent manually.
    /// </summary>
    [HttpDelete("profile/intent/{id}")]
    public async Task<IActionResult> DeleteIntent(Guid id, CancellationToken ct)
    {
        var userId = Guid.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier)!);
        var intent = await _context.UserIntents
            .FirstOrDefaultAsync(i => i.Id == id && i.UserId == userId && !i.IsDeleted, ct);
            
        if (intent is not null)
        {
            intent.IsDeleted = true;
            _context.UserIntents.Update(intent);
            await _context.SaveChangesAsync(ct);
        }
        
        return Ok(ApiResponse<bool>.Ok(true));
    }

    /// <summary>
    /// Add or update an AI/Smart collection manually.
    /// </summary>
    [HttpPost("profile/collection")]
    public async Task<IActionResult> AddOrUpdateCollection([FromBody] AddOrUpdateCollectionRequest request, CancellationToken ct)
    {
        var userId = Guid.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier)!);
        
        AutoCollection? collection = null;
        if (request.Id.HasValue && request.Id.Value != Guid.Empty)
        {
            collection = await _context.AutoCollections
                .FirstOrDefaultAsync(c => c.Id == request.Id.Value && c.UserId == userId, ct);
        }
        
        if (collection is null)
        {
            collection = new AutoCollection
            {
                UserId = userId,
                Name = request.Name,
                Description = request.Description,
                UserIntentId = request.UserIntentId,
                CreatedAt = DateTime.UtcNow,
                IsDeleted = false
            };
            await _context.AutoCollections.AddAsync(collection, ct);
        }
        else
        {
            collection.Name = request.Name;
            collection.Description = request.Description;
            collection.UserIntentId = request.UserIntentId;
            collection.IsDeleted = false;
            _context.AutoCollections.Update(collection);
        }
        
        await _context.SaveChangesAsync(ct);
        return Ok(ApiResponse<bool>.Ok(true));
    }

    /// <summary>
    /// Soft delete an AI/Smart collection manually.
    /// </summary>
    [HttpDelete("profile/collection/{id}")]
    public async Task<IActionResult> DeleteCollection(Guid id, CancellationToken ct)
    {
        var userId = Guid.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier)!);
        var collection = await _context.AutoCollections
            .FirstOrDefaultAsync(c => c.Id == id && c.UserId == userId && !c.IsDeleted, ct);
            
        if (collection is not null)
        {
            collection.IsDeleted = true;
            _context.AutoCollections.Update(collection);
            await _context.SaveChangesAsync(ct);
        }
        
        return Ok(ApiResponse<bool>.Ok(true));
    }
}
