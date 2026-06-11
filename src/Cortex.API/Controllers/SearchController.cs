using Cortex.Application.DTOs.Common;
using Cortex.Application.DTOs.Search;
using Cortex.Application.UseCases.Search;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System.Security.Claims;

namespace Cortex.API.Controllers;

[ApiController]
[Route("api/v1/search")]
[Authorize]
public class SearchController : ControllerBase
{
    private readonly SemanticSearchUseCase _searchUseCase;
    public SearchController(SemanticSearchUseCase searchUseCase) => _searchUseCase = searchUseCase;

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
}
