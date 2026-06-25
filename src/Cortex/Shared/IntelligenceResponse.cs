using System.Collections.Generic;

namespace Cortex.Shared;

public class IntelligenceResponse<T>
{
    public bool HasData { get; set; }
    public string? Reason { get; set; }
    public double? Confidence { get; set; }
    public int? SourceCount { get; set; }
    public List<string>? GeneratedFrom { get; set; }
    public T? Data { get; set; }

    public static IntelligenceResponse<T> Success(T data, double confidence, int sourceCount, List<string> generatedFrom)
    {
        return new IntelligenceResponse<T>
        {
            HasData = true,
            Confidence = confidence,
            SourceCount = sourceCount,
            GeneratedFrom = generatedFrom,
            Data = data
        };
    }

    public static IntelligenceResponse<T> InsufficientData(string reason = "insufficient_data")
    {
        return new IntelligenceResponse<T>
        {
            HasData = false,
            Reason = reason
        };
    }
}
