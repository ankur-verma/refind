using Cortex.Application.DTOs.Common;
using Cortex.Application.DTOs.Drip;
using Cortex.Application.UseCases.Drip;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System.Security.Claims;

namespace Cortex.API.Controllers;

[ApiController]
[Route("api/v1/drip")]
[Authorize]
public class DripController : ControllerBase
{
    private readonly CreateDripTrackUseCase _createUseCase;
    private readonly AdvanceDripStepUseCase _advanceUseCase;
    private readonly GetDripTracksUseCase _getTracksUseCase;

    public DripController(
        CreateDripTrackUseCase createUseCase,
        AdvanceDripStepUseCase advanceUseCase,
        GetDripTracksUseCase getTracksUseCase)
    {
        _createUseCase = createUseCase;
        _advanceUseCase = advanceUseCase;
        _getTracksUseCase = getTracksUseCase;
    }

    private Guid GetUserId() => Guid.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier)!);

    /// <summary>
    /// Create a new Drip Track from a content item.
    /// </summary>
    [HttpPost]
    public async Task<IActionResult> CreateTrack([FromBody] CreateDripTrackRequest request, CancellationToken ct)
    {
        var result = await _createUseCase.ExecuteAsync(GetUserId(), request, ct);
        if (result.IsFailure) return BadRequest(ApiResponse<Guid>.Fail(result.Error));
        return CreatedAtAction(nameof(GetTracks), null, ApiResponse<Guid>.Ok(result.Value));
    }

    /// <summary>
    /// Get all drip tracks for the current user.
    /// </summary>
    [HttpGet("tracks")]
    public async Task<IActionResult> GetTracks(CancellationToken ct)
    {
        var tracks = await _getTracksUseCase.ExecuteAsync(GetUserId(), ct);
        var response = tracks.Select(t => new DripTrackResponse
        {
            Id = t.Id,
            ContentItemId = t.ContentItemId,
            ContentTitle = t.ContentItem?.Title ?? string.Empty,
            Status = t.Status,
            TotalDays = t.TotalDays,
            CurrentDay = t.CurrentDay,
            Steps = t.Steps.Select(s => new DripStepResponse
            {
                Id = s.Id,
                DayNumber = s.DayNumber,
                TaskTitle = s.TaskTitle,
                TaskDescription = s.TaskDescription,
                MediaStartSec = s.MediaStartSec,
                MediaEndSec = s.MediaEndSec,
                IsCompleted = s.IsCompleted,
                ScheduledFor = s.ScheduledFor
            }).ToList()
        }).ToList();
        return Ok(ApiResponse<List<DripTrackResponse>>.Ok(response));
    }

    /// <summary>
    /// Mark a drip step as completed.
    /// </summary>
    [HttpPatch("steps/{id:guid}")]
    public async Task<IActionResult> AdvanceStep(Guid id, CancellationToken ct)
    {
        var result = await _advanceUseCase.ExecuteAsync(GetUserId(), id, ct);
        if (result.IsFailure) return BadRequest(ApiResponse<object>.Fail(result.Error));
        return Ok(ApiResponse<string>.Ok("Step completed."));
    }
}
