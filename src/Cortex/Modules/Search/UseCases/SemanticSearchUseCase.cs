using Cortex.Modules.Search.DTOs;
using Cortex.Modules.Auth.Persistence;
using Cortex.Modules.Content.Persistence;
using Cortex.Modules.Drip.Persistence;
using Cortex.Database;
using Cortex.Modules.Auth.Services;
using Cortex.Infrastructure.AI;
using Cortex.Infrastructure.Caching;
using Cortex.Infrastructure.Messaging;
using Cortex.Shared;
using Pgvector;

namespace Cortex.Modules.Search.UseCases;

/// <summary>
/// Semantic search: converts NL query → embedding → vector similarity search.
/// </summary>
public class SemanticSearchUseCase
{
    private readonly IAIExtractionService _aiService;
    private readonly IContentPayloadRepository _payloadRepo;
    private readonly IContentItemRepository _contentRepo;
    private readonly Cortex.Infrastructure.Settings.AIServiceSettings _aiSettings;

    public SemanticSearchUseCase(
        IAIExtractionService aiService,
        IContentPayloadRepository payloadRepo,
        IContentItemRepository contentRepo,
        Microsoft.Extensions.Options.IOptions<Cortex.Infrastructure.Settings.AIServiceSettings> aiOptions)
    {
        _aiService = aiService;
        _payloadRepo = payloadRepo;
        _contentRepo = contentRepo;
        _aiSettings = aiOptions.Value;
    }

    public async Task<Result<List<SearchResultResponse>>> ExecuteAsync(
        Guid userId, SemanticSearchRequest request, CancellationToken ct = default)
    {
        Guard.AgainstNullOrEmpty(request.Query, nameof(request.Query));

        // Generate embedding for the search query
        var queryEmbedding = await _aiService.GenerateEmbeddingAsync(request.Query, ct);
        var queryVector = new Vector(queryEmbedding);

        // Find similar content via vector search
        var matchResults = await _payloadRepo.SearchByVectorAsync(userId, queryVector, _aiSettings.ActiveProvider, request.Limit, ct);

        var results = new List<SearchResultResponse>();
        foreach (var match in matchResults)
        {
            var contentItem = await _contentRepo.GetByIdAsync(match.Payload.ContentItemId, ct);
            if (contentItem is null) continue;

            // Cosine distance of 0 means identical, 1 means orthogonal, 2 means opposite.
            // Similarity is typically 1 - distance (bounding to 0).
            var similarity = Math.Max(0, 1.0 - match.Distance);

            results.Add(new SearchResultResponse
            {
                ContentItemId = contentItem.Id,
                Title = contentItem.Title,
                QuickSparkSummary = match.Payload.QuickSparkSummary,
                HeroImageUrl = contentItem.HeroImageUrl,
                SimilarityScore = similarity
            });
        }

        return Result.Success(results);
    }
}
