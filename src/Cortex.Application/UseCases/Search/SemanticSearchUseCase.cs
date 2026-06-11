using Cortex.Application.DTOs.Search;
using Cortex.Application.Interfaces.Repositories;
using Cortex.Application.Interfaces.Services;
using Cortex.SharedKernel;
using Pgvector;

namespace Cortex.Application.UseCases.Search;

/// <summary>
/// Semantic search: converts NL query → embedding → vector similarity search.
/// </summary>
public class SemanticSearchUseCase
{
    private readonly IAIExtractionService _aiService;
    private readonly IContentPayloadRepository _payloadRepo;
    private readonly IContentItemRepository _contentRepo;

    public SemanticSearchUseCase(
        IAIExtractionService aiService,
        IContentPayloadRepository payloadRepo,
        IContentItemRepository contentRepo)
    {
        _aiService = aiService;
        _payloadRepo = payloadRepo;
        _contentRepo = contentRepo;
    }

    public async Task<Result<List<SearchResultResponse>>> ExecuteAsync(
        Guid userId, SemanticSearchRequest request, CancellationToken ct = default)
    {
        Guard.AgainstNullOrEmpty(request.Query, nameof(request.Query));

        // Generate embedding for the search query
        var queryEmbedding = await _aiService.GenerateEmbeddingAsync(request.Query, ct);
        var queryVector = new Vector(queryEmbedding);

        // Find similar content via vector search
        var payloads = await _payloadRepo.SearchByVectorAsync(userId, queryVector, request.Limit, ct);

        var results = new List<SearchResultResponse>();
        foreach (var payload in payloads)
        {
            var contentItem = await _contentRepo.GetByIdAsync(payload.ContentItemId, ct);
            if (contentItem is null) continue;

            results.Add(new SearchResultResponse
            {
                ContentItemId = contentItem.Id,
                Title = contentItem.Title,
                QuickSparkSummary = payload.QuickSparkSummary,
                HeroImageUrl = contentItem.HeroImageUrl
            });
        }

        return Result.Success(results);
    }
}
