using Cortex.Infrastructure.Media;
using Cortex.Infrastructure.AI;
using Cortex.Modules.Content.Domain;
using Cortex.Shared.Enums;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using static Cortex.Modules.Content.Services.Sources.ContentExtractionHelpers;

namespace Cortex.Modules.Content.Services.Sources;

public class VideoSource : IContentSource
{
    private readonly HttpClient _httpClient;
    private readonly LocalVideoExtractor _extractor;
    private readonly LocalAudioTranscriber _transcriber;
    private readonly IAIExtractionService _aiService;
    private readonly Cortex.Infrastructure.Settings.LocalExtractionSettings _settings;
    private readonly ILogger<VideoSource> _logger;

    public PlatformType PlatformType => PlatformType.Video;

    public VideoSource(
        HttpClient httpClient,
        LocalVideoExtractor extractor,
        LocalAudioTranscriber transcriber,
        IAIExtractionService aiService,
        IOptions<Cortex.Infrastructure.Settings.LocalExtractionSettings> settings,
        ILogger<VideoSource> logger)
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
        var path = uri.AbsolutePath.ToLowerInvariant();
        
        return path.EndsWith(".mp4") || path.EndsWith(".mov") || path.EndsWith(".webm") || path.EndsWith(".avi") || 
               path.EndsWith(".mkv") || path.EndsWith(".flv") || path.EndsWith(".mp3") || path.EndsWith(".wav") || 
               path.EndsWith(".m4a") || path.EndsWith(".ogg") ||
               host.Contains("vimeo.com") || host.Contains("dailymotion.com") || host.Contains("twitch.tv") || 
               host.Contains("fb.watch") || host.Contains("facebook.com/watch") || host.Contains("loom.com") || 
               host.Contains("wistia.com") || host.Contains("rumble.com") || host.Contains("bilibili.com");
    }

    public async Task<ContentExtractionResult> ExtractAsync(string url, CancellationToken ct = default)
    {
        var result = new ContentExtractionResult();

        if (_settings.UseGeminiVideoAnalysis)
        {
            _logger.LogInformation("Processing Video URL via Gemini Native Video API: {Url}", url);
            var mp4Result = await _extractor.DownloadMp4OnlyAsync(url, ct);
            if (mp4Result?.Mp4FilePath is not null)
            {
                var geminiAnalysis = await _aiService.AnalyzeVideoFileAsync(
                    mp4Result.Mp4FilePath, 
                    "Provide a full transcript and a chronological visual description of this video.", 
                    ct);

                var geminiMetadata = await ReadHtmlMetadataAsync(_httpClient, url, "Video content", ct);

                try { File.Delete(mp4Result.Mp4FilePath); } catch { }

                result.Title = mp4Result.Title ?? geminiMetadata.Title ?? "Video content";
                result.Description = geminiMetadata.Description ?? "";
                result.Transcript = CleanText(geminiAnalysis);
                result.Tags = new List<string> { "video", "media" };
                
                if (!string.IsNullOrEmpty(mp4Result.ThumbnailUrl ?? geminiMetadata.ThumbnailUrl))
                {
                    result.Media.Add(mp4Result.ThumbnailUrl ?? geminiMetadata.ThumbnailUrl!);
                }
                
                return result;
            }
            _logger.LogWarning("Gemini video download failed for {Url}, falling back to local extraction.", url);
        }

        _logger.LogInformation("Processing Video URL: {Url}", url);

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
                _logger.LogInformation("Generating visual description timeline from {Count} keyframes for generic video: {Url}", extraction.Keyframes.Count, url);
                visualTimeline = await _aiService.DescribeVideoFramesAsync(extraction.Keyframes, ct);
            }
        }

        if (string.IsNullOrWhiteSpace(title))
        {
            _logger.LogWarning("Local video extraction failed/unavailable for {Url}; falling back to HTML metadata", url);
            var metadata = await ReadHtmlMetadataAsync(_httpClient, url, "Video content", ct);
            title ??= metadata.Title;
            description ??= metadata.Description;
            thumbnailUrl ??= metadata.ThumbnailUrl;
        }

        result.Title = title ?? "Video content";
        result.Description = description ?? "";
        
        var transcriptBuilder = new System.Text.StringBuilder();
        if (!string.IsNullOrWhiteSpace(visualTimeline)) transcriptBuilder.AppendLine($"[Visual Timeline & Keyframe Description]\n{visualTimeline}\n");
        if (!string.IsNullOrWhiteSpace(transcript)) transcriptBuilder.AppendLine($"[Audio Transcript]\n{transcript}\n");
        
        result.Transcript = CleanText(transcriptBuilder.ToString());
        if (!string.IsNullOrEmpty(thumbnailUrl)) result.Media.Add(thumbnailUrl);
        result.Tags = new List<string> { "video", "media" };

        return result;
    }
}
