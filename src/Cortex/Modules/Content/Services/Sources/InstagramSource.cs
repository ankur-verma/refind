using Cortex.Infrastructure.Media;
using Cortex.Infrastructure.AI;
using Cortex.Modules.Content.Domain;
using Cortex.Shared.Enums;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using static Cortex.Modules.Content.Services.Sources.ContentExtractionHelpers;

namespace Cortex.Modules.Content.Services.Sources;

public class InstagramSource : IContentSource
{
    private readonly HttpClient _httpClient;
    private readonly LocalVideoExtractor _extractor;
    private readonly LocalAudioTranscriber _transcriber;
    private readonly IAIExtractionService _aiService;
    private readonly Cortex.Infrastructure.Settings.LocalExtractionSettings _settings;
    private readonly ILogger<InstagramSource> _logger;

    public PlatformType PlatformType => PlatformType.Instagram;

    public InstagramSource(
        HttpClient httpClient,
        LocalVideoExtractor extractor,
        LocalAudioTranscriber transcriber,
        IAIExtractionService aiService,
        IOptions<Cortex.Infrastructure.Settings.LocalExtractionSettings> settings,
        ILogger<InstagramSource> logger)
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
        return uri.Host.ToLowerInvariant().Contains("instagram.com");
    }

    public async Task<ContentExtractionResult> ExtractAsync(string url, CancellationToken ct = default)
    {
        var result = new ContentExtractionResult();

        if (_settings.UseGeminiVideoAnalysis)
        {
            _logger.LogInformation("Processing Instagram URL via Gemini Native Video API: {Url}", url);
            var mp4Result = await _extractor.DownloadMp4OnlyAsync(url, ct);
            if (mp4Result?.Mp4FilePath is not null)
            {
                var geminiAnalysis = await _aiService.AnalyzeVideoFileAsync(
                    mp4Result.Mp4FilePath, 
                    "Provide a full transcript and a chronological visual description of this video.", 
                    ct);

                var geminiMetadata = await ReadHtmlMetadataAsync(_httpClient, url, "Instagram content", ct);

                try { File.Delete(mp4Result.Mp4FilePath); } catch { }

                result.Title = mp4Result.Title ?? geminiMetadata.Title ?? "Instagram content";
                result.Description = geminiMetadata.Description ?? "";
                result.Transcript = CleanText(geminiAnalysis);
                result.Tags = new List<string> { "social", "instagram", "reel" };
                
                if (!string.IsNullOrEmpty(mp4Result.ThumbnailUrl ?? geminiMetadata.ThumbnailUrl))
                {
                    result.Media.Add(mp4Result.ThumbnailUrl ?? geminiMetadata.ThumbnailUrl!);
                }
                
                return result;
            }
            _logger.LogWarning("Gemini video download failed for {Url}, falling back to local extraction.", url);
        }

        _logger.LogInformation("Processing Instagram URL: {Url}", url);

        var extraction = await _extractor.ExtractAsync(url, ct);
        if (extraction is not null)
        {
            string? transcript = null;
            string? visualTimeline = null;
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
                _logger.LogInformation("Generating visual description timeline from {Count} keyframes for Instagram post: {Url}", extraction.Keyframes.Count, url);
                visualTimeline = await _aiService.DescribeVideoFramesAsync(extraction.Keyframes, ct);
            }

            result.Title = extraction.Title ?? "Instagram content";
            result.Description = extraction.Description ?? "";
            
            var transcriptBuilder = new System.Text.StringBuilder();
            if (!string.IsNullOrWhiteSpace(visualTimeline)) transcriptBuilder.AppendLine($"[Visual Timeline & Keyframe Description]\n{visualTimeline}\n");
            if (!string.IsNullOrWhiteSpace(transcript)) transcriptBuilder.AppendLine($"[Audio Transcript]\n{transcript}\n");
            
            result.Transcript = CleanText(transcriptBuilder.ToString());
            if (!string.IsNullOrEmpty(extraction.ThumbnailUrl)) result.Media.Add(extraction.ThumbnailUrl);
            result.Tags = new List<string> { "social", "instagram", "reel" };

            return result;
        }

        _logger.LogWarning("Local extraction unavailable for {Url}; falling back to HTML metadata", url);
        var metadata = await ReadHtmlMetadataAsync(_httpClient, url, "Instagram content", ct);
        
        result.Title = metadata.Title ?? "Instagram content";
        result.Description = metadata.Description ?? "";
        if (!string.IsNullOrEmpty(metadata.ThumbnailUrl)) result.Media.Add(metadata.ThumbnailUrl);
        result.Tags = new List<string> { "social", "instagram" };

        return result;
    }
}
