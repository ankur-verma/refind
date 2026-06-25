using Cortex.Shared;

namespace Cortex.Modules.Content.Entities;

public enum EntityType
{
    General,
    Person,
    Event
}

public class SemanticEntity : BaseEntity
{
    public string Name { get; set; } = string.Empty;
    public EntityType Type { get; set; }
    
    public ICollection<ContentInsightEntity> Insights { get; set; } = new List<ContentInsightEntity>();
}

public class Location : BaseEntity
{
    public string Name { get; set; } = string.Empty;
    
    public ICollection<ContentInsightLocation> Insights { get; set; } = new List<ContentInsightLocation>();
}

public class Product : BaseEntity
{
    public string Name { get; set; } = string.Empty;
    
    public ICollection<ContentInsightProduct> Insights { get; set; } = new List<ContentInsightProduct>();
}

public class Brand : BaseEntity
{
    public string Name { get; set; } = string.Empty;
    
    public ICollection<ContentInsightBrand> Insights { get; set; } = new List<ContentInsightBrand>();
}
