using System;
using Cortex.Shared;
using Cortex.Modules.Auth.Entities;

namespace Cortex.Modules.Content.Entities;

public enum MemoryEventType
{
    TimelineEvent,
    Recommendation,
    ForgottenContent,
    RevisitPrompt
}

public class MemoryTimelineEvent : BaseEntity
{
    public Guid UserId { get; set; }
    
    public string MonthYear { get; set; } = string.Empty; // e.g. "January 2026"
    public string Theme { get; set; } = string.Empty; // e.g. "Travel Planning"
    public string Summary { get; set; } = string.Empty;
    public int ItemCount { get; set; }
    
    public User User { get; set; } = null!;
}

public class MemoryRecommendation : BaseEntity
{
    public Guid UserId { get; set; }
    public Guid? RelatedContentId { get; set; }
    public Guid? RelatedCollectionId { get; set; }
    
    public MemoryEventType Type { get; set; }
    public string Title { get; set; } = string.Empty;
    public string Reason { get; set; } = string.Empty;
    public double Score { get; set; }
    
    public bool IsDismissed { get; set; } = false;
    
    public User User { get; set; } = null!;
    public ContentItem? RelatedContent { get; set; }
    public SmartCollection? RelatedCollection { get; set; }
}
