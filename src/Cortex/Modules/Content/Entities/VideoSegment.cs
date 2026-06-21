using System;
using Cortex.Shared;

namespace Cortex.Modules.Content.Entities;

public class VideoSegment : BaseEntity
{
    public Guid ContentItemId { get; set; }
    public int StartSeconds { get; set; }
    public int EndSeconds { get; set; }
    public string Title { get; set; } = string.Empty;
    public string? Summary { get; set; }

    // Navigation
    public ContentItem ContentItem { get; set; } = null!;
}
