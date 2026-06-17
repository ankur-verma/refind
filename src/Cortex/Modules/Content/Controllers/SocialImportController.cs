using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Security.Claims;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;
using Cortex.Modules.Content.DTOs;
using Cortex.Modules.Content.Services;
using Cortex.Modules.Content.UseCases;
using Cortex.Shared;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;

namespace Cortex.Modules.Content.Controllers;

[ApiController]
[Route("api/v1/content/social")]
[Authorize]
public class SocialImportController : ControllerBase
{
    private readonly YouTubeImportService _youtubeService;
    private readonly SaveContentUseCase _saveUseCase;
    private readonly ILogger<SocialImportController> _logger;

    public SocialImportController(
        YouTubeImportService youtubeService,
        SaveContentUseCase saveUseCase,
        ILogger<SocialImportController> logger)
    {
        _youtubeService = youtubeService;
        _saveUseCase = saveUseCase;
        _logger = logger;
    }

    private Guid GetUserId() => Guid.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier)!);

    // ──────────────────────── YouTube ────────────────────────

    /// <summary>
    /// Returns the Google OAuth URL the frontend should open for YouTube authorization.
    /// </summary>
    [HttpGet("youtube/auth-url")]
    public IActionResult GetYouTubeAuthUrl()
    {
        // Encode the user's JWT so we can identify them in the callback
        var state = GetUserId().ToString();
        var url = _youtubeService.GetAuthorizationUrl(state);
        return Ok(ApiResponse<object>.Ok(new { authUrl = url }));
    }

    /// <summary>
    /// Google redirects here after user grants access. We exchange the code for a token,
    /// fetch liked videos, and ingest them. Then redirect user back to the frontend.
    /// </summary>
    [HttpGet("youtube/callback")]
    [AllowAnonymous] // Google redirects here without our JWT
    public async Task<IActionResult> YouTubeCallback([FromQuery] string code, [FromQuery] string state, CancellationToken ct)
    {
        if (string.IsNullOrEmpty(code))
            return BadRequest("Missing authorization code.");

        if (!Guid.TryParse(state, out var userId))
            return BadRequest("Invalid state parameter.");

        var accessToken = await _youtubeService.ExchangeCodeForTokenAsync(code, ct);
        if (accessToken == null)
            return BadRequest("Failed to exchange authorization code for access token.");

        var videos = await _youtubeService.GetLikedVideosAsync(accessToken, 100, ct);

        int imported = 0;
        int duplicates = 0;
        foreach (var video in videos)
        {
            var result = await _saveUseCase.ExecuteAsync(userId, new SaveContentRequest { Url = video.Url }, ct);
            if (result.IsSuccess) imported++;
            else if (result.Error == "DUPLICATE_URL") duplicates++;
        }

        _logger.LogInformation("YouTube import for user {UserId}: {Imported} imported, {Duplicates} duplicates, {Total} total", 
            userId, imported, duplicates, videos.Count);

        // Redirect back to frontend with import results
        var redirectUrl = $"http://localhost:5173/?ytImport=true&imported={imported}&duplicates={duplicates}&total={videos.Count}";
        return Redirect(redirectUrl);
    }

    // ──────────────────────── Instagram (Data Export File Upload) ────────────────────────

    /// <summary>
    /// Accepts an Instagram data export JSON file and extracts saved post URLs.
    /// Instagram exports saved posts under: saved_saved_media.json -> ig_saved_media -> [].uri
    /// </summary>
    [HttpPost("instagram/import")]
    [RequestSizeLimit(50 * 1024 * 1024)] // 50 MB limit
    public async Task<IActionResult> ImportInstagramExport(IFormFile file, CancellationToken ct)
    {
        if (file == null || file.Length == 0)
            return BadRequest(ApiResponse<object>.Fail("No file uploaded."));

        var userId = GetUserId();
        var urls = new List<string>();

        try
        {
            using var stream = file.OpenReadStream();
            using var doc = await JsonDocument.ParseAsync(stream, cancellationToken: ct);
            var root = doc.RootElement;

            // Instagram export format: root can be an array of objects with "string_map_data" containing "href"
            // Or it can be structured as "ig_saved_media" array
            if (root.ValueKind == JsonValueKind.Array)
            {
                foreach (var item in root.EnumerateArray())
                {
                    ExtractInstagramUrls(item, urls);
                }
            }
            else if (root.TryGetProperty("ig_saved_media", out var savedMedia))
            {
                foreach (var item in savedMedia.EnumerateArray())
                {
                    ExtractInstagramUrls(item, urls);
                }
            }
        }
        catch (JsonException ex)
        {
            _logger.LogError(ex, "Failed to parse Instagram export file");
            return BadRequest(ApiResponse<object>.Fail("Invalid JSON file. Please upload the correct Instagram data export file."));
        }

        var result = await IngestUrlsAsync(userId, urls, ct);
        return Ok(ApiResponse<object>.Ok(result));
    }

    private void ExtractInstagramUrls(JsonElement item, List<string> urls)
    {
        // Try "string_map_data" -> any key -> "href"
        if (item.TryGetProperty("string_map_data", out var stringMap))
        {
            foreach (var prop in stringMap.EnumerateObject())
            {
                if (prop.Value.TryGetProperty("href", out var href))
                {
                    var url = href.GetString();
                    if (!string.IsNullOrEmpty(url)) urls.Add(url);
                }
            }
        }
        // Try direct "uri" field
        if (item.TryGetProperty("uri", out var uri))
        {
            var url = uri.GetString();
            if (!string.IsNullOrEmpty(url) && url.StartsWith("http")) urls.Add(url);
        }
    }

    // ──────────────────────── LinkedIn (Data Export File Upload) ────────────────────────

    /// <summary>
    /// Accepts a LinkedIn data export file (JSON or CSV) and extracts saved post URLs.
    /// LinkedIn exports can contain URLs in various formats.
    /// </summary>
    [HttpPost("linkedin/import")]
    [RequestSizeLimit(50 * 1024 * 1024)]
    public async Task<IActionResult> ImportLinkedInExport(IFormFile file, CancellationToken ct)
    {
        if (file == null || file.Length == 0)
            return BadRequest(ApiResponse<object>.Fail("No file uploaded."));

        var userId = GetUserId();
        var urls = new List<string>();
        var urlRegex = new System.Text.RegularExpressions.Regex(
            @"https?://[^\s,""'<>\]\)]+",
            System.Text.RegularExpressions.RegexOptions.IgnoreCase);

        try
        {
            using var reader = new StreamReader(file.OpenReadStream());
            var content = await reader.ReadToEndAsync(ct);

            // Try to parse as JSON first
            try
            {
                using var doc = JsonDocument.Parse(content);
                // Extract all URLs from the JSON regardless of structure
                ExtractUrlsFromJson(doc.RootElement, urls, urlRegex);
            }
            catch (JsonException)
            {
                // Not JSON, treat as CSV/text — extract all URLs
                var matches = urlRegex.Matches(content);
                foreach (System.Text.RegularExpressions.Match match in matches)
                {
                    var url = match.Value.TrimEnd(',', '.', ';', ')', ']', '"', '\'');
                    if (url.Contains("linkedin.com") || url.Contains("http"))
                        urls.Add(url);
                }
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to parse LinkedIn export file");
            return BadRequest(ApiResponse<object>.Fail("Failed to parse the uploaded file."));
        }

        // De-duplicate
        urls = urls.Distinct().ToList();
        var result = await IngestUrlsAsync(userId, urls, ct);
        return Ok(ApiResponse<object>.Ok(result));
    }

    private void ExtractUrlsFromJson(JsonElement element, List<string> urls, System.Text.RegularExpressions.Regex urlRegex)
    {
        switch (element.ValueKind)
        {
            case JsonValueKind.String:
                var str = element.GetString() ?? "";
                var matches = urlRegex.Matches(str);
                foreach (System.Text.RegularExpressions.Match match in matches)
                    urls.Add(match.Value.TrimEnd(',', '.', ';', ')', ']'));
                break;
            case JsonValueKind.Array:
                foreach (var item in element.EnumerateArray())
                    ExtractUrlsFromJson(item, urls, urlRegex);
                break;
            case JsonValueKind.Object:
                foreach (var prop in element.EnumerateObject())
                    ExtractUrlsFromJson(prop.Value, urls, urlRegex);
                break;
        }
    }

    // ──────────────────────── Shared ────────────────────────

    private async Task<object> IngestUrlsAsync(Guid userId, List<string> urls, CancellationToken ct)
    {
        int imported = 0, duplicates = 0, errors = 0;

        foreach (var url in urls)
        {
            try
            {
                var result = await _saveUseCase.ExecuteAsync(userId, new SaveContentRequest { Url = url }, ct);
                if (result.IsSuccess) imported++;
                else if (result.Error == "DUPLICATE_URL") duplicates++;
                else errors++;
            }
            catch
            {
                errors++;
            }
        }

        return new { imported, duplicates, errors, total = urls.Count };
    }
}
