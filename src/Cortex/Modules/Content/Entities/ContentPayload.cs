using Pgvector;

namespace Cortex.Modules.Content.Entities;

/// <summary>
/// Isolated heavy AI data (1:1 with ContentItem).
/// Contains the raw text, summary, and semantic embedding vector.
/// Lazy-loaded to prevent accidentally pulling massive data in list queries.
/// </summary>
public class ContentPayload
{
    public Guid ContentItemId { get; set; }
    public string RawText { get; set; } = string.Empty;
    public string? QuickSparkSummary { get; set; }
    public Vector? GeminiEmbedding { get; set; }
    public Vector? OllamaEmbedding { get; set; }

    // Navigation
    public ContentItem ContentItem { get; set; } = null!;
}
