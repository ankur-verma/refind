using Cortex.SharedKernel;
using Cortex.SharedKernel.Enums;

namespace Cortex.Domain.Entities;

/// <summary>
/// AI-extracted tools or step-by-step instructions from content.
/// E.g., "Set layer blend mode to Multiply" or tool: "Figma".
/// </summary>
public class ActionItem : BaseEntity
{
    public Guid ContentItemId { get; set; }
    public ActionItemType ItemType { get; set; }
    public string Description { get; set; } = string.Empty;
    public int SequenceOrder { get; set; }
    public bool IsCompleted { get; set; }

    // Navigation
    public ContentItem ContentItem { get; set; } = null!;
}
