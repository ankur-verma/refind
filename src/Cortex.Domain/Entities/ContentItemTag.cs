namespace Cortex.Domain.Entities;

/// <summary>
/// Bridge entity for the M:M relationship between ContentItems and Tags.
/// Uses composite primary key (ContentItemId, TagId).
/// </summary>
public class ContentItemTag
{
    public Guid ContentItemId { get; set; }
    public Guid TagId { get; set; }

    // Navigation
    public ContentItem ContentItem { get; set; } = null!;
    public Tag Tag { get; set; } = null!;
}
