namespace Cortex.Infrastructure.Settings;

public class LocalExtractionSettings
{
    /// <summary>Absolute path to the yt-dlp binary.</summary>
    public string YtDlpPath { get; set; } = "/usr/local/bin/yt-dlp";

    /// <summary>Absolute path to the ffmpeg binary (required by yt-dlp for audio conversion).</summary>
    public string FfmpegPath { get; set; } = "/usr/local/bin/ffmpeg";

    /// <summary>Relative or absolute path to the ggml Whisper model file (e.g. ggml-base.en.bin).</summary>
    public string WhisperModelPath { get; set; } = "whisper-models/ggml-base.en.bin";

    /// <summary>Temporary directory used to store downloaded audio files before transcription. Cleaned up automatically.</summary>
    public string TempDirectory { get; set; } = "/tmp/cortex-media";

    /// <summary>Set to true to upload the video to Gemini's File API for full processing instead of using local Whisper/Keyframes.</summary>
    public bool UseGeminiVideoAnalysis { get; set; } = true;

    /// <summary>The Whisper model type to use for local extraction (e.g. BaseEn, LargeV3).</summary>
    public string WhisperModelType { get; set; } = "LargeV3";
}
