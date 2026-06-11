using Cortex.SharedKernel;
using Cortex.SharedKernel.Enums;

namespace Cortex.Domain.Entities;

/// <summary>
/// Multi-day learning journey container for the Drip System.
/// Manages a user's progression through micro-dosed content.
/// </summary>
public class DripTrack : BaseEntity, IAggregateRoot
{
    public Guid UserId { get; set; }
    public Guid ContentItemId { get; set; }
    public DripTrackStatus Status { get; set; } = DripTrackStatus.Active;
    public int TotalDays { get; set; }
    public int CurrentDay { get; set; } = 1;

    // Navigation
    public User User { get; set; } = null!;
    public ContentItem ContentItem { get; set; } = null!;
    public ICollection<DripStep> Steps { get; set; } = new List<DripStep>();
}
