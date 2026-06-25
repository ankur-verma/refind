using Cortex.Modules.Content.Domain;
using Cortex.Shared.Enums;
using Microsoft.Extensions.Logging;
using HtmlAgilityPack;
using static Cortex.Modules.Content.Services.Sources.ContentExtractionHelpers;

namespace Cortex.Modules.Content.Services.Sources;

public class ShoppingSource : IContentSource
{
    private readonly HttpClient _httpClient;
    private readonly ILogger<ShoppingSource> _logger;

    public PlatformType PlatformType => PlatformType.Shopping;

    public ShoppingSource(HttpClient httpClient, ILogger<ShoppingSource> logger)
    {
        _httpClient = PrepareClient(httpClient);
        _logger = logger;
    }

    public bool CanHandle(string url)
    {
        if (!Uri.TryCreate(url, UriKind.Absolute, out var uri)) return false;
        var host = uri.Host.ToLowerInvariant();
        return host.Contains("amazon.com") || host.Contains("ebay.com") || host.Contains("walmart.com") || host.Contains("target.com") || host.Contains("etsy.com");
    }

    public async Task<ContentExtractionResult> ExtractAsync(string url, CancellationToken ct = default)
    {
        _logger.LogInformation("Processing Shopping URL: {Url}", url);

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
            CleanText(document.DocumentNode.SelectSingleNode("//h1")?.InnerText),
            url);
            
        result.Description = FirstNonEmpty(
            ReadMeta(document, "name", "description"),
            ReadMeta(document, "property", "og:description"));
            
        var heroImage = FirstNonEmpty(
            ReadMeta(document, "property", "og:image"),
            ReadMeta(document, "name", "twitter:image"));
            
        if (!string.IsNullOrEmpty(heroImage)) result.Media.Add(heroImage);

        // Put page content into Transcript so it can be summarized
        result.Transcript = ExtractMainText(document);
        result.Tags = ExtractTags(document, "shopping", "product", "ecommerce");

        return result;
    }
}
