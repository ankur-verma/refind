using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;

using System.Text.Json.Serialization;

namespace Cortex.Infrastructure.AI;

public class VideoSegmentDto
{
    [JsonPropertyName("start_seconds")]
    public int StartSeconds { get; set; }
    
    [JsonPropertyName("end_seconds")]
    public int EndSeconds { get; set; }
    
    [JsonPropertyName("title")]
    public string Title { get; set; } = string.Empty;
    
    [JsonPropertyName("summary")]
    public string? Summary { get; set; }
    
    [JsonPropertyName("segment_type")]
    public string? SegmentType { get; set; }
    
    [JsonPropertyName("topics")]
    public List<string>? Topics { get; set; }
    
    [JsonPropertyName("products")]
    public List<string>? Products { get; set; }
    
    [JsonPropertyName("locations")]
    public List<string>? Locations { get; set; }
    
    [JsonPropertyName("restaurants")]
    public List<string>? Restaurants { get; set; }
    
    [JsonPropertyName("tips")]
    public List<string>? Tips { get; set; }
    
    [JsonPropertyName("prices")]
    public List<string>? Prices { get; set; }
    
    [JsonPropertyName("recommendations")]
    public List<string>? Recommendations { get; set; }
}
public record AutoCollectionSuggestionDto(string Name, string Description, string? IntentDescription);
public record RecommendedItemDto(Guid Id, string Title, string OriginalUrl, string? HeroImageUrl, int ConsumeTimeMins);

public class DynamicReelItemDto
{
    [JsonPropertyName("Id")]
    public Guid Id { get; set; }
    
    [JsonPropertyName("Title")]
    public string Title { get; set; } = string.Empty;
    
    [JsonPropertyName("Summary")]
    public string? Summary { get; set; }
    
    [JsonPropertyName("StartSeconds")]
    public int StartSeconds { get; set; }
    
    [JsonPropertyName("EndSeconds")]
    public int EndSeconds { get; set; }
    
    [JsonPropertyName("ContentItemId")]
    public Guid ContentItemId { get; set; }
    
    [JsonPropertyName("OriginalUrl")]
    public string OriginalUrl { get; set; } = string.Empty;
    
    [JsonPropertyName("PlatformType")]
    public string? PlatformType { get; set; }
}

public class SynthesizeVideoResponse
{
    [JsonPropertyName("success")]
    public bool Success { get; set; }
    
    [JsonPropertyName("script")]
    public string? Script { get; set; }
    
    [JsonPropertyName("theme")]
    public string? Theme { get; set; }
    
    [JsonPropertyName("final_video_url")]
    public string? FinalVideoUrl { get; set; }
    
    [JsonPropertyName("broll_url")]
    public string? BRollUrl { get; set; }
    
    [JsonPropertyName("error")]
    public string? Error { get; set; }
}
public class SynthesizeStartResponse
{
    [JsonPropertyName("task_id")]
    public string? TaskId { get; set; }
}

public class SynthesizeStatusResponse
{
    [JsonPropertyName("status")]
    public string? Status { get; set; }

    [JsonPropertyName("progress")]
    public int Progress { get; set; }

    [JsonPropertyName("result")]
    public SynthesizeVideoResponse? Result { get; set; }
}

public interface IPythonAIService
{
    Task<List<VideoSegmentDto>> AnalyzeVideoAsync(Guid contentItemId, string? url, string? mp4FilePath, string? rawText, CancellationToken ct = default);
    Task<bool> UpdateUserProfileAsync(Guid userId, CancellationToken ct = default);
    Task<List<AutoCollectionSuggestionDto>> SuggestCollectionsAsync(Guid userId, CancellationToken ct = default);
    Task<List<RecommendedItemDto>> GetRecommendationsAsync(Guid userId, int limit = 5, CancellationToken ct = default);
    Task<HybridRecommendationResponse?> GetHybridRecommendationsAsync(Guid userId, CancellationToken ct = default);
    Task<bool> GenerateUserEmbeddingAsync(Guid userId, CancellationToken ct = default);
    Task<bool> TriggerClusteringAsync(CancellationToken ct = default);
    Task<List<DynamicReelItemDto>> GetDynamicReelAsync(Guid userId, int limit = 5, CancellationToken ct = default);
    Task<bool> EmbedVideoSegmentsAsync(CancellationToken ct = default);
    Task<SynthesizeStartResponse> SynthesizeAIVideoAsync(List<string> transcripts, CancellationToken ct = default);
    Task<SynthesizeStatusResponse?> GetSynthesisStatusAsync(string taskId, CancellationToken ct = default);
}
