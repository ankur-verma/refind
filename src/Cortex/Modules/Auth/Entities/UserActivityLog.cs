using Cortex.Shared;

namespace Cortex.Modules.Auth.Entities;

/// <summary>
/// Audit and activity log recorded on a per-user basis.
/// </summary>
public class UserActivityLog : BaseEntity
{
    public Guid UserId { get; set; }
    public string Action { get; set; } = string.Empty;
    public string? Details { get; set; }
    public string? IpAddress { get; set; }

    // Navigation
    public User User { get; set; } = null!;
}
