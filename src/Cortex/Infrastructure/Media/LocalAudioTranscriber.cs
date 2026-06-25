using Cortex.Infrastructure.Settings;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Whisper.net;
using Whisper.net.Ggml;

namespace Cortex.Infrastructure.Media;

/// <summary>
/// Transcribes a 16kHz mono WAV audio file to text using the local Whisper model.
/// The model is loaded lazily and cached for the lifetime of the service.
/// </summary>
public class LocalAudioTranscriber : IDisposable
{
    private readonly LocalExtractionSettings _settings;
    private readonly IHttpClientFactory _httpClientFactory;
    private readonly ILogger<LocalAudioTranscriber> _logger;

    private WhisperFactory? _factory;
    private readonly SemaphoreSlim _initLock = new(1, 1);
    private bool _disposed;

    public LocalAudioTranscriber(IOptions<LocalExtractionSettings> settings, IHttpClientFactory httpClientFactory, ILogger<LocalAudioTranscriber> logger)
    {
        _settings = settings.Value;
        _httpClientFactory = httpClientFactory;
        _logger = logger;
    }

    /// <summary>
    /// Transcribes the given WAV file and returns the full transcript text.
    /// Returns null if the model cannot be loaded or transcription fails.
    /// </summary>
    public async Task<string?> TranscribeAsync(string wavFilePath, CancellationToken ct = default)
    {
        if (!File.Exists(wavFilePath))
        {
            _logger.LogWarning("WAV file not found for transcription: {Path}", wavFilePath);
            return null;
        }

        var factory = await EnsureModelLoadedAsync(ct);
        if (factory is null)
            return null;

        try
        {
            _logger.LogInformation("Transcribing audio: {Path}", wavFilePath);

            await using var processor = factory.CreateBuilder()
                .WithLanguage("en")
                .Build();

            var segments = new List<string>();

            await using var fileStream = File.OpenRead(wavFilePath);
            await foreach (var segment in processor.ProcessAsync(fileStream, ct))
            {
                if (!string.IsNullOrWhiteSpace(segment.Text))
                    segments.Add(segment.Text.Trim());
            }

            var transcript = string.Join(" ", segments);
            _logger.LogInformation("Transcription complete ({Length} chars) for {Path}", transcript.Length, wavFilePath);
            return string.IsNullOrWhiteSpace(transcript) ? null : transcript;
        }
        catch (OperationCanceledException)
        {
            throw;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Transcription failed for {Path}", wavFilePath);
            return null;
        }
    }

    private async Task<WhisperFactory?> EnsureModelLoadedAsync(CancellationToken ct)
    {
        _logger.LogWarning("Local Whisper transcription is disabled on x86_64 macOS to prevent native library crash.");
        return null;
#pragma warning disable CS0162
        if (_factory is not null)
            return _factory;

        await _initLock.WaitAsync(ct);
        try
        {
            if (_factory is not null)
                return _factory;

            var modelPath = _settings.WhisperModelPath;

            // Support paths relative to the app base directory
            if (!Path.IsPathRooted(modelPath))
                modelPath = Path.Combine(AppContext.BaseDirectory, modelPath);

            // Auto-download the model if it is missing
            if (!File.Exists(modelPath))
            {
                if (!Enum.TryParse<GgmlType>(_settings.WhisperModelType, true, out var ggmlType))
                {
                    _logger.LogWarning("Invalid WhisperModelType '{Type}' configured; falling back to BaseEn.", _settings.WhisperModelType);
                    ggmlType = GgmlType.BaseEn;
                }

                _logger.LogInformation("Whisper model not found at {Path}; downloading {GgmlType} …", modelPath, ggmlType);
                Directory.CreateDirectory(Path.GetDirectoryName(modelPath)!);

                using var httpClient = _httpClientFactory.CreateClient();
                await using var modelStream = await new WhisperGgmlDownloader(httpClient).GetGgmlModelAsync(ggmlType);
                await using var fileStream = File.OpenWrite(modelPath);
                await modelStream.CopyToAsync(fileStream, ct);

                _logger.LogInformation("Whisper model downloaded to {Path}", modelPath);
            }

            _logger.LogInformation("Loading Whisper model from {Path}", modelPath);
            _factory = WhisperFactory.FromPath(modelPath);
            return _factory;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to load Whisper model");
            return null;
        }
        finally
        {
            _initLock.Release();
        }
    }

    public void Dispose()
    {
        if (_disposed) return;
        _disposed = true;
        _factory?.Dispose();
        _initLock.Dispose();
    }
}
