namespace Cortex.Infrastructure.AI;

public class HybridRecommendationDto
{
    public Guid TargetId { get; set; }
    public string Title { get; set; } = string.Empty;
    public double Score { get; set; }
    public string Reason { get; set; } = string.Empty;
}

public class HybridRecommendationResponse
{
    public Dictionary<string, List<HybridRecommendationDto>> Recommendations { get; set; } = new();
}
