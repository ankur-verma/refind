using Cortex.Modules.Content.Domain;
using Cortex.Shared.Enums;
using HtmlAgilityPack;
using Microsoft.Extensions.Logging;
using static Cortex.Modules.Content.Services.Sources.ContentExtractionHelpers;

namespace Cortex.Modules.Content.Services.Sources;

public class ArticleSource : IContentSource
{
    private readonly HttpClient _httpClient;
    private readonly ILogger<ArticleSource> _logger;

    public PlatformType PlatformType => PlatformType.Article;

    public ArticleSource(HttpClient httpClient, ILogger<ArticleSource> logger)
    {
        _httpClient = PrepareClient(httpClient);
        _logger = logger;
    }

    public bool CanHandle(string url)
    {
        if (!Uri.TryCreate(url, UriKind.Absolute, out var uri)) return false;
        var host = uri.Host.ToLowerInvariant();
        
        return host.Contains("medium.com") || host.Contains("dev.to") || host.Contains("substack.com") || 
               host.Contains("nytimes.com") || host.Contains("wsj.com") || host.Contains("bloomberg.com") ||
               host.Contains("theverge.com") || host.Contains("techcrunch.com") || host.Contains("wired.com") ||
               host.Contains("blog.");
    }

    public async Task<ContentExtractionResult> ExtractAsync(string url, CancellationToken ct = default)
    {
        _logger.LogInformation("Processing Article URL: {Url}", url);

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

        result.Author = FirstNonEmpty(
            ReadMeta(document, "name", "author"),
            ReadMeta(document, "property", "article:author"));

        // For Article, we place the main text in Transcript so it can be summarized
        result.Transcript = ExtractMainText(document);
        result.Tags = ExtractTags(document, "web", "article", "blog");

        return result;
    }
}
