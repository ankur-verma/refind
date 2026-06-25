using System.Collections.Generic;

namespace Cortex.Infrastructure.AI;

public class SearchIntentResult
{
    public bool IsKnowledgeGraphQuery { get; set; }
    public bool IsMemoryQuery { get; set; }
    public bool IsCollectionQuery { get; set; }
    public List<string> Locations { get; set; } = new();
    public List<string> Entities { get; set; } = new();
    public List<string> Topics { get; set; } = new();
    public List<string> Collections { get; set; } = new();
    public List<string> Intents { get; set; } = new();
    public string TimeFrame { get; set; } = string.Empty;
}
