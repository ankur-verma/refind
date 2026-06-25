using System.Text.Json.Serialization;
using Cortex.Shared.Enums;

namespace Cortex.Modules.Content.DTOs;

public class SaveContentRequest
{
    public string Url { get; set; } = string.Empty;

    public SaveContentRequest() {}
    public SaveContentRequest(string url) { Url = url; }
}

public class BulkSaveContentRequest
{
    public List<string> Urls { get; set; } = new();
}

public class ContentItemResponse
{
    public Guid Id { get; set; }
    public string OriginalUrl { get; set; } = string.Empty;
    public PlatformType PlatformType { get; set; }
    public string Title { get; set; } = string.Empty;
    public string? HeroImageUrl { get; set; }
    public EnergyLevel EnergyLevel { get; set; }
    public int ConsumeTimeMins { get; set; }
    public ContentStatus Status { get; set; }
    public bool IsArchived { get; set; }
    public bool IsPinned { get; set; }
    public DateTime CreatedAt { get; set; }
    public List<string> Tags { get; set; } = new();
}

public class ContentDetailResponse : ContentItemResponse
{
    public string? RawText { get; set; }
    public string? QuickSparkSummary { get; set; }
    public List<ActionItemResponse> ActionItems { get; set; } = new();
    public List<VideoSegmentResponse> VideoSegments { get; set; } = new();
}

public class VideoSegmentResponse
{
    public Guid Id { get; set; }
    public int StartSeconds { get; set; }
    public int EndSeconds { get; set; }
    public string Title { get; set; } = string.Empty;
    public string? Summary { get; set; }
    public string SegmentType { get; set; } = string.Empty;
    public VideoSegmentMetadataResponse Metadata { get; set; } = new();
}

public class VideoSegmentMetadataResponse
{
    public List<string> Topics { get; set; } = new();
    public List<string> Products { get; set; } = new();
    public List<string> Locations { get; set; } = new();
    public List<string> Restaurants { get; set; } = new();
    public List<string> Tips { get; set; } = new();
    public List<string> Prices { get; set; } = new();
    public List<string> Recommendations { get; set; } = new();
}

// ----------------------------------------------------
// Internal & Integration DTOs
// ----------------------------------------------------

public class VideoAnalysisResultDto
{
    public List<VideoSegmentAnalysisDto> Segments { get; set; } = new();
}

public class VideoSegmentAnalysisDto
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

public class ActionItemResponse
{
    public Guid Id { get; set; }
    public ActionItemType ItemType { get; set; }
    public string Description { get; set; } = string.Empty;
    public int SequenceOrder { get; set; }
    public bool IsCompleted { get; set; }
}

public class ContentFeedQuery
{
    public EnergyLevel? EnergyLevel { get; set; }
    public PlatformType? PlatformType { get; set; }
    public int Page { get; set; } = 1;
    public int PageSize { get; set; } = 20;
}

public class DeclutterActionRequest
{
    public Guid ContentItemId { get; set; }
    public DeclutterAction Action { get; set; }
}

public enum DeclutterAction
{
    Pin,
    Archive,
    Delete
}
