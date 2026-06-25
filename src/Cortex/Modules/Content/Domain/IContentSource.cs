using Cortex.Shared.Enums;

namespace Cortex.Modules.Content.Domain;

public interface IContentSource
{
    PlatformType PlatformType { get; }
    
    bool CanHandle(string url);
    
    Task<ContentExtractionResult> ExtractAsync(string url, CancellationToken ct = default);
}
