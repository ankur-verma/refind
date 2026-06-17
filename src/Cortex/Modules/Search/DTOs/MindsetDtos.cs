using System;
using System.Collections.Generic;

namespace Cortex.Modules.Search.DTOs;

public class UpdateMindsetRequest
{
    public List<string> FocusAreas { get; set; } = new();
    public string ConsumptionPreference { get; set; } = "Concise";
    public string NarrativeSummary { get; set; } = string.Empty;
}

public class MindsetResponse
{
    public Guid Id { get; set; }
    public Guid UserId { get; set; }
    public List<string> FocusAreas { get; set; } = new();
    public string ConsumptionPreference { get; set; } = "Concise";
    public string NarrativeSummary { get; set; } = string.Empty;
    public DateTime LastUpdated { get; set; }
}

public class RAGQueryRequest
{
    public string Query { get; set; } = string.Empty;
    public string Mode { get; set; } = "Hybrid"; // Hybrid, Vector, Vectorless, SelectedLinks
    public List<Guid>? SelectedContentItemIds { get; set; }
}

public class RAGQueryResponse
{
    public string Answer { get; set; } = string.Empty;
    public List<RAGSourceDto> Sources { get; set; } = new();
    public string MindsetUsed { get; set; } = string.Empty;
}

public class RAGSourceDto
{
    public Guid ContentItemId { get; set; }
    public string Title { get; set; } = string.Empty;
    public string OriginalUrl { get; set; } = string.Empty;
    public string PlatformType { get; set; } = string.Empty;
    public double SimilarityScore { get; set; }
}
