using System;
using Cortex.Shared;
using Pgvector;

namespace Cortex.Modules.Content.Entities;

public class VideoSegment : BaseEntity
{
    public Guid ContentItemId { get; set; }
    public int StartSeconds { get; set; }
    public int EndSeconds { get; set; }
    public string Title { get; set; } = string.Empty;
    public string? Summary { get; set; }
    
    public string SegmentType { get; set; } = "General"; // e.g., "Restaurant Review", "Hotel Tour"
    public VideoSegmentMetadata Metadata { get; set; } = new();
    
    public Vector? SegmentEmbedding { get; set; }

    // Navigation
    public ContentItem ContentItem { get; set; } = null!;
}

public class VideoSegmentMetadata 
{
    public List<string> Topics { get; set; } = new();
    public List<string> Products { get; set; } = new();
    public List<string> Locations { get; set; } = new();
    public List<string> Restaurants { get; set; } = new();
    public List<string> Tips { get; set; } = new();
    public List<string> Prices { get; set; } = new();
    public List<string> Recommendations { get; set; } = new();
}
