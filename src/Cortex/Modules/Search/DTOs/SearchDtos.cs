namespace Cortex.Modules.Search.DTOs;

public class SemanticSearchRequest
{
    public string Query { get; set; } = string.Empty;
    public int Limit { get; set; } = 10;
}

public class SearchResultResponse
{
    public Guid ContentItemId { get; set; }
    public string Title { get; set; } = string.Empty;
    public string? QuickSparkSummary { get; set; }
    public string? HeroImageUrl { get; set; }
    public double SimilarityScore { get; set; }
}
