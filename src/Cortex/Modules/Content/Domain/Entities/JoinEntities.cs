using System;

namespace Cortex.Modules.Content.Entities;

public class ContentInsightEntity
{
    public Guid ContentInsightId { get; set; }
    public ContentInsight ContentInsight { get; set; } = null!;
    
    public Guid SemanticEntityId { get; set; }
    public SemanticEntity SemanticEntity { get; set; } = null!;
}

public class ContentInsightLocation
{
    public Guid ContentInsightId { get; set; }
    public ContentInsight ContentInsight { get; set; } = null!;
    
    public Guid LocationId { get; set; }
    public Location Location { get; set; } = null!;
}

public class ContentInsightProduct
{
    public Guid ContentInsightId { get; set; }
    public ContentInsight ContentInsight { get; set; } = null!;
    
    public Guid ProductId { get; set; }
    public Product Product { get; set; } = null!;
}

public class ContentInsightBrand
{
    public Guid ContentInsightId { get; set; }
    public ContentInsight ContentInsight { get; set; } = null!;
    
    public Guid BrandId { get; set; }
    public Brand Brand { get; set; } = null!;
}
