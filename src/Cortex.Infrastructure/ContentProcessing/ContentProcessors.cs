using Cortex.Application.Interfaces.Factories;
using Cortex.SharedKernel.Enums;
using HtmlAgilityPack;
using Microsoft.Extensions.Logging;
using System.Net;
using System.Text;
using System.Text.Json;
using System.Text.RegularExpressions;
using System.Xml.Linq;
using static Cortex.Infrastructure.ContentProcessing.ContentProcessorHelpers;

namespace Cortex.Infrastructure.ContentProcessing;

public class YouTubeContentProcessor : IContentProcessor
{
    private readonly HttpClient _httpClient;
    private readonly ILogger<YouTubeContentProcessor> _logger;

    public YouTubeContentProcessor(HttpClient httpClient, ILogger<YouTubeContentProcessor> logger)
    {
        _httpClient = PrepareClient(httpClient);
        _logger = logger;
    }

    public async Task<ContentProcessingResult> ProcessAsync(string url, CancellationToken ct = default)
    {
        _logger.LogInformation("Processing YouTube URL: {Url}", url);

        var metadata = await ReadYouTubeMetadataAsync(url, ct);
        var transcript = await TryReadYouTubeTranscriptAsync(url, ct);
        var rawText = CleanText(string.Join(". ", new[] { metadata.Title, metadata.Author, metadata.Description, transcript }
            .Where(value => !string.IsNullOrWhiteSpace(value))));

        return new ContentProcessingResult
        {
            Title = metadata.Title ?? "YouTube video",
            RawText = rawText,
            HeroImageUrl = metadata.ThumbnailUrl,
            EstimatedConsumeTimeMins = EstimateMinutes(rawText, fallback: 12),
            SuggestedEnergyLevel = EnergyLevel.DeepDive,
            SuggestedTags = new List<string> { "video", "youtube" }
        };
    }

    private async Task<YouTubeMetadata> ReadYouTubeMetadataAsync(string url, CancellationToken ct)
    {
        try
        {
            var oembedUrl = $"https://www.youtube.com/oembed?url={Uri.EscapeDataString(url)}&format=json";
            using var response = await _httpClient.GetAsync(oembedUrl, ct);
            if (response.IsSuccessStatusCode)
            {
                using var document = JsonDocument.Parse(await response.Content.ReadAsStringAsync(ct));
                var root = document.RootElement;
                return new YouTubeMetadata(
                    GetJsonString(root, "title"),
                    GetJsonString(root, "author_name"),
                    null,
                    GetJsonString(root, "thumbnail_url"));
            }
        }
        catch (Exception ex)
        {
            _logger.LogDebug(ex, "YouTube oEmbed metadata extraction failed for {Url}", url);
        }

        var htmlMetadata = await ReadHtmlMetadataAsync(_httpClient, url, defaultTitle: "YouTube video", ct);
        return new YouTubeMetadata(htmlMetadata.Title, null, htmlMetadata.Description, htmlMetadata.ThumbnailUrl);
    }

    private async Task<string?> TryReadYouTubeTranscriptAsync(string url, CancellationToken ct)
    {
        var videoId = ExtractYouTubeVideoId(url);
        if (videoId is null) return null;

        try
        {
            var html = await _httpClient.GetStringAsync($"https://www.youtube.com/watch?v={videoId}", ct);
            var match = Regex.Match(html, "\"captionTracks\":(?<tracks>\\[.*?\\])", RegexOptions.Singleline);
            if (!match.Success) return null;

            var tracksJson = match.Groups["tracks"].Value.Replace("\\u0026", "&").Replace("\\/", "/");
            using var tracks = JsonDocument.Parse(tracksJson);
            var baseUrl = tracks.RootElement.EnumerateArray()
                .OrderByDescending(track => GetJsonString(track, "languageCode")?.StartsWith("en", StringComparison.OrdinalIgnoreCase) == true)
                .Select(track => GetJsonString(track, "baseUrl"))
                .FirstOrDefault(value => !string.IsNullOrWhiteSpace(value));
            if (string.IsNullOrWhiteSpace(baseUrl)) return null;

            var transcriptXml = await _httpClient.GetStringAsync(baseUrl, ct);
            var transcript = XDocument.Parse(transcriptXml)
                .Descendants("text")
                .Select(node => WebUtility.HtmlDecode(node.Value))
                .Where(value => !string.IsNullOrWhiteSpace(value));

            return CleanText(string.Join(' ', transcript));
        }
        catch (Exception ex)
        {
            _logger.LogDebug(ex, "YouTube transcript extraction failed for {Url}", url);
            return null;
        }
    }

    private sealed record YouTubeMetadata(string? Title, string? Author, string? Description, string? ThumbnailUrl);
}

public class WebPageContentProcessor : IContentProcessor
{
    private readonly HttpClient _httpClient;
    private readonly ILogger<WebPageContentProcessor> _logger;

    public WebPageContentProcessor(HttpClient httpClient, ILogger<WebPageContentProcessor> logger)
    {
        _httpClient = PrepareClient(httpClient);
        _logger = logger;
    }

    public async Task<ContentProcessingResult> ProcessAsync(string url, CancellationToken ct = default)
    {
        _logger.LogInformation("Processing Web URL: {Url}", url);

        using var response = await _httpClient.GetAsync(url, ct);
        response.EnsureSuccessStatusCode();
        var html = await response.Content.ReadAsStringAsync(ct);

        var document = new HtmlDocument();
        document.LoadHtml(html);
        RemoveNoise(document);

        var title = FirstNonEmpty(
            ReadMeta(document, "property", "og:title"),
            ReadMeta(document, "name", "twitter:title"),
            CleanText(document.DocumentNode.SelectSingleNode("//title")?.InnerText),
            CleanText(document.DocumentNode.SelectSingleNode("//h1")?.InnerText),
            url);
        var description = FirstNonEmpty(
            ReadMeta(document, "name", "description"),
            ReadMeta(document, "property", "og:description"));
        var heroImage = FirstNonEmpty(
            ReadMeta(document, "property", "og:image"),
            ReadMeta(document, "name", "twitter:image"));
        var mainText = ExtractMainText(document);
        var rawText = CleanText(string.Join(". ", new[] { title, description, mainText }
            .Where(value => !string.IsNullOrWhiteSpace(value))));
        var consumeTime = EstimateMinutes(rawText, fallback: 8);

        return new ContentProcessingResult
        {
            Title = title,
            RawText = rawText,
            HeroImageUrl = heroImage,
            EstimatedConsumeTimeMins = consumeTime,
            SuggestedEnergyLevel = EnergyForMinutes(consumeTime),
            SuggestedTags = ExtractTags(document, "web", "article")
        };
    }
}

public class InstagramContentProcessor : IContentProcessor
{
    private readonly HttpClient _httpClient;
    private readonly ILogger<InstagramContentProcessor> _logger;

    public InstagramContentProcessor(HttpClient httpClient, ILogger<InstagramContentProcessor> logger)
    {
        _httpClient = PrepareClient(httpClient);
        _logger = logger;
    }

    public async Task<ContentProcessingResult> ProcessAsync(string url, CancellationToken ct = default)
    {
        _logger.LogInformation("Processing Instagram URL: {Url}", url);
        var metadata = await ReadHtmlMetadataAsync(_httpClient, url, defaultTitle: "Instagram content", ct);
        var rawText = CleanText(string.Join(". ", new[] { metadata.Title, metadata.Description }.Where(x => !string.IsNullOrWhiteSpace(x))));

        return new ContentProcessingResult
        {
            Title = metadata.Title ?? "Instagram content",
            RawText = rawText,
            HeroImageUrl = metadata.ThumbnailUrl,
            EstimatedConsumeTimeMins = EstimateMinutes(rawText, fallback: 2),
            SuggestedEnergyLevel = EnergyLevel.BrainDead,
            SuggestedTags = new List<string> { "social", "instagram" }
        };
    }
}

public class PdfContentProcessor : IContentProcessor
{
    private readonly HttpClient _httpClient;
    private readonly ILogger<PdfContentProcessor> _logger;

    public PdfContentProcessor(HttpClient httpClient, ILogger<PdfContentProcessor> logger)
    {
        _httpClient = PrepareClient(httpClient);
        _logger = logger;
    }

    public async Task<ContentProcessingResult> ProcessAsync(string url, CancellationToken ct = default)
    {
        _logger.LogInformation("Processing PDF: {Url}", url);

        var bytes = await _httpClient.GetByteArrayAsync(url, ct);
        var title = CreateTitleFromUrl(url);
        var rawText = ExtractReadablePdfText(bytes);
        var consumeTime = EstimateMinutes(rawText, fallback: 20);

        return new ContentProcessingResult
        {
            Title = title,
            RawText = string.IsNullOrWhiteSpace(rawText) ? title : rawText,
            EstimatedConsumeTimeMins = consumeTime,
            SuggestedEnergyLevel = EnergyLevel.DeepDive,
            SuggestedTags = new List<string> { "document", "pdf" }
        };
    }
}

public class TikTokContentProcessor : IContentProcessor
{
    private readonly HttpClient _httpClient;
    private readonly ILogger<TikTokContentProcessor> _logger;

    public TikTokContentProcessor(HttpClient httpClient, ILogger<TikTokContentProcessor> logger)
    {
        _httpClient = PrepareClient(httpClient);
        _logger = logger;
    }

    public async Task<ContentProcessingResult> ProcessAsync(string url, CancellationToken ct = default)
    {
        _logger.LogInformation("Processing TikTok URL: {Url}", url);
        var metadata = await ReadHtmlMetadataAsync(_httpClient, url, defaultTitle: "TikTok content", ct);
        var rawText = CleanText(string.Join(". ", new[] { metadata.Title, metadata.Description }.Where(x => !string.IsNullOrWhiteSpace(x))));

        return new ContentProcessingResult
        {
            Title = metadata.Title ?? "TikTok content",
            RawText = rawText,
            HeroImageUrl = metadata.ThumbnailUrl,
            EstimatedConsumeTimeMins = EstimateMinutes(rawText, fallback: 1),
            SuggestedEnergyLevel = EnergyLevel.BrainDead,
            SuggestedTags = new List<string> { "social", "tiktok", "short-form" }
        };
    }
}

internal sealed record HtmlMetadata(string? Title, string? Description, string? ThumbnailUrl);

internal static class ContentProcessorHelpers
{
    public static HttpClient PrepareClient(HttpClient httpClient)
    {
        httpClient.Timeout = TimeSpan.FromSeconds(30);
        if (!httpClient.DefaultRequestHeaders.UserAgent.Any())
            httpClient.DefaultRequestHeaders.UserAgent.ParseAdd("CortexBot/1.0 (+https://localhost)");
        return httpClient;
    }

    public static async Task<HtmlMetadata> ReadHtmlMetadataAsync(HttpClient httpClient, string url, string defaultTitle, CancellationToken ct)
    {
        try
        {
            using var response = await httpClient.GetAsync(url, ct);
            response.EnsureSuccessStatusCode();
            var html = await response.Content.ReadAsStringAsync(ct);
            var document = new HtmlDocument();
            document.LoadHtml(html);

            return new HtmlMetadata(
                FirstNonEmpty(ReadMeta(document, "property", "og:title"), ReadMeta(document, "name", "twitter:title"), CleanText(document.DocumentNode.SelectSingleNode("//title")?.InnerText), defaultTitle),
                FirstNonEmpty(ReadMeta(document, "property", "og:description"), ReadMeta(document, "name", "description")),
                FirstNonEmpty(ReadMeta(document, "property", "og:image"), ReadMeta(document, "name", "twitter:image")));
        }
        catch
        {
            return new HtmlMetadata(defaultTitle, url, null);
        }
    }

    public static void RemoveNoise(HtmlDocument document)
    {
        var nodes = document.DocumentNode.SelectNodes("//script|//style|//noscript|//svg|//nav|//footer|//header|//form|//aside|//iframe");
        if (nodes is null) return;

        foreach (var node in nodes)
            node.Remove();
    }

    public static string ExtractMainText(HtmlDocument document)
    {
        var candidates = document.DocumentNode.SelectNodes("//article|//main|//*[contains(@class,'content')]|//*[contains(@class,'post')]|//*[contains(@class,'article')]|//body");
        var bestNode = candidates?
            .OrderByDescending(node => CleanText(node.InnerText).Length)
            .FirstOrDefault();

        return CleanText(bestNode?.InnerText);
    }

    public static string? ReadMeta(HtmlDocument document, string attribute, string value)
    {
        var node = document.DocumentNode.SelectSingleNode($"//meta[translate(@{attribute}, 'ABCDEFGHIJKLMNOPQRSTUVWXYZ', 'abcdefghijklmnopqrstuvwxyz')='{value.ToLowerInvariant()}']");
        return CleanText(node?.GetAttributeValue("content", null));
    }

    public static List<string> ExtractTags(HtmlDocument document, params string[] defaults)
    {
        var tags = defaults.Select(tag => tag.ToLowerInvariant()).ToList();
        var keywords = ReadMeta(document, "name", "keywords");
        if (!string.IsNullOrWhiteSpace(keywords))
        {
            tags.AddRange(keywords
                .Split(',', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries)
                .Select(tag => tag.ToLowerInvariant())
                .Where(tag => tag.Length <= 40)
                .Take(6));
        }

        return tags.Distinct().ToList();
    }

    public static string CleanText(string? value)
    {
        if (string.IsNullOrWhiteSpace(value))
            return string.Empty;

        var decoded = HtmlEntity.DeEntitize(WebUtility.HtmlDecode(value));
        return Regex.Replace(decoded, "\\s+", " ").Trim();
    }

    public static int EstimateMinutes(string rawText, int fallback)
    {
        var wordCount = Regex.Matches(rawText ?? string.Empty, "\\b\\w+\\b").Count;
        if (wordCount == 0) return fallback;
        return Math.Max(1, (int)Math.Ceiling(wordCount / 220d));
    }

    public static EnergyLevel EnergyForMinutes(int minutes)
        => minutes switch
        {
            <= 3 => EnergyLevel.BrainDead,
            <= 12 => EnergyLevel.QuickSpark,
            _ => EnergyLevel.DeepDive
        };

    public static string FirstNonEmpty(params string?[] values)
        => values.FirstOrDefault(value => !string.IsNullOrWhiteSpace(value))?.Trim() ?? string.Empty;

    public static string? GetJsonString(JsonElement element, string propertyName)
        => element.TryGetProperty(propertyName, out var property) && property.ValueKind == JsonValueKind.String
            ? property.GetString()
            : null;

    public static string? ExtractYouTubeVideoId(string url)
    {
        if (!Uri.TryCreate(url, UriKind.Absolute, out var uri)) return null;

        if (uri.Host.Contains("youtu.be", StringComparison.OrdinalIgnoreCase))
            return uri.AbsolutePath.Trim('/').Split('/').FirstOrDefault();

        var videoId = GetQueryParameter(uri.Query, "v");
        if (!string.IsNullOrWhiteSpace(videoId)) return videoId;

        var segments = uri.AbsolutePath.Trim('/').Split('/', StringSplitOptions.RemoveEmptyEntries);
        if (segments.Length >= 2 && segments[0] is "embed" or "shorts")
            return segments[1];

        return null;
    }

    public static string CreateTitleFromUrl(string url)
    {
        if (!Uri.TryCreate(url, UriKind.Absolute, out var uri))
            return "PDF document";

        var fileName = WebUtility.UrlDecode(Path.GetFileNameWithoutExtension(uri.AbsolutePath));
        return string.IsNullOrWhiteSpace(fileName)
            ? "PDF document"
            : CleanText(fileName.Replace('-', ' ').Replace('_', ' '));
    }

    public static string ExtractReadablePdfText(byte[] bytes)
    {
        var text = Encoding.Latin1.GetString(bytes);
        var matches = Regex.Matches(text, "\\((?<text>(?:\\\\.|[^\\\\)])*)\\)\\s*Tj");
        var values = matches
            .Select(match => match.Groups["text"].Value)
            .Select(UnescapePdfText)
            .Where(value => !string.IsNullOrWhiteSpace(value));

        return CleanText(string.Join(' ', values));
    }

    private static string UnescapePdfText(string value)
        => value
            .Replace("\\(", "(", StringComparison.Ordinal)
            .Replace("\\)", ")", StringComparison.Ordinal)
            .Replace("\\\\", "\\", StringComparison.Ordinal);

    private static string? GetQueryParameter(string query, string name)
    {
        var trimmed = query.TrimStart('?');
        foreach (var pair in trimmed.Split('&', StringSplitOptions.RemoveEmptyEntries))
        {
            var parts = pair.Split('=', 2);
            if (parts.Length == 2 && string.Equals(Uri.UnescapeDataString(parts[0]), name, StringComparison.OrdinalIgnoreCase))
                return Uri.UnescapeDataString(parts[1]);
        }

        return null;
    }
}
