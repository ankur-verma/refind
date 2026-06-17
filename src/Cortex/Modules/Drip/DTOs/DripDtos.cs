using Cortex.Shared.Enums;

namespace Cortex.Modules.Drip.DTOs;

public class CreateDripTrackRequest
{
    public Guid ContentItemId { get; set; }
    public int TotalDays { get; set; } = 5;
}

public class DripTrackResponse
{
    public Guid Id { get; set; }
    public Guid ContentItemId { get; set; }
    public string ContentTitle { get; set; } = string.Empty;
    public DripTrackStatus Status { get; set; }
    public int TotalDays { get; set; }
    public int CurrentDay { get; set; }
    public List<DripStepResponse> Steps { get; set; } = new();
}

public class DripStepResponse
{
    public Guid Id { get; set; }
    public int DayNumber { get; set; }
    public string TaskTitle { get; set; } = string.Empty;
    public string TaskDescription { get; set; } = string.Empty;
    public int? MediaStartSec { get; set; }
    public int? MediaEndSec { get; set; }
    public bool IsCompleted { get; set; }
    public DateTime? ScheduledFor { get; set; }
}
