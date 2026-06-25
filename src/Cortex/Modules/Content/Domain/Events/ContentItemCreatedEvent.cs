using Cortex.Shared;

namespace Cortex.Modules.Content.Events;

/// <summary>
/// Raised when a new content item is created and needs AI processing.
/// Triggers the async AI extraction pipeline via message queue.
/// </summary>
public class ContentItemCreatedEvent : IDomainEvent
{
    public Guid ContentItemId { get; }
    public Guid UserId { get; }
    public string OriginalUrl { get; }
    public DateTime OccurredAt { get; } = DateTime.UtcNow;

    public ContentItemCreatedEvent(Guid contentItemId, Guid userId, string originalUrl)
    {
        ContentItemId = contentItemId;
        UserId = userId;
        OriginalUrl = originalUrl;
    }
}
