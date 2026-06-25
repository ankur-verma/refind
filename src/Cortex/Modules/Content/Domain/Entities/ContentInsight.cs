using Cortex.Shared;

namespace Cortex.Modules.Content.Entities;

public class ContentInsight : BaseEntity
{
    public Guid ContentItemId { get; set; }
    
    public string Category { get; set; } = string.Empty;
    public string SubCategory { get; set; } = string.Empty;
    public string Intent { get; set; } = string.Empty;
    public string Sentiment { get; set; } = string.Empty;
    
    /// <summary>
    /// Stored as JSON string array in PostgreSQL
    /// </summary>
    public List<string> Topics { get; set; } = new();

    public ContentItem ContentItem { get; set; } = null!;
    
    public ICollection<ContentInsightEntity> Entities { get; set; } = new List<ContentInsightEntity>();
    public ICollection<ContentInsightLocation> Locations { get; set; } = new List<ContentInsightLocation>();
    public ICollection<ContentInsightProduct> Products { get; set; } = new List<ContentInsightProduct>();
    public ICollection<ContentInsightBrand> Brands { get; set; } = new List<ContentInsightBrand>();
}
