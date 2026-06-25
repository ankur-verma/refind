using System;

namespace Cortex.Modules.Search.Messaging;

public class BehaviorEventMessage
{
    public Guid UserId { get; set; }
    public Guid? ContentItemId { get; set; }
    public string EventType { get; set; } = string.Empty;
    public string? MetadataJson { get; set; }
}
