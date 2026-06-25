using HtmlAgilityPack;
using System.Net;
using System.Text;
using System.Text.Json;
using System.Text.RegularExpressions;
using Cortex.Shared.Enums;

namespace Cortex.Modules.Content.Services.Sources;

public record HtmlMetadata(string? Title, string? Description, string? ThumbnailUrl);

public static class ContentExtractionHelpers
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
        return CleanText(node?.GetAttributeValue("content", ""));
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
