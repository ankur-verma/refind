using System;
using Cortex.Shared;
using Cortex.Modules.Auth.Entities;
using Cortex.Modules.Content.Entities;

namespace Cortex.Modules.Search.Entities;

public class UserInteraction : BaseEntity
{
    public Guid UserId { get; set; }
    public Guid? ContentItemId { get; set; }
    
    // "Save", "View", "Revisit", "Search", "Click", "Session"
    public string InteractionType { get; set; } = string.Empty;
    public int DurationSeconds { get; set; }
    public string? SearchQuery { get; set; }

    // Navigation
    public User User { get; set; } = null!;
    public ContentItem? ContentItem { get; set; }
}
