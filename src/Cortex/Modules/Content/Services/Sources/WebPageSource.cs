using Cortex.Modules.Content.Domain;
using Cortex.Shared.Enums;
using HtmlAgilityPack;
using Microsoft.Extensions.Logging;
using static Cortex.Modules.Content.Services.Sources.ContentExtractionHelpers;

namespace Cortex.Modules.Content.Services.Sources;

public class WebPageSource : IContentSource
{
    private readonly HttpClient _httpClient;
    private readonly ILogger<WebPageSource> _logger;

    public PlatformType PlatformType => PlatformType.Web;

    public WebPageSource(HttpClient httpClient, ILogger<WebPageSource> logger)
    {
        _httpClient = PrepareClient(httpClient);
        _logger = logger;
    }

    public bool CanHandle(string url)
    {
        return true; // Fallback
    }

    public async Task<ContentExtractionResult> ExtractAsync(string url, CancellationToken ct = default)
    {
        _logger.LogInformation("Processing Web URL: {Url}", url);

        using var response = await _httpClient.GetAsync(url, ct);
        response.EnsureSuccessStatusCode();
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

        // For generic web pages, we put everything in Transcript
        result.Transcript = ExtractMainText(document);
        result.Tags = ExtractTags(document, "web");

        return result;
    }
}
