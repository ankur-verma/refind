using Cortex.Shared;

namespace Cortex.Modules.Content.Entities;

/// <summary>
/// Taxonomy tag for content classification.
/// Tags can be AI-generated or manually created by users.
/// </summary>
public class Tag : BaseEntity
{
    public string Name { get; set; } = string.Empty;
    public bool IsAIGenerated { get; set; }

    // Navigation
    public ICollection<ContentItemTag> ContentItemTags { get; set; } = new List<ContentItemTag>();
}
