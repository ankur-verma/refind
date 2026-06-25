using System.Collections.Generic;

namespace Cortex.Infrastructure.AI;

public class ContentUnderstandingResult
{
    public string Category { get; set; } = string.Empty;
    public string SubCategory { get; set; } = string.Empty;
    public List<string> Topics { get; set; } = new();
    public List<string> Entities { get; set; } = new();
    public string Intent { get; set; } = string.Empty;
    public string Sentiment { get; set; } = string.Empty;
    public List<string> Locations { get; set; } = new();
    public List<string> Products { get; set; } = new();
    public List<string> Brands { get; set; } = new();
    public List<string> People { get; set; } = new();
    public List<string> Events { get; set; } = new();
}
