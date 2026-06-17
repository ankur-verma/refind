using System;

namespace Cortex.Modules.Search.Entities;

public class ChatMessage
{
    public Guid Id { get; set; } = Guid.NewGuid();
    public Guid ChatSessionId { get; set; }
    
    /// <summary>
    /// e.g. "User", "Assistant", "System"
    /// </summary>
    public string Role { get; set; } = string.Empty;
    public string Content { get; set; } = string.Empty;
    
    /// <summary>
    /// JSON serialized string of citations if the role is Assistant
    /// </summary>
    public string? SourcesJson { get; set; }
    
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

    // Navigation property
    public ChatSession Session { get; set; } = null!;
}
