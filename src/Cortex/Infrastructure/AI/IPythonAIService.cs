using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;

namespace Cortex.Infrastructure.AI;

public record VideoSegmentDto(int StartSeconds, int EndSeconds, string Title, string? Summary);
public record AutoCollectionSuggestionDto(string Name, string Description, string? IntentDescription);
public record RecommendedItemDto(Guid Id, string Title, string OriginalUrl, string? HeroImageUrl, int ConsumeTimeMins);

public interface IPythonAIService
{
    Task<List<VideoSegmentDto>> AnalyzeVideoAsync(Guid contentItemId, string? url, string? mp4FilePath, string? rawText, CancellationToken ct = default);
    Task<bool> UpdateUserProfileAsync(Guid userId, CancellationToken ct = default);
    Task<List<AutoCollectionSuggestionDto>> SuggestCollectionsAsync(Guid userId, CancellationToken ct = default);
    Task<List<RecommendedItemDto>> GetRecommendationsAsync(Guid userId, int limit = 5, CancellationToken ct = default);
}
