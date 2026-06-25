using Cortex.Modules.Content.Domain;
using Cortex.Shared.Enums;
using Microsoft.Extensions.Logging;
using HtmlAgilityPack;
using static Cortex.Modules.Content.Services.Sources.ContentExtractionHelpers;

namespace Cortex.Modules.Content.Services.Sources;

public class MapsSource : IContentSource
{
    private readonly HttpClient _httpClient;
    private readonly ILogger<MapsSource> _logger;

    public PlatformType PlatformType => PlatformType.Maps;

    public MapsSource(HttpClient httpClient, ILogger<MapsSource> logger)
    {
        _httpClient = PrepareClient(httpClient);
        _logger = logger;
    }

    public bool CanHandle(string url)
    {
        if (!Uri.TryCreate(url, UriKind.Absolute, out var uri)) return false;
        var host = uri.Host.ToLowerInvariant();
        return host.Contains("google.com/maps") || host.Contains("maps.apple.com") || host.Contains("openstreetmap.org");
    }

    public async Task<ContentExtractionResult> ExtractAsync(string url, CancellationToken ct = default)
    {
        _logger.LogInformation("Processing Maps URL: {Url}", url);

        // Simple extraction for maps. Usually requires specialized API for rich data.
        using var response = await _httpClient.GetAsync(url, ct);
        var html = await response.Content.ReadAsStringAsync(ct);

        var document = new HtmlDocument();
        document.LoadHtml(html);
        RemoveNoise(document);

        var result = new ContentExtractionResult();
        
        result.Title = FirstNonEmpty(
            ReadMeta(document, "property", "og:title"),
            ReadMeta(document, "name", "twitter:title"),
            CleanText(document.DocumentNode.SelectSingleNode("//title")?.InnerText),
            "Map Location");
            
        result.Description = FirstNonEmpty(
            ReadMeta(document, "name", "description"),
            ReadMeta(document, "property", "og:description"));
            
        var heroImage = FirstNonEmpty(
            ReadMeta(document, "property", "og:image"),
            ReadMeta(document, "name", "twitter:image"));
            
        if (!string.IsNullOrEmpty(heroImage)) result.Media.Add(heroImage);

        result.Transcript = ExtractMainText(document);
        result.Location = result.Title; // Map URLs title is usually the location
        result.Tags = ExtractTags(document, "map", "location", "place");

        return result;
    }
}
