using Cortex.Infrastructure.Settings;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using System.Diagnostics;
using System.Text.Json;

namespace Cortex.Infrastructure.Media;

/// <summary>
/// Extracts audio from social media / video URLs using yt-dlp.
/// The output is a 16kHz mono WAV file suitable for Whisper transcription.
/// </summary>
public class LocalVideoExtractor
{
    private readonly LocalExtractionSettings _settings;
    private readonly ILogger<LocalVideoExtractor> _logger;

    public LocalVideoExtractor(IOptions<LocalExtractionSettings> settings, ILogger<LocalVideoExtractor> logger)
    {
        _settings = settings.Value;
        _logger = logger;
    }

    /// <summary>
    /// Downloads low-res video from the given URL and returns a <see cref="VideoExtractionResult"/>
    /// containing metadata, the path to a temporary WAV file, and visual keyframe images.
    /// The caller is responsible for deleting the WAV file after use.
    /// Returns null if extraction is disabled or fails.
    /// </summary>
    public async Task<VideoExtractionResult?> ExtractAsync(string url, CancellationToken ct = default)
    {
        Directory.CreateDirectory(_settings.TempDirectory);
        var sessionId = Guid.NewGuid().ToString("N");
        var mp4Path = Path.Combine(_settings.TempDirectory, $"{sessionId}.mp4");
        var wavPath = Path.Combine(_settings.TempDirectory, $"{sessionId}.wav");

        try
        {
            _logger.LogInformation("Starting yt-dlp extraction for {Url}", url);

            // Step 1 – Fetch JSON metadata
            var metadata = await RunYtDlpAsync(new[]
            {
                "--dump-json",
                "--no-check-certificates",
                "--no-playlist",
                "--no-warnings",
                url
            }, ct);

            string? title = null, description = null, thumbnailUrl = null;
            double duration = 0;
            if (!string.IsNullOrWhiteSpace(metadata))
            {
                try
                {
                    using var doc = JsonDocument.Parse(metadata);
                    var root = doc.RootElement;
                    title = TryGetString(root, "title");
                    description = TryGetString(root, "description");
                    thumbnailUrl = TryGetString(root, "thumbnail");
                    if (root.TryGetProperty("duration", out var durProp) && durProp.ValueKind == JsonValueKind.Number)
                    {
                        duration = durProp.GetDouble();
                    }
                }
                catch (JsonException ex)
                {
                    _logger.LogDebug(ex, "Could not parse yt-dlp JSON metadata for {Url}", url);
                }
            }

            // Step 2 – Download worst/low-resolution video merged track
            _logger.LogInformation("Downloading low-res video for {Url}", url);
            await RunYtDlpAsync(new[]
            {
                "--no-check-certificates",
                "--no-playlist",
                "--no-warnings",
                "-f", "worstvideo[height<=240]+worstaudio/worst/worst",
                "--merge-output-format", "mp4",
                "-o", mp4Path,
                url
            }, ct);

            // Robust check: if output was saved with another extension (e.g. .mkv, .webm)
            if (!File.Exists(mp4Path))
            {
                var files = Directory.GetFiles(_settings.TempDirectory, $"{sessionId}.*");
                var videoFile = files.FirstOrDefault(f => !f.EndsWith(".wav", StringComparison.OrdinalIgnoreCase) && !f.EndsWith(".jpg", StringComparison.OrdinalIgnoreCase));
                if (videoFile is not null)
                {
                    mp4Path = videoFile;
                }
            }

            if (!File.Exists(mp4Path))
            {
                _logger.LogWarning("Video file download failed at {Path} for {Url}", mp4Path, url);
                return null;
            }

            // Step 3 – Extract audio to 16kHz mono WAV suitable for Whisper
            _logger.LogInformation("Extracting WAV audio track for {Url}", url);
            await RunFfmpegAsync(new[]
            {
                "-y",
                "-i", mp4Path,
                "-ar", "16000",
                "-ac", "1",
                wavPath
            }, ct);

            if (!File.Exists(wavPath))
            {
                _logger.LogWarning("Failed to extract WAV audio file for {Url}", url);
            }

            // Step 4 – Extract visual keyframes dynamically based on duration
            var keyframes = new List<Cortex.Infrastructure.AI.VideoKeyframe>();
            var interval = duration switch
            {
                <= 120 => 10,  // Short videos: extract every 10s
                <= 600 => 30,  // Medium videos: extract every 30s
                _ => 60        // Long videos: extract every 60s
            };

            var fps = $"1/{interval}";
            var framePattern = Path.Combine(_settings.TempDirectory, $"{sessionId}_frame_%03d.jpg");
            _logger.LogInformation("Extracting keyframes at {Interval}s interval (fps={Fps}) for {Url}", interval, fps, url);

            await RunFfmpegAsync(new[]
            {
                "-y",
                "-i", mp4Path,
                "-vf", $"fps={fps},scale=480:-1",
                "-q:v", "5",
                framePattern
            }, ct);

            var frameFiles = Directory.GetFiles(_settings.TempDirectory, $"{sessionId}_frame_*.jpg")
                .OrderBy(f => f)
                .ToList();

            // Sample keyframes: cap at 20 frames total to control token/memory consumption
            if (frameFiles.Count > 20)
            {
                var sampled = new List<string>();
                double step = (double)frameFiles.Count / 20;
                for (int i = 0; i < 20; i++)
                {
                    int idx = (int)Math.Min(frameFiles.Count - 1, Math.Round(i * step));
                    sampled.Add(frameFiles[idx]);
                }
                frameFiles = sampled.Distinct().ToList();
            }

            foreach (var frameFile in frameFiles)
            {
                try
                {
                    // Extract index from filename (e.g. frame_001.jpg -> index 1)
                    var match = System.Text.RegularExpressions.Regex.Match(Path.GetFileName(frameFile), @"_frame_(\d+)\.jpg");
                    if (match.Success && int.TryParse(match.Groups[1].Value, out var frameIndex))
                    {
                        var totalSeconds = frameIndex * interval;
                        var timeSpan = TimeSpan.FromSeconds(totalSeconds);
                        var timestamp = $"[{timeSpan:mm\\:ss}]";

                        var bytes = await File.ReadAllBytesAsync(frameFile, ct);
                        var base64 = Convert.ToBase64String(bytes);

                        keyframes.Add(new Cortex.Infrastructure.AI.VideoKeyframe(timestamp, base64));
                    }
                }
                catch (Exception ex)
                {
                    _logger.LogWarning(ex, "Failed to load/encode keyframe {Path}", frameFile);
                }
                finally
                {
                    // Clean up individual keyframe file immediately after encoding
                    TryCleanup(frameFile);
                }
            }

            _logger.LogInformation("Successfully extracted WAV audio and {Count} keyframes for {Url}", keyframes.Count, url);

            return new VideoExtractionResult
            {
                Title = title,
                Description = description,
                ThumbnailUrl = thumbnailUrl,
                WavFilePath = File.Exists(wavPath) ? wavPath : null,
                Keyframes = keyframes
            };
        }
        catch (OperationCanceledException)
        {
            throw;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Local extraction failed for {Url}", url);
            TryCleanup(wavPath);
            return null;
        }
        finally
        {
            // Clean up downloaded video file
            TryCleanup(mp4Path);

            // Ensure all leftover frame files for this session are deleted
            try
            {
                var leftovers = Directory.GetFiles(_settings.TempDirectory, $"{sessionId}_frame_*.jpg");
                foreach (var leftover in leftovers)
                {
                    TryCleanup(leftover);
                }
            }
            catch { /* best effort */ }
        }
    }

    /// <summary>
    /// Downloads the video as MP4 without extracting audio or keyframes.
    /// Useful for uploading directly to Gemini for native processing.
    /// </summary>
    public async Task<VideoExtractionResult?> DownloadMp4OnlyAsync(string url, CancellationToken ct = default)
    {
        Directory.CreateDirectory(_settings.TempDirectory);
        var sessionId = Guid.NewGuid().ToString("N");
        var mp4Path = Path.Combine(_settings.TempDirectory, $"{sessionId}.mp4");

        try
        {
            _logger.LogInformation("Starting yt-dlp extraction for {Url} (MP4 only)", url);

            // Step 1 – Fetch JSON metadata
            var metadata = await RunYtDlpAsync(new[]
            {
                "--dump-json",
                "--no-check-certificates",
                "--no-playlist",
                "--no-warnings",
                url
            }, ct);

            string? title = null, description = null, thumbnailUrl = null;
            if (!string.IsNullOrWhiteSpace(metadata))
            {
                try
                {
                    using var doc = JsonDocument.Parse(metadata);
                    var root = doc.RootElement;
                    title = TryGetString(root, "title");
                    description = TryGetString(root, "description");
                    thumbnailUrl = TryGetString(root, "thumbnail");
                }
                catch (JsonException ex)
                {
                    _logger.LogDebug(ex, "Could not parse yt-dlp JSON metadata for {Url}", url);
                }
            }

            // Step 2 – Download worst/low-resolution video merged track
            _logger.LogInformation("Downloading low-res video for {Url}", url);
            await RunYtDlpAsync(new[]
            {
                "--no-check-certificates",
                "--no-playlist",
                "--no-warnings",
                "-f", "worstvideo[height<=480]+worstaudio/worst/worst",
                "--merge-output-format", "mp4",
                "-o", mp4Path,
                url
            }, ct);

            // Robust check: if output was saved with another extension
            if (!File.Exists(mp4Path))
            {
                var files = Directory.GetFiles(_settings.TempDirectory, $"{sessionId}.*");
                var videoFile = files.FirstOrDefault(f => !f.EndsWith(".wav", StringComparison.OrdinalIgnoreCase) && !f.EndsWith(".jpg", StringComparison.OrdinalIgnoreCase));
                if (videoFile is not null)
                {
                    mp4Path = videoFile;
                }
            }

            if (!File.Exists(mp4Path))
            {
                _logger.LogWarning("Video file download failed at {Path} for {Url}", mp4Path, url);
                return null;
            }

            _logger.LogInformation("Successfully downloaded MP4 for {Url}", url);

            return new VideoExtractionResult
            {
                Title = title,
                Description = description,
                ThumbnailUrl = thumbnailUrl,
                Mp4FilePath = mp4Path
            };
        }
        catch (OperationCanceledException)
        {
            throw;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Local extraction (MP4 only) failed for {Url}", url);
            TryCleanup(mp4Path);
            return null;
        }
    }

    private async Task RunFfmpegAsync(IEnumerable<string> args, CancellationToken ct)
    {
        var psi = new ProcessStartInfo
        {
            FileName = _settings.FfmpegPath,
            RedirectStandardOutput = true,
            RedirectStandardError = true,
            UseShellExecute = false,
            CreateNoWindow = true
        };

        psi.EnvironmentVariables["PATH"] = $"/usr/local/bin:{Environment.GetEnvironmentVariable("PATH")}";

        foreach (var arg in args)
            psi.ArgumentList.Add(arg);

        using var process = new Process { StartInfo = psi };
        process.Start();

        var stdoutTask = process.StandardOutput.ReadToEndAsync(ct);
        var stderrTask = process.StandardError.ReadToEndAsync(ct);

        await process.WaitForExitAsync(ct);

        var stdout = await stdoutTask;
        var stderr = await stderrTask;

        if (process.ExitCode != 0)
        {
            _logger.LogWarning("ffmpeg exited with code {ExitCode}. Stderr: {Stderr}", process.ExitCode, stderr);
            throw new Exception($"ffmpeg failed with exit code {process.ExitCode}");
        }
    }

    private async Task<string> RunYtDlpAsync(IEnumerable<string> args, CancellationToken ct)
    {
        var psi = new ProcessStartInfo
        {
            FileName = _settings.YtDlpPath,
            RedirectStandardOutput = true,
            RedirectStandardError = true,
            UseShellExecute = false,
            CreateNoWindow = true
        };

        psi.EnvironmentVariables["PATH"] = $"/usr/local/bin:{Environment.GetEnvironmentVariable("PATH")}";

        foreach (var arg in args)
            psi.ArgumentList.Add(arg);

        using var process = new Process { StartInfo = psi };
        process.Start();

        var stdoutTask = process.StandardOutput.ReadToEndAsync(ct);
        var stderrTask = process.StandardError.ReadToEndAsync(ct);

        await process.WaitForExitAsync(ct);

        var stdout = await stdoutTask;
        var stderr = await stderrTask;

        if (process.ExitCode != 0 && !string.IsNullOrWhiteSpace(stderr))
            _logger.LogDebug("yt-dlp stderr: {Stderr}", stderr);

        return stdout;
    }

    private static string? TryGetString(JsonElement element, string key)
        => element.TryGetProperty(key, out var prop) && prop.ValueKind == JsonValueKind.String
            ? prop.GetString()
            : null;

    private static void TryCleanup(string path)
    {
        try { if (File.Exists(path)) File.Delete(path); } catch { /* best effort */ }
    }
}

public sealed record VideoExtractionResult
{
    public string? Title { get; init; }
    public string? Description { get; init; }
    public string? ThumbnailUrl { get; init; }

    /// <summary>
    /// Absolute path to the temporary 16kHz mono WAV file.
    /// Null if audio could not be downloaded (metadata only).
    /// </summary>
    public string? WavFilePath { get; init; }

    /// <summary>
    /// Absolute path to the downloaded MP4 video file.
    /// Used when uploading directly to Gemini File API.
    /// </summary>
    public string? Mp4FilePath { get; init; }

    /// <summary>
    /// List of keyframes, containing the timestamp string (e.g. "[01:00]") and the base64 image data.
    /// </summary>
    public List<Cortex.Infrastructure.AI.VideoKeyframe>? Keyframes { get; init; }
}

