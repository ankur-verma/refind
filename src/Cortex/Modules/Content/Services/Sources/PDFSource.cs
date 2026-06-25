using Cortex.Modules.Content.Domain;
using Cortex.Shared.Enums;
using Microsoft.Extensions.Logging;
using static Cortex.Modules.Content.Services.Sources.ContentExtractionHelpers;

namespace Cortex.Modules.Content.Services.Sources;

public class PDFSource : IContentSource
{
    private readonly HttpClient _httpClient;
    private readonly ILogger<PDFSource> _logger;

    public PlatformType PlatformType => PlatformType.PDF;

    public PDFSource(HttpClient httpClient, ILogger<PDFSource> logger)
    {
        _httpClient = PrepareClient(httpClient);
        _logger = logger;
    }

    public bool CanHandle(string url)
    {
        if (!Uri.TryCreate(url, UriKind.Absolute, out var uri)) return false;
        return uri.AbsolutePath.EndsWith(".pdf", StringComparison.OrdinalIgnoreCase);
    }

    public async Task<ContentExtractionResult> ExtractAsync(string url, CancellationToken ct = default)
    {
        _logger.LogInformation("Processing PDF: {Url}", url);

        var bytes = await _httpClient.GetByteArrayAsync(url, ct);
        
        var result = new ContentExtractionResult();
        result.Title = CreateTitleFromUrl(url);
        result.Transcript = ExtractReadablePdfText(bytes);
        result.Tags = new List<string> { "document", "pdf" };

        return result;
    }
}
