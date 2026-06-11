using Cortex.SharedKernel;
using Cortex.SharedKernel.Enums;

namespace Cortex.Domain.Entities;

/// <summary>
/// Subscription state isolated from the User entity.
/// Tracks billing lifecycle (start, end, active status).
/// </summary>
public class Subscription : BaseEntity
{
    public Guid UserId { get; set; }
    public SubscriptionTier Tier { get; set; } = SubscriptionTier.Free;
    public DateTime StartDate { get; set; } = DateTime.UtcNow;
    public DateTime? EndDate { get; set; }
    public bool IsActive { get; set; } = true;

    // Navigation
    public User User { get; set; } = null!;
}
