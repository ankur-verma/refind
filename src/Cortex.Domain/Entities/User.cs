using Cortex.SharedKernel;

namespace Cortex.Domain.Entities;

/// <summary>
/// Core user identity. Stripped of provider-specific data for normalization.
/// Aggregate root for the Identity bounded context.
/// </summary>
public class User : AuditableEntity, IAggregateRoot
{
    public string Email { get; set; } = string.Empty;
    public string? PasswordHash { get; set; }
    public string? RefreshTokenHash { get; set; }
    public DateTime? RefreshTokenExpiresAt { get; set; }
    public string? PushToken { get; set; }
    public DateTime? LastActiveAt { get; set; }

    // Navigation properties
    public ICollection<UserIdentity> Identities { get; set; } = new List<UserIdentity>();
    public Subscription? Subscription { get; set; }
    public ICollection<ContentItem> ContentItems { get; set; } = new List<ContentItem>();
    public ICollection<DripTrack> DripTracks { get; set; } = new List<DripTrack>();
}
