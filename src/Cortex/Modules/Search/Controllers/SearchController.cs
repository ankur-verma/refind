using Cortex.Shared;
using Cortex.Modules.Search.DTOs;
using Cortex.Modules.Search.UseCases;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System.Security.Claims;
using System.Text.Json;

namespace Cortex.Modules.Search.Controllers;

[ApiController]
[Route("api/v1/search")]
[Authorize]
public class SearchController : ControllerBase
{
    private readonly SemanticSearchUseCase _searchUseCase;
    private readonly GetMindsetUseCase _getMindsetUseCase;
    private readonly RefreshMindsetUseCase _refreshMindsetUseCase;
    private readonly UpdateMindsetUseCase _updateMindsetUseCase;
    private readonly RAGQueryUseCase _ragUseCase;

    public SearchController(
        SemanticSearchUseCase searchUseCase,
        GetMindsetUseCase getMindsetUseCase,
        RefreshMindsetUseCase refreshMindsetUseCase,
        UpdateMindsetUseCase updateMindsetUseCase,
        RAGQueryUseCase ragUseCase)
    {
        _searchUseCase = searchUseCase;
        _getMindsetUseCase = getMindsetUseCase;
        _refreshMindsetUseCase = refreshMindsetUseCase;
        _updateMindsetUseCase = updateMindsetUseCase;
        _ragUseCase = ragUseCase;
    }

    /// <summary>
    /// Semantic natural language search across saved content.
    /// </summary>
    [HttpPost]
    public async Task<IActionResult> Search([FromBody] SemanticSearchRequest request, CancellationToken ct)
    {
        var userId = Guid.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier)!);
        var result = await _searchUseCase.ExecuteAsync(userId, request, ct);
        if (result.IsFailure) return BadRequest(ApiResponse<List<SearchResultResponse>>.Fail(result.Error));
        return Ok(ApiResponse<List<SearchResultResponse>>.Ok(result.Value));
    }

    /// <summary>
    /// Get the compiled user cognitive mindset profile.
    /// </summary>
    [HttpGet("mindset")]
    public async Task<IActionResult> GetMindset(CancellationToken ct)
    {
        var userId = Guid.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier)!);
        var result = await _getMindsetUseCase.ExecuteAsync(userId, ct);
        if (result.IsFailure) return BadRequest(ApiResponse<MindsetResponse>.Fail(result.Error));

        var m = result.Value;
        var focusAreas = JsonSerializer.Deserialize<List<string>>(m.FocusAreasJson) ?? new List<string>();
        
        return Ok(ApiResponse<MindsetResponse>.Ok(new MindsetResponse
        {
            Id = m.Id,
            UserId = m.UserId,
            FocusAreas = focusAreas,
            ConsumptionPreference = m.ConsumptionPreference,
            NarrativeSummary = m.NarrativeSummary,
            LastUpdated = m.LastUpdated
        }));
    }

    /// <summary>
    /// Force trigger a compilation of the user mindset profile.
    /// </summary>
    [HttpPost("mindset/refresh")]
    public async Task<IActionResult> RefreshMindset(CancellationToken ct)
    {
        var userId = Guid.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier)!);
        var result = await _refreshMindsetUseCase.ExecuteAsync(userId, ct);
        if (result.IsFailure) return BadRequest(ApiResponse<MindsetResponse>.Fail(result.Error));

        var m = result.Value;
        var focusAreas = JsonSerializer.Deserialize<List<string>>(m.FocusAreasJson) ?? new List<string>();
        
        return Ok(ApiResponse<MindsetResponse>.Ok(new MindsetResponse
        {
            Id = m.Id,
            UserId = m.UserId,
            FocusAreas = focusAreas,
            ConsumptionPreference = m.ConsumptionPreference,
            NarrativeSummary = m.NarrativeSummary,
            LastUpdated = m.LastUpdated
        }));
    }

    /// <summary>
    /// Manually update user cognitive mindset preferences.
    /// </summary>
    [HttpPut("mindset")]
    public async Task<IActionResult> UpdateMindset([FromBody] UpdateMindsetRequest request, CancellationToken ct)
    {
        var userId = Guid.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier)!);
        var result = await _updateMindsetUseCase.ExecuteAsync(userId, request, ct);
        if (result.IsFailure) return BadRequest(ApiResponse<MindsetResponse>.Fail(result.Error));

        var m = result.Value;
        var focusAreas = JsonSerializer.Deserialize<List<string>>(m.FocusAreasJson) ?? new List<string>();
        
        return Ok(ApiResponse<MindsetResponse>.Ok(new MindsetResponse
        {
            Id = m.Id,
            UserId = m.UserId,
            FocusAreas = focusAreas,
            ConsumptionPreference = m.ConsumptionPreference,
            NarrativeSummary = m.NarrativeSummary,
            LastUpdated = m.LastUpdated
        }));
    }

    /// <summary>
    /// Query the cognitive database via RAG.
    /// </summary>
    [HttpPost("rag")]
    public async Task<IActionResult> RAGQuery([FromBody] RAGQueryRequest request, CancellationToken ct)
    {
        var userId = Guid.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier)!);
        var result = await _ragUseCase.ExecuteAsync(userId, request, ct);
        if (result.IsFailure) return BadRequest(ApiResponse<RAGQueryResponse>.Fail(result.Error));
        return Ok(ApiResponse<RAGQueryResponse>.Ok(result.Value));
    }
}
