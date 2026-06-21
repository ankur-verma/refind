using System;
using Cortex.Shared;
using Cortex.Modules.Auth.Entities;
using Cortex.Modules.Search.Entities;

namespace Cortex.Modules.Content.Entities;

public class AutoCollection : BaseEntity
{
    public Guid UserId { get; set; }
    public Guid? UserIntentId { get; set; }
    
    public string Name { get; set; } = string.Empty;
    public string Description { get; set; } = string.Empty;

    // Navigation
    public User User { get; set; } = null!;
    public UserIntent? UserIntent { get; set; }
}
