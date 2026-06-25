using Cortex.Infrastructure.Media;
using Cortex.Infrastructure.AI;
using Cortex.Modules.Content.Domain;
using Cortex.Shared.Enums;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using System.Text.Json;
using System.Xml.Linq;
using System.Text.RegularExpressions;
using System.Net;
using static Cortex.Modules.Content.Services.Sources.ContentExtractionHelpers;

namespace Cortex.Modules.Content.Services.Sources;

public class YouTubeSource : IContentSource
{
    private readonly HttpClient _httpClient;
    private readonly LocalVideoExtractor _extractor;
    private readonly LocalAudioTranscriber _transcriber;
    private readonly IAIExtractionService _aiService;
    private readonly Cortex.Infrastructure.Settings.LocalExtractionSettings _settings;
    private readonly ILogger<YouTubeSource> _logger;

    public PlatformType PlatformType => PlatformType.YouTube;

    public YouTubeSource(
        HttpClient httpClient,
        LocalVideoExtractor extractor,
        LocalAudioTranscriber transcriber,
        IAIExtractionService aiService,
        IOptions<Cortex.Infrastructure.Settings.LocalExtractionSettings> settings,
        ILogger<YouTubeSource> logger)
    {
        _httpClient = PrepareClient(httpClient);
        _extractor = extractor;
        _transcriber = transcriber;
        _aiService = aiService;
        _settings = settings.Value;
        _logger = logger;
    }

    public bool CanHandle(string url)
    {
        if (!Uri.TryCreate(url, UriKind.Absolute, out var uri)) return false;
        var host = uri.Host.ToLowerInvariant();
        return host.Contains("youtube.com") || host.Contains("youtu.be");
    }

    public async Task<ContentExtractionResult> ExtractAsync(string url, CancellationToken ct = default)
    {
        var result = new ContentExtractionResult();

        if (_settings.UseGeminiVideoAnalysis)
        {
            _logger.LogInformation("Processing YouTube URL via Gemini Native Video API: {Url}", url);
            var mp4Result = await _extractor.DownloadMp4OnlyAsync(url, ct);
            if (mp4Result?.Mp4FilePath is not null)
            {
                var geminiAnalysis = await _aiService.AnalyzeVideoFileAsync(
                    mp4Result.Mp4FilePath, 
                    "Provide a full transcript and a chronological visual description of this video.", 
                    ct);

                var geminiMetadata = await ReadYouTubeMetadataAsync(url, ct);

                try { File.Delete(mp4Result.Mp4FilePath); } catch { }

                result.Title = mp4Result.Title ?? geminiMetadata.Title ?? "YouTube video";
                result.Author = geminiMetadata.Author ?? "";
                result.Transcript = CleanText(geminiAnalysis);
                result.Tags = new List<string> { "video", "youtube", "tutorial" };
                
                if (!string.IsNullOrEmpty(mp4Result.ThumbnailUrl ?? geminiMetadata.ThumbnailUrl))
                {
                    result.Media.Add(mp4Result.ThumbnailUrl ?? geminiMetadata.ThumbnailUrl!);
                }
                
                return result;
            }
            _logger.LogWarning("Gemini video download failed for {Url}, falling back to local extraction.", url);
        }

        _logger.LogInformation("Processing YouTube URL via yt-dlp + Whisper: {Url}", url);

        string? transcript = null;
        string? title = null, description = null, thumbnailUrl = null;
        string? visualTimeline = null;

        var extraction = await _extractor.ExtractAsync(url, ct);
        if (extraction is not null)
        {
            title = extraction.Title;
            description = extraction.Description;
            thumbnailUrl = extraction.ThumbnailUrl;

            if (extraction.WavFilePath is not null)
            {
                try
                {
                    transcript = await _transcriber.TranscribeAsync(extraction.WavFilePath, ct);
                }
                finally
                {
                    try { File.Delete(extraction.WavFilePath); } catch { }
                }
            }

            if (extraction.Keyframes is not null && extraction.Keyframes.Count > 0)
            {
                _logger.LogInformation("Generating visual description timeline from {Count} keyframes for YouTube video: {Url}", extraction.Keyframes.Count, url);
                visualTimeline = await _aiService.DescribeVideoFramesAsync(extraction.Keyframes, ct);
            }
        }

        if (string.IsNullOrWhiteSpace(transcript))
        {
            _logger.LogWarning("Audio transcription unavailable for {Url}; falling back to XML caption track", url);
            transcript = await TryReadYouTubeTranscriptAsync(url, ct);
        }

        var metadata = await ReadYouTubeMetadataAsync(url, ct);
        title ??= metadata.Title;
        description ??= metadata.Description;
        thumbnailUrl ??= metadata.ThumbnailUrl;

        result.Title = title ?? "YouTube video";
        result.Description = description ?? "";
        result.Author = metadata.Author ?? "";
        if (!string.IsNullOrEmpty(thumbnailUrl)) result.Media.Add(thumbnailUrl);
        
        var transcriptBuilder = new System.Text.StringBuilder();
        if (!string.IsNullOrWhiteSpace(visualTimeline)) transcriptBuilder.AppendLine($"[Visual Timeline & Keyframe Description]\n{visualTimeline}\n");
        if (!string.IsNullOrWhiteSpace(transcript)) transcriptBuilder.AppendLine($"[Audio Transcript]\n{transcript}\n");
        
        result.Transcript = CleanText(transcriptBuilder.ToString());
        result.Tags = new List<string> { "video", "youtube", "tutorial" };

        return result;
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
