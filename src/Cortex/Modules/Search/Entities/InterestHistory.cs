using System;
using Cortex.Shared;

namespace Cortex.Modules.Search.Entities;

public class InterestHistory : BaseEntity
{
    public Guid UserInterestId { get; set; }
    public int PreviousScore { get; set; }
    public int NewScore { get; set; }
    public int Delta { get; set; }
    public string Reason { get; set; } = string.Empty;

    // Navigation
    public UserInterest UserInterest { get; set; } = null!;
}
