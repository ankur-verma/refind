using System;
using Cortex.Shared;
using Cortex.Modules.Auth.Entities;

namespace Cortex.Modules.Search.Entities;

public class UserIntent : BaseEntity
{
    public Guid UserId { get; set; }
    
    // Inferred goal (e.g. Planning Bali Trip, Buying a Drone, Cooking Recipes)
    public string GoalDescription { get; set; } = string.Empty;
    public double Confidence { get; set; } // 0.0 - 1.0
    public bool IsResolved { get; set; }

    // Navigation
    public User User { get; set; } = null!;
}
