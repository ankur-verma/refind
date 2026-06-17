using Cortex.Shared.Enums;

namespace Cortex.Modules.Content.DTOs;

public class SaveContentRequest
{
    public string Url { get; set; } = string.Empty;
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
