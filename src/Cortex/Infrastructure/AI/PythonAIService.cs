using Cortex.Infrastructure.Settings;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using System;
using System.Collections.Generic;
using System.Net.Http;
using System.Net.Http.Json;
using System.Text;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;

namespace Cortex.Infrastructure.AI;

public class PythonAIService : IPythonAIService
{
    private readonly HttpClient _httpClient;
    private readonly AIServiceSettings _aiSettings;
    private readonly IConfiguration _configuration;
    private readonly ILogger<PythonAIService> _logger;

    public PythonAIService(
        HttpClient httpClient,
        IOptions<AIServiceSettings> aiOptions,
        IConfiguration configuration,
        ILogger<PythonAIService> logger)
    {
        _httpClient = httpClient;
        _aiSettings = aiOptions.Value;
        _configuration = configuration;
        _logger = logger;

        _httpClient.BaseAddress ??= new Uri("http://localhost:8000/");
        _httpClient.Timeout = TimeSpan.FromMinutes(5); // Video analysis can take time
    }

    private string GetPostgreSqlUri()
    {
        var connStr = _configuration.GetConnectionString("CortexDatabase");
        if (string.IsNullOrWhiteSpace(connStr))
            return string.Empty;

        try
        {
            var parts = connStr.Split(';', StringSplitOptions.RemoveEmptyEntries);
            string host = "localhost", port = "5432", db = "", user = "", pass = "";
            foreach (var part in parts)
            {
                var kv = part.Split('=', 2);
                if (kv.Length < 2) continue;
                var key = kv[0].Trim().ToLowerInvariant();
                var val = kv[1].Trim();
                if (key == "host" || key == "server") host = val;
                else if (key == "port") port = val;
                else if (key == "database" || key == "db") db = val;
                else if (key == "username" || key == "user" || key == "user id") user = val;
                else if (key == "password" || key == "pwd") pass = val;
            }
            return $"postgresql://{user}:{pass}@{host}:{port}/{db}";
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to parse PostgreSQL connection string");
            return string.Empty;
        }
    }

    private string GetGeminiApiKey() => _aiSettings.Gemini.OpenAIApiKey;

    public async Task<List<VideoSegmentDto>> AnalyzeVideoAsync(Guid contentItemId, string? url, string? mp4FilePath, string? rawText, CancellationToken ct = default)
    {
        var request = new
        {
            url = url,
            mp4_file_path = mp4FilePath,
            raw_text = rawText,
            api_key = GetGeminiApiKey(),
            completion_model = _aiSettings.Gemini.CompletionModel
        };

        try
        {
            using var response = await _httpClient.PostAsJsonAsync("api/ai/video/analyze", request, ct);
            if (!response.IsSuccessStatusCode)
            {
                var err = await response.Content.ReadAsStringAsync(ct);
                _logger.LogError("Python AI service video analysis failed: {Error}", err);
                return new List<VideoSegmentDto>();
            }

            var result = await response.Content.ReadFromJsonAsync<VideoAnalyzeResponse>(cancellationToken: ct);
            return result?.Segments ?? new List<VideoSegmentDto>();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to communicate with Python AI video analysis endpoint");
            return new List<VideoSegmentDto>();
        }
    }

    public async Task<bool> UpdateUserProfileAsync(Guid userId, CancellationToken ct = default)
    {
        var request = new
        {
            user_id = userId.ToString(),
            db_url = GetPostgreSqlUri(),
            api_key = GetGeminiApiKey(),
            completion_model = _aiSettings.Gemini.CompletionModel
        };

        try
        {
            using var response = await _httpClient.PostAsJsonAsync("api/ai/profile/update", request, ct);
            if (!response.IsSuccessStatusCode)
            {
                var err = await response.Content.ReadAsStringAsync(ct);
                _logger.LogError("Python AI user profile update failed: {Error}", err);
                return false;
            }

            return true;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to communicate with Python AI profile update endpoint");
            return false;
        }
    }

    public async Task<List<AutoCollectionSuggestionDto>> SuggestCollectionsAsync(Guid userId, CancellationToken ct = default)
    {
        var request = new
        {
            user_id = userId.ToString(),
            db_url = GetPostgreSqlUri(),
            api_key = GetGeminiApiKey(),
            completion_model = _aiSettings.Gemini.CompletionModel
        };

        try
        {
            using var response = await _httpClient.PostAsJsonAsync("api/ai/collections/suggest", request, ct);
            if (!response.IsSuccessStatusCode)
            {
                var err = await response.Content.ReadAsStringAsync(ct);
                _logger.LogError("Python AI suggest collections failed: {Error}", err);
                return new List<AutoCollectionSuggestionDto>();
            }

            var result = await response.Content.ReadFromJsonAsync<SuggestCollectionsResponse>(cancellationToken: ct);
            return result?.SuggestedCollections ?? new List<AutoCollectionSuggestionDto>();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to communicate with Python AI suggest collections endpoint");
            return new List<AutoCollectionSuggestionDto>();
        }
    }

    public async Task<List<RecommendedItemDto>> GetRecommendationsAsync(Guid userId, int limit = 5, CancellationToken ct = default)
    {
        var request = new
        {
            user_id = userId.ToString(),
            db_url = GetPostgreSqlUri(),
            api_key = GetGeminiApiKey(),
            limit = limit
        };

        try
        {
            using var response = await _httpClient.PostAsJsonAsync("api/ai/recommend", request, ct);
            if (!response.IsSuccessStatusCode)
            {
                var err = await response.Content.ReadAsStringAsync(ct);
                _logger.LogError("Python AI recommendation failed: {Error}", err);
                return new List<RecommendedItemDto>();
            }

            var result = await response.Content.ReadFromJsonAsync<RecommendResponse>(cancellationToken: ct);
            return result?.Recommendations ?? new List<RecommendedItemDto>();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to communicate with Python AI recommend endpoint");
            return new List<RecommendedItemDto>();
        }
    }

    public async Task<HybridRecommendationResponse?> GetHybridRecommendationsAsync(Guid userId, CancellationToken ct = default)
    {
        var request = new
        {
            user_id = userId.ToString(),
            db_url = GetPostgreSqlUri(),
            api_key = GetGeminiApiKey()
        };

        try
        {
            using var response = await _httpClient.PostAsJsonAsync("api/ml/recommend_hybrid", request, ct);
            if (!response.IsSuccessStatusCode)
            {
                var err = await response.Content.ReadAsStringAsync(ct);
                _logger.LogError("Python AI hybrid recommendation failed: {Error}", err);
                return null;
            }

            return await response.Content.ReadFromJsonAsync<HybridRecommendationResponse>(cancellationToken: ct);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to communicate with Python AI hybrid recommend endpoint");
            return null;
        }
    }

    public async Task<bool> GenerateUserEmbeddingAsync(Guid userId, CancellationToken ct = default)
    {
        var request = new
        {
            user_id = userId.ToString(),
            db_url = GetPostgreSqlUri(),
            api_key = GetGeminiApiKey()
        };

        try
        {
            using var response = await _httpClient.PostAsJsonAsync("api/ml/embed/user", request, ct);
            if (!response.IsSuccessStatusCode)
            {
                var err = await response.Content.ReadAsStringAsync(ct);
                _logger.LogError("Python ML generate embedding failed: {Error}", err);
                return false;
            }

            return true;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to communicate with Python ML embed endpoint");
            return false;
        }
    }

    public async Task<bool> TriggerClusteringAsync(CancellationToken ct = default)
    {
        var request = new
        {
            db_url = GetPostgreSqlUri(),
            api_key = GetGeminiApiKey()
        };

        try
        {
            using var response = await _httpClient.PostAsJsonAsync("api/ml/cluster", request, ct);
            if (!response.IsSuccessStatusCode)
            {
                var err = await response.Content.ReadAsStringAsync(ct);
                _logger.LogError("Python ML cluster users failed: {Error}", err);
                return false;
            }

            return true;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to communicate with Python ML cluster endpoint");
            return false;
        }
    }

    private class VideoAnalyzeResponse
    {
        public List<VideoSegmentDto>? Segments { get; set; }
    }

    private class SuggestCollectionsResponse
    {
        public List<AutoCollectionSuggestionDto>? SuggestedCollections { get; set; }
    }

    private class RecommendResponse
    {
        public List<RecommendedItemDto>? Recommendations { get; set; }
    }

    private class DynamicReelResponse
    {
        public bool Success { get; set; }
        public List<DynamicReelItemDto>? Data { get; set; }
    }

    public async Task<List<DynamicReelItemDto>> GetDynamicReelAsync(Guid userId, int limit = 5, CancellationToken ct = default)
    {
        var request = new
        {
            user_id = userId.ToString(),
            limit = limit,
            db_url = GetPostgreSqlUri()
        };

        try
        {
            using var response = await _httpClient.PostAsJsonAsync("api/ml/dynamic_reel", request, ct);
            if (!response.IsSuccessStatusCode)
            {
                var err = await response.Content.ReadAsStringAsync(ct);
                _logger.LogError("Python ML dynamic reel failed: {Error}", err);
                return new List<DynamicReelItemDto>();
            }

            var result = await response.Content.ReadFromJsonAsync<DynamicReelResponse>(cancellationToken: ct);
            return result?.Data ?? new List<DynamicReelItemDto>();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to communicate with Python ML dynamic reel endpoint");
            return new List<DynamicReelItemDto>();
        }
    }

    public async Task<bool> EmbedVideoSegmentsAsync(CancellationToken ct = default)
    {
        var request = new
        {
            db_url = GetPostgreSqlUri()
        };

        try
        {
            using var response = await _httpClient.PostAsJsonAsync("api/ml/embed/segments", request, ct);
            if (!response.IsSuccessStatusCode)
            {
                var err = await response.Content.ReadAsStringAsync(ct);
                _logger.LogError("Python ML embed segments failed: {Error}", err);
                return false;
            }

            return true;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to communicate with Python ML embed segments endpoint");
            return false;
        }
    }

    public async Task<SynthesizeStartResponse> SynthesizeAIVideoAsync(List<string> transcripts, CancellationToken ct = default)
    {
        var request = new
        {
            transcripts = transcripts,
            api_key = GetGeminiApiKey()
        };

        try
        {
            using var response = await _httpClient.PostAsJsonAsync("api/ml/synthesize", request, ct);
            if (!response.IsSuccessStatusCode)
            {
                var err = await response.Content.ReadAsStringAsync(ct);
                _logger.LogError("Python ML synthesize video failed: {Error}", err);
                return new SynthesizeStartResponse { TaskId = null };
            }

            var result = await response.Content.ReadFromJsonAsync<SynthesizeStartResponse>(cancellationToken: ct);
            return result ?? new SynthesizeStartResponse { TaskId = null };
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to communicate with Python ML synthesize endpoint");
            return new SynthesizeStartResponse { TaskId = null };
        }
    }

    public async Task<SynthesizeStatusResponse?> GetSynthesisStatusAsync(string taskId, CancellationToken ct = default)
    {
        try
        {
            using var response = await _httpClient.GetAsync($"api/ml/synthesize/status?task_id={taskId}", ct);
            if (!response.IsSuccessStatusCode)
            {
                return null;
            }

            return await response.Content.ReadFromJsonAsync<SynthesizeStatusResponse>(cancellationToken: ct);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to communicate with Python ML synthesize status endpoint");
            return null;
        }
    }
}
