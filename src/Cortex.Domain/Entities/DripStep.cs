using Cortex.SharedKernel;

namespace Cortex.Domain.Entities;

/// <summary>
/// Individual daily micro-task within a DripTrack.
/// Contains the AI-generated bite-sized instruction and optional video segment timestamps.
/// </summary>
public class DripStep : BaseEntity
{
    public Guid DripTrackId { get; set; }
    public int DayNumber { get; set; }
    public string TaskTitle { get; set; } = string.Empty;
    public string TaskDescription { get; set; } = string.Empty;
    public int? MediaStartSec { get; set; }
    public int? MediaEndSec { get; set; }
    public bool IsCompleted { get; set; }
    public DateTime? ScheduledFor { get; set; }

    // Navigation
    public DripTrack DripTrack { get; set; } = null!;
}
