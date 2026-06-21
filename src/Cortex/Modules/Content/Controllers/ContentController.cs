using Cortex.Shared;
using Cortex.Modules.Content.DTOs;
using Cortex.Modules.Content.UseCases;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System.Security.Claims;

namespace Cortex.Modules.Content.Controllers;

[ApiController]
[Route("api/v1/content")]
[Authorize]
public class ContentController : ControllerBase
{
    private readonly SaveContentUseCase _saveUseCase;
    private readonly GetContentFeedUseCase _feedUseCase;
    private readonly GetContentDetailUseCase _detailUseCase;
    private readonly DeleteContentUseCase _deleteUseCase;
    private readonly DeclutterContentUseCase _declutterUseCase;
    private readonly ReprocessContentUseCase _reprocessUseCase;

    public ContentController(
        SaveContentUseCase saveUseCase,
        GetContentFeedUseCase feedUseCase,
        GetContentDetailUseCase detailUseCase,
        DeleteContentUseCase deleteUseCase,
        DeclutterContentUseCase declutterUseCase,
        ReprocessContentUseCase reprocessUseCase)
    {
        _saveUseCase = saveUseCase;
        _feedUseCase = feedUseCase;
        _detailUseCase = detailUseCase;
        _deleteUseCase = deleteUseCase;
        _declutterUseCase = declutterUseCase;
        _reprocessUseCase = reprocessUseCase;
    }

    private Guid GetUserId() => Guid.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier)!);

    /// <summary>
    /// Save a URL for AI-powered ingestion and processing.
    /// </summary>
    [HttpPost]
    public async Task<IActionResult> SaveContent([FromBody] SaveContentRequest request, CancellationToken ct)
    {
        var result = await _saveUseCase.ExecuteAsync(GetUserId(), request, ct);
        if (result.IsFailure) 
        {
            if (result.Error == "DUPLICATE_URL")
            {
                return Ok(new { success = true, isDuplicate = true, message = "This URL has already been saved." });
            }
            return BadRequest(ApiResponse<Guid>.Fail(result.Error));
        }
        return CreatedAtAction(nameof(GetContentDetail), new { id = result.Value }, ApiResponse<Guid>.Ok(result.Value));
    }

    /// <summary>
    /// Get mood-filtered content feed for the Cognitive Dashboard.
    /// </summary>
    [HttpGet("feed")]
    public async Task<IActionResult> GetFeed([FromQuery] ContentFeedQuery query, CancellationToken ct)
    {
        var result = await _feedUseCase.ExecuteAsync(GetUserId(), query, ct);
        return Ok(new PagedResponse<ContentItemResponse>
        {
            Items = result.Items.Select(i => new ContentItemResponse
            {
                Id = i.Id,
                OriginalUrl = i.OriginalUrl,
                PlatformType = i.PlatformType,
                Title = i.Title,
                HeroImageUrl = i.HeroImageUrl,
                EnergyLevel = i.EnergyLevel,
                ConsumeTimeMins = i.ConsumeTimeMins,
                Status = i.Status,
                IsArchived = i.IsArchived,
                IsPinned = i.IsPinned,
                CreatedAt = i.CreatedAt,
                Tags = i.ContentItemTags?.Select(t => t.Tag.Name).ToList() ?? new()
            }).ToList(),
            PageNumber = result.PageNumber,
            PageSize = result.PageSize,
            TotalCount = result.TotalCount,
            TotalPages = result.TotalPages,
            HasPreviousPage = result.HasPreviousPage,
            HasNextPage = result.HasNextPage
        });
    }

    /// <summary>
    /// Get full content detail with AI-generated payload.
    /// </summary>
    [HttpGet("{id:guid}")]
    public async Task<IActionResult> GetContentDetail(Guid id, CancellationToken ct)
    {
        var item = await _detailUseCase.ExecuteAsync(id, ct);
        return Ok(ApiResponse<ContentDetailResponse>.Ok(new ContentDetailResponse
        {
            Id = item.Id,
            OriginalUrl = item.OriginalUrl,
            PlatformType = item.PlatformType,
            Title = item.Title,
            HeroImageUrl = item.HeroImageUrl,
            EnergyLevel = item.EnergyLevel,
            ConsumeTimeMins = item.ConsumeTimeMins,
            Status = item.Status,
            CreatedAt = item.CreatedAt,
            RawText = item.Payload?.RawText,
            QuickSparkSummary = item.Payload?.QuickSparkSummary,
            ActionItems = item.ActionItems?.Select(a => new ActionItemResponse
            {
                Id = a.Id,
                ItemType = a.ItemType,
                Description = a.Description,
                SequenceOrder = a.SequenceOrder,
                IsCompleted = a.IsCompleted
            }).ToList() ?? new(),
            VideoSegments = item.VideoSegments?.Select(v => new VideoSegmentResponse
            {
                Id = v.Id,
                StartSeconds = v.StartSeconds,
                EndSeconds = v.EndSeconds,
                Title = v.Title,
                Summary = v.Summary
            }).ToList() ?? new(),
            Tags = item.ContentItemTags?.Select(t => t.Tag.Name).ToList() ?? new()
        }));
    }

    /// <summary>
    /// Delete a content item (cascading).
    /// </summary>
    [HttpDelete("{id:guid}")]
    public async Task<IActionResult> Delete(Guid id, CancellationToken ct)
    {
        var result = await _deleteUseCase.ExecuteAsync(GetUserId(), id, ct);
        if (result.IsFailure) return NotFound(ApiResponse<object>.Fail(result.Error));
        return NoContent();
    }

    /// <summary>
    /// Declutter action: Pin, Archive, or Delete (Swipe to Clean).
    /// </summary>
    [HttpPost("{id:guid}/declutter")]
    public async Task<IActionResult> Declutter(Guid id, [FromBody] DeclutterActionRequest request, CancellationToken ct)
    {
        request.ContentItemId = id;
        var result = await _declutterUseCase.ExecuteAsync(GetUserId(), request, ct);
        if (result.IsFailure) return BadRequest(ApiResponse<object>.Fail(result.Error));
        return Ok(ApiResponse<string>.Ok("Action completed successfully."));
    }

    /// <summary>
    /// Reprocess an existing content item through the AI pipeline.
    /// </summary>
    [HttpPost("{id:guid}/reprocess")]
    public async Task<IActionResult> Reprocess(Guid id, CancellationToken ct)
    {
        var result = await _reprocessUseCase.ExecuteAsync(GetUserId(), id, ct);
        if (result.IsFailure) return NotFound(ApiResponse<object>.Fail(result.Error));
        return Ok(ApiResponse<string>.Ok("Reprocessing started."));
    }
}
