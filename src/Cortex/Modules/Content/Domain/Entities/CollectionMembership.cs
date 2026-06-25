using System;

namespace Cortex.Modules.Content.Entities;

public class CollectionMembership
{
    public Guid AutoCollectionId { get; set; }
    public AutoCollection AutoCollection { get; set; } = null!;

    public Guid ContentItemId { get; set; }
    public ContentItem ContentItem { get; set; } = null!;

    public DateTime AddedAt { get; set; } = DateTime.UtcNow;
}
