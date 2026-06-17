using Cortex.Shared;
using Cortex.Shared.Enums;

namespace Cortex.Modules.Auth.Entities;

/// <summary>
/// Multi-provider authentication record.
/// Allows a user to link Google, Apple, and local auth to the same account.
/// </summary>
public class UserIdentity : BaseEntity
{
    public Guid UserId { get; set; }
    public AuthProvider AuthProvider { get; set; }
    public string ProviderId { get; set; } = string.Empty;

    // Navigation
    public User User { get; set; } = null!;
}
