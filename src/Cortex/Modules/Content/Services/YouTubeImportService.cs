using System;
using System.Collections.Generic;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;

namespace Cortex.Modules.Content.Services;

public class YouTubeVideoInfo
{
    public string VideoId { get; set; } = string.Empty;
    public string Title { get; set; } = string.Empty;
    public string Url => $"https://www.youtube.com/watch?v={VideoId}";
    public string ThumbnailUrl { get; set; } = string.Empty;
    public string Description { get; set; } = string.Empty;
}

public class YouTubeImportService
{
    private readonly HttpClient _httpClient;
    private readonly IConfiguration _configuration;
    private readonly ILogger<YouTubeImportService> _logger;

    public YouTubeImportService(HttpClient httpClient, IConfiguration configuration, ILogger<YouTubeImportService> logger)
    {
        _httpClient = httpClient;
        _configuration = configuration;
        _logger = logger;
    }

    /// <summary>
    /// Builds the Google OAuth authorization URL for YouTube access.
    /// </summary>
    public string GetAuthorizationUrl(string state)
    {
        var clientId = _configuration["GoogleOAuth:ClientId"];
        var redirectUri = _configuration["YouTube:RedirectUri"];
        var scopes = _configuration["YouTube:Scopes"];

        return $"https://accounts.google.com/o/oauth2/v2/auth?" +
               $"client_id={Uri.EscapeDataString(clientId!)}" +
               $"&redirect_uri={Uri.EscapeDataString(redirectUri!)}" +
               $"&response_type=code" +
               $"&scope={Uri.EscapeDataString(scopes!)}" +
               $"&access_type=offline" +
               $"&state={Uri.EscapeDataString(state)}" +
               $"&prompt=consent";
    }

    /// <summary>
    /// Exchanges the authorization code for an access token.
    /// </summary>
    public async Task<string?> ExchangeCodeForTokenAsync(string code, CancellationToken ct = default)
    {
        var clientId = _configuration["GoogleOAuth:ClientId"];
        var clientSecret = _configuration["GoogleOAuth:ClientSecret"];
        var redirectUri = _configuration["YouTube:RedirectUri"];

        var body = new FormUrlEncodedContent(new[]
        {
            new KeyValuePair<string, string>("code", code),
            new KeyValuePair<string, string>("client_id", clientId!),
            new KeyValuePair<string, string>("client_secret", clientSecret!),
            new KeyValuePair<string, string>("redirect_uri", redirectUri!),
            new KeyValuePair<string, string>("grant_type", "authorization_code")
        });

        var response = await _httpClient.PostAsync("https://oauth2.googleapis.com/token", body, ct);
        var json = await response.Content.ReadAsStringAsync(ct);

        if (!response.IsSuccessStatusCode)
        {
            _logger.LogError("YouTube token exchange failed: {Response}", json);
            return null;
        }

        using var doc = JsonDocument.Parse(json);
        return doc.RootElement.GetProperty("access_token").GetString();
    }

    /// <summary>
    /// Fetches the user's liked videos from YouTube.
    /// </summary>
    public async Task<List<YouTubeVideoInfo>> GetLikedVideosAsync(string accessToken, int maxResults = 50, CancellationToken ct = default)
    {
        var videos = new List<YouTubeVideoInfo>();
        string? pageToken = null;

        do
        {
            var url = $"https://www.googleapis.com/youtube/v3/videos?part=snippet&myRating=like&maxResults={Math.Min(maxResults, 50)}";
            if (pageToken != null) url += $"&pageToken={pageToken}";

            using var request = new HttpRequestMessage(HttpMethod.Get, url);
            request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", accessToken);

            var response = await _httpClient.SendAsync(request, ct);
            var json = await response.Content.ReadAsStringAsync(ct);

            if (!response.IsSuccessStatusCode)
            {
                _logger.LogError("YouTube liked videos fetch failed: {Response}", json);
                break;
            }

            using var doc = JsonDocument.Parse(json);
            var root = doc.RootElement;

            if (root.TryGetProperty("items", out var items))
            {
                foreach (var item in items.EnumerateArray())
                {
                    var snippet = item.GetProperty("snippet");
                    var videoId = item.GetProperty("id").GetString() ?? "";
                    var thumbnails = snippet.GetProperty("thumbnails");
                    var thumbUrl = thumbnails.TryGetProperty("medium", out var medium)
                        ? medium.GetProperty("url").GetString() ?? ""
                        : "";

                    videos.Add(new YouTubeVideoInfo
                    {
                        VideoId = videoId,
                        Title = snippet.GetProperty("title").GetString() ?? "",
                        ThumbnailUrl = thumbUrl,
                        Description = snippet.TryGetProperty("description", out var desc) ? desc.GetString() ?? "" : ""
                    });
                }
            }

            pageToken = root.TryGetProperty("nextPageToken", out var npt) ? npt.GetString() : null;

            // Safety limit: don't fetch more than 200 videos in one go
            if (videos.Count >= 200) break;

        } while (pageToken != null);

        return videos;
    }
}
