using System;
using Cortex.Modules.Auth.Entities;
using Cortex.Shared;

namespace Cortex.Modules.Content.Entities;

public enum MemoryType
{
    LocationBased,
    TimeBased,
    IntentBased,
    General
}

public class ProactiveMemory : BaseEntity
{
    public Guid UserId { get; set; }
    
    public MemoryType MemoryType { get; set; }
    
    public string Title { get; set; } = string.Empty;
    public string Message { get; set; } = string.Empty;
    public string? ActionUrl { get; set; }
    
    public bool IsRead { get; set; } = false;
    public DateTime? ReadAt { get; set; }
    
    public User User { get; set; } = null!;
}
