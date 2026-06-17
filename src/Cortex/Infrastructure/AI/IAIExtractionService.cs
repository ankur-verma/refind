

namespace Cortex.Infrastructure.AI;

public record VideoKeyframe(string Timestamp, string Base64Data);

public interface IAIExtractionService
{
    Task<string> GenerateSummaryAsync(string rawText, CancellationToken ct = default);
    Task<float[]> GenerateEmbeddingAsync(string text, CancellationToken ct = default);
    Task<List<(string Description, string Type, int Order)>> ExtractActionsAsync(string rawText, CancellationToken ct = default);
    Task<string> GetChatCompletionAsync(string systemMessage, string userMessage, CancellationToken ct = default);
    Task<string> DescribeVideoFramesAsync(List<VideoKeyframe> keyframes, CancellationToken ct = default);
    Task<string> AnalyzeVideoFileAsync(string mp4FilePath, string prompt, CancellationToken ct = default);
}

