using Cortex.SharedKernel;
using Cortex.SharedKernel.Enums;

namespace Cortex.Domain.Entities;

/// <summary>
/// Lightweight content metadata for the Cognitive Dashboard feeds.
/// Heavy AI data (RawText, Embeddings) is isolated in ContentPayload (1:1).
/// Aggregate root for the Content bounded context.
/// </summary>
public class ContentItem : BaseEntity, IAggregateRoot
{
    public Guid UserId { get; set; }
    public string OriginalUrl { get; set; } = string.Empty;
    public PlatformType PlatformType { get; set; }
    public string Title { get; set; } = string.Empty;
    public string? HeroImageUrl { get; set; }
    public EnergyLevel EnergyLevel { get; set; }
    public int ConsumeTimeMins { get; set; }
    public ContentStatus Status { get; set; } = ContentStatus.Processing;
    public bool IsArchived { get; set; }
    public bool IsPinned { get; set; }
    public DateTime? LastReviewedAt { get; set; }

    // Navigation properties
    public User User { get; set; } = null!;
    public ContentPayload? Payload { get; set; }
    public ICollection<ActionItem> ActionItems { get; set; } = new List<ActionItem>();
    public ICollection<ContentItemTag> ContentItemTags { get; set; } = new List<ContentItemTag>();
    public ICollection<DripTrack> DripTracks { get; set; } = new List<DripTrack>();
}
