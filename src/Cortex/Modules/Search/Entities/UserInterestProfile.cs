using System;
using Cortex.Shared;
using Cortex.Modules.Auth.Entities;

namespace Cortex.Modules.Search.Entities;

public class UserInterestProfile : BaseEntity
{
    public Guid UserId { get; set; }
    
    // Category (e.g. Travel, Shopping, Technology, Cooking, Hobbies, Career)
    public string Category { get; set; } = string.Empty;
    public int Score { get; set; } // 0-100
    
    // "Emerging", "Stable", "Declining", "Temporary"
    public string Trend { get; set; } = "Stable";
    public DateTime LastCalculated { get; set; } = DateTime.UtcNow;

    // Navigation
    public User User { get; set; } = null!;
}
