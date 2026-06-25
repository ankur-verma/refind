using System;
using Cortex.Shared;
using Cortex.Modules.Auth.Entities;
using Cortex.Modules.Content.Entities;

namespace Cortex.Modules.Search.Entities;

public enum BehaviorEventType
{
    Viewed,
    Opened,
    Saved,
    Shared,
    Searched,
    Ignored,
    Revisited,
    FeedViewed,
    Clicked,
    Bookmarked,
    CollectionOpened,
    SessionEnded
}

public class UserBehaviorEvent : BaseEntity
{
    public Guid UserId { get; set; }
    public Guid? ContentItemId { get; set; }
    public BehaviorEventType EventType { get; set; }
    public string? MetadataJson { get; set; } // For storing search queries, context, etc.

    // Navigation
    public User User { get; set; } = null!;
    public ContentItem? ContentItem { get; set; }
}
