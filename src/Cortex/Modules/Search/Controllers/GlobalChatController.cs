using System;
using System.Threading;
using System.Threading.Tasks;
using Cortex.Modules.Search.DTOs;
using Cortex.Modules.Search.UseCases;
using Cortex.Shared;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Cortex.Modules.Search.Controllers;

[ApiController]
[Route("api/v1/search/chats")]
[Authorize]
public class GlobalChatController : ControllerBase
{
    private readonly GlobalChatUseCase _chatUseCase;

    public GlobalChatController(GlobalChatUseCase chatUseCase)
    {
        _chatUseCase = chatUseCase;
    }

    private Guid GetUserId() => Guid.Parse(User.FindFirst(System.Security.Claims.ClaimTypes.NameIdentifier)?.Value ?? Guid.Empty.ToString());

    [HttpGet]
    public async Task<IActionResult> GetSessions(CancellationToken ct)
    {
        var result = await _chatUseCase.GetSessionsAsync(GetUserId(), ct);
        if (result.IsFailure) return BadRequest(ApiResponse<object>.Fail(result.Error));
        return Ok(ApiResponse<object>.Ok(result.Value));
    }

    [HttpPost]
    public async Task<IActionResult> CreateSession([FromBody] CreateChatSessionRequest request, CancellationToken ct)
    {
        var result = await _chatUseCase.CreateSessionAsync(GetUserId(), request, ct);
        if (result.IsFailure) return BadRequest(ApiResponse<object>.Fail(result.Error));
        return Ok(ApiResponse<object>.Ok(result.Value));
    }

    [HttpDelete("{id}")]
    public async Task<IActionResult> DeleteSession(Guid id, CancellationToken ct)
    {
        var result = await _chatUseCase.DeleteSessionAsync(GetUserId(), id, ct);
        if (result.IsFailure) return BadRequest(ApiResponse<object>.Fail(result.Error));
        return Ok(ApiResponse<object>.Ok(null));
    }

    [HttpGet("{id}/messages")]
    public async Task<IActionResult> GetMessages(Guid id, CancellationToken ct)
    {
        var result = await _chatUseCase.GetMessagesAsync(GetUserId(), id, ct);
        if (result.IsFailure) return BadRequest(ApiResponse<object>.Fail(result.Error));
        return Ok(ApiResponse<object>.Ok(result.Value));
    }

    [HttpPost("{id}/messages")]
    public async Task<IActionResult> SendMessage(Guid id, [FromBody] SendMessageRequest request, CancellationToken ct)
    {
        var result = await _chatUseCase.SendMessageAsync(GetUserId(), id, request, ct);
        if (result.IsFailure) return BadRequest(ApiResponse<object>.Fail(result.Error));
        return Ok(ApiResponse<object>.Ok(result.Value));
    }
}
