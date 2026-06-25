namespace Cortex.Modules.Content.Domain;

public class ContentExtractionResult
{
    public string Title { get; set; } = string.Empty;
    public string Description { get; set; } = string.Empty;
    public string Author { get; set; } = string.Empty;
    public List<string> Media { get; set; } = new();
    public string Transcript { get; set; } = string.Empty;
    public string Location { get; set; } = string.Empty;
    public List<string> Products { get; set; } = new();
    public Dictionary<string, string> Metadata { get; set; } = new();
    public List<string> Tags { get; set; } = new();
}
