using Cortex.Modules.Content.Services;
using Cortex.Shared.Enums;
using Microsoft.Extensions.DependencyInjection;

namespace Cortex.Modules.Content.Services;

/// <summary>
/// ★ FACTORY PATTERN: Creates the appropriate content processor based on PlatformType.
/// 
/// SOLID Principles Applied:
/// - Single Responsibility: Factory only creates processors, doesn't process content itself.
/// - Open/Closed: Add new platform support by creating a new processor class + updating the switch.
/// - Liskov Substitution: All processors implement IContentProcessor and are fully interchangeable.
/// - Dependency Inversion: Depends on IServiceProvider abstraction, not concrete types.
/// </summary>
public class ContentProcessorFactory : IContentProcessorFactory
{
    private readonly IServiceProvider _serviceProvider;

    public ContentProcessorFactory(IServiceProvider serviceProvider)
    {
        _serviceProvider = serviceProvider;
    }

    public IContentProcessor CreateProcessor(PlatformType platformType)
    {
        return platformType switch
        {
            PlatformType.YouTube => _serviceProvider.GetRequiredService<YouTubeContentProcessor>(),
            PlatformType.Web => _serviceProvider.GetRequiredService<WebPageContentProcessor>(),
            PlatformType.Instagram => _serviceProvider.GetRequiredService<InstagramContentProcessor>(),
            PlatformType.PDF => _serviceProvider.GetRequiredService<PdfContentProcessor>(),
            PlatformType.TikTok => _serviceProvider.GetRequiredService<TikTokContentProcessor>(),
            PlatformType.Video => _serviceProvider.GetRequiredService<VideoContentProcessor>(),
            _ => throw new NotSupportedException($"Platform type '{platformType}' is not supported.")
        };
    }
}
