using Cortex.Shared.Enums;

namespace Cortex.Modules.Content.Services;

/// <summary>
/// Factory Pattern: Creates the appropriate content processor based on platform type.
/// Single Responsibility — each processor handles one platform.
/// Open/Closed — new platforms = new processor + update factory.
/// </summary>
public interface IContentProcessorFactory
{
    IContentProcessor CreateProcessor(PlatformType platformType);
}

/// <summary>
/// Common interface that all platform-specific content processors implement.
/// Liskov Substitution — any processor can be used interchangeably.
/// </summary>
public interface IContentProcessor
{
    Task<ContentProcessingResult> ProcessAsync(string url, CancellationToken ct = default);
}

/// <summary>
/// Result of content processing — extracted raw text, metadata, and hero image.
/// </summary>
public class ContentProcessingResult
{
    public string Title { get; set; } = string.Empty;
    public string RawText { get; set; } = string.Empty;
    public string? HeroImageUrl { get; set; }
    public int EstimatedConsumeTimeMins { get; set; }
    public EnergyLevel SuggestedEnergyLevel { get; set; }
    public List<string> SuggestedTags { get; set; } = new();
}
