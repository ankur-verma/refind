using System.Net.Http.Headers;
using System.Text;
using System.Text.Json;
using Cortex.Infrastructure.Settings;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace Cortex.Infrastructure.AI;

public class OllamaExtractionService : IAIExtractionService
{
    private readonly HttpClient _httpClient;
    private readonly AIServiceSettings _settings;
    private readonly ILogger<OllamaExtractionService> _logger;
    private const int MaxInputCharacters = 25000;

    public OllamaExtractionService(HttpClient httpClient, IOptions<AIServiceSettings> options, ILogger<OllamaExtractionService> logger)
    {
        _httpClient = httpClient;
        _settings = options.Value;
        _logger = logger;

        _httpClient.BaseAddress ??= new Uri(_settings.LocalOllama.EndpointUrl);
        _httpClient.Timeout = TimeSpan.FromSeconds(Math.Max(10, _settings.LocalOllama.TimeoutSeconds));
    }

    public async Task<string> GenerateSummaryAsync(string rawText, CancellationToken ct = default)
    {
        if (string.IsNullOrWhiteSpace(rawText)) return string.Empty;

        return await ExecuteWithRetriesAsync(async () =>
        {
            var request = new
            {
                model = _settings.LocalOllama.CompletionModel,
                messages = new object[]
                {
                    new { role = "system", content = "Create a concise, useful summary for a saved learning item. Prefer concrete takeaways over hype." },
                    new { role = "user", content = Truncate(rawText, MaxInputCharacters) }
                },
                stream = false
            };

            using var requestMessage = new HttpRequestMessage(HttpMethod.Post, "api/chat")
            {
                Content = ToJsonContent(request)
            };

            using var response = await _httpClient.SendAsync(requestMessage, ct);
            response.EnsureSuccessStatusCode();

            using var doc = JsonDocument.Parse(await response.Content.ReadAsStringAsync(ct));
            return doc.RootElement.GetProperty("message").GetProperty("content").GetString()?.Trim() ?? string.Empty;
        }, () => "Failed to generate summary.", ct);
    }

    public async Task<float[]> GenerateEmbeddingAsync(string text, CancellationToken ct = default)
    {
        if (string.IsNullOrWhiteSpace(text)) return Array.Empty<float>();

        return await ExecuteWithRetriesAsync(async () =>
        {
            var request = new
            {
                model = _settings.LocalOllama.EmbeddingModel,
                prompt = Truncate(text, MaxInputCharacters)
            };

            using var requestMessage = new HttpRequestMessage(HttpMethod.Post, "api/embeddings")
            {
                Content = ToJsonContent(request)
            };

            using var response = await _httpClient.SendAsync(requestMessage, ct);
            response.EnsureSuccessStatusCode();

            using var doc = JsonDocument.Parse(await response.Content.ReadAsStringAsync(ct));
            var embeddingArray = doc.RootElement.GetProperty("embedding").EnumerateArray();
            return embeddingArray.Select(e => e.GetSingle()).ToArray();
        }, Array.Empty<float>, ct);
    }

    public async Task<ContentUnderstandingResult> ExtractInsightsAsync(string rawText, CancellationToken ct = default)
    {
        if (string.IsNullOrWhiteSpace(rawText))
            return new ContentUnderstandingResult();

        return await ExecuteWithRetriesAsync(async () =>
        {
            var systemPrompt = @"Analyze the provided content and extract structural insights.
Return ONLY a raw JSON object matching this schema exactly:
{
  ""Category"": ""String (e.g. Technology, Food, Travel, Shopping)"",
  ""SubCategory"": ""String"",
  ""Intent"": ""String (e.g. Tutorial, Review, Place To Visit, Potential Purchase)"",
  ""Sentiment"": ""String (e.g. Positive, Neutral, Negative)"",
  ""Topics"": [""Array of Strings""],
  ""Entities"": [""Array of Strings (General concepts, ideas)""],
  ""Locations"": [""Array of Strings (Places, cities)""],
  ""Products"": [""Array of Strings (Specific physical or digital items)""],
  ""Brands"": [""Array of Strings (Companies, brands)""],
  ""People"": [""Array of Strings (Names)""],
  ""Events"": [""Array of Strings (Specific events)""]
}";

            var request = new
            {
                model = _settings.LocalOllama.CompletionModel,
                messages = new[]
                {
                    new { role = "system", content = systemPrompt },
                    new { role = "user", content = Truncate(rawText, MaxInputCharacters) }
                },
                format = "json",
                stream = false,
                options = new { temperature = 0.1 }
            };

            using var requestMessage = new HttpRequestMessage(HttpMethod.Post, "api/chat")
            {
                Content = ToJsonContent(request)
            };

            using var response = await _httpClient.SendAsync(requestMessage, ct);
            response.EnsureSuccessStatusCode();

            using var doc = JsonDocument.Parse(await response.Content.ReadAsStringAsync(ct));
            var content = doc.RootElement.GetProperty("message").GetProperty("content").GetString();

            if (string.IsNullOrWhiteSpace(content)) return new ContentUnderstandingResult();
            
            try
            {
                return JsonSerializer.Deserialize<ContentUnderstandingResult>(content, new JsonSerializerOptions { PropertyNameCaseInsensitive = true }) 
                       ?? new ContentUnderstandingResult();
            }
            catch (JsonException)
            {
                return new ContentUnderstandingResult();
            }
        }, () => new ContentUnderstandingResult(), ct);
    }

    public async Task<SearchIntentResult> ExtractSearchIntentAsync(string query, CancellationToken ct = default)
    {
        if (string.IsNullOrWhiteSpace(query))
            return new SearchIntentResult();

        return await ExecuteWithRetriesAsync(async () =>
        {
            var systemPrompt = @"Analyze the user's natural language search query.
Return ONLY a raw JSON object matching this schema exactly:
{
  ""IsKnowledgeGraphQuery"": true/false,
  ""IsMemoryQuery"": true/false,
  ""IsCollectionQuery"": true/false,
  ""Locations"": [""Array of Strings (Places, cities)""],
  ""Entities"": [""Array of Strings (Things, gadgets, objects, e.g. cafe, drone)""],
  ""Topics"": [""Array of Strings""],
  ""Collections"": [""Array of Strings (Collection names)""],
  ""Intents"": [""Array of Strings (e.g. 'buy', 'visit', 'read')""],
  ""TimeFrame"": ""String""
}";

            var request = new
            {
                model = _settings.LocalOllama.CompletionModel,
                messages = new[]
                {
                    new { role = "system", content = systemPrompt },
                    new { role = "user", content = Truncate(query, MaxInputCharacters) }
                },
                format = "json",
                stream = false,
                options = new { temperature = 0.1 }
            };

            using var requestMessage = new HttpRequestMessage(HttpMethod.Post, "api/chat")
            {
                Content = ToJsonContent(request)
            };

            using var response = await _httpClient.SendAsync(requestMessage, ct);
            response.EnsureSuccessStatusCode();

            using var doc = JsonDocument.Parse(await response.Content.ReadAsStringAsync(ct));
            var content = doc.RootElement.GetProperty("message").GetProperty("content").GetString();

            if (string.IsNullOrWhiteSpace(content)) return new SearchIntentResult();
            
            try
            {
                return JsonSerializer.Deserialize<SearchIntentResult>(content, new JsonSerializerOptions { PropertyNameCaseInsensitive = true }) 
                       ?? new SearchIntentResult();
            }
            catch (JsonException)
            {
                return new SearchIntentResult();
            }
        }, () => new SearchIntentResult(), ct);
    }

    public async Task<List<(string Description, string Type, int Order)>> ExtractActionsAsync(string rawText, CancellationToken ct = default)
    {
        if (string.IsNullOrWhiteSpace(rawText)) return new();

        return await ExecuteWithRetriesAsync(async () =>
        {
            var request = new
            {
                model = _settings.LocalOllama.CompletionModel,
                messages = new object[]
                {
                    new
                    {
                        role = "system",
                        content = "Extract actionable items from the text. Respond strictly in JSON array format: [{\"description\": \"...\", \"type\": \"Task|Idea|ToRead\"}]."
                    },
                    new { role = "user", content = Truncate(rawText, MaxInputCharacters) }
                },
                stream = false,
                format = "json"
            };

            using var requestMessage = new HttpRequestMessage(HttpMethod.Post, "api/chat")
            {
                Content = ToJsonContent(request)
            };

            using var response = await _httpClient.SendAsync(requestMessage, ct);
            response.EnsureSuccessStatusCode();

            using var doc = JsonDocument.Parse(await response.Content.ReadAsStringAsync(ct));
            var jsonContent = doc.RootElement.GetProperty("message").GetProperty("content").GetString();

            if (string.IsNullOrWhiteSpace(jsonContent))
                return new List<(string, string, int)>();

            using var actionsDoc = JsonDocument.Parse(jsonContent);
            var results = new List<(string Description, string Type, int Order)>();
            int order = 1;

            if (actionsDoc.RootElement.ValueKind == JsonValueKind.Array)
            {
                foreach (var el in actionsDoc.RootElement.EnumerateArray())
                {
                    var desc = el.TryGetProperty("description", out var d) ? d.GetString() : null;
                    var type = el.TryGetProperty("type", out var t) ? t.GetString() : "Idea";

                    if (!string.IsNullOrWhiteSpace(desc))
                    {
                        results.Add((desc, type ?? "Idea", order++));
                    }
                }
            }

            return results;
        }, () => new List<(string Description, string Type, int Order)>(), ct);
    }

    public async Task<string> GetChatCompletionAsync(string systemMessage, string userMessage, CancellationToken ct = default)
    {
        return await ExecuteWithRetriesAsync(async () =>
        {
            var request = new
            {
                model = _settings.LocalOllama.CompletionModel,
                messages = new object[]
                {
                    new { role = "system", content = systemMessage },
                    new { role = "user", content = userMessage }
                },
                stream = false
            };

            using var requestMessage = new HttpRequestMessage(HttpMethod.Post, "api/chat")
            {
                Content = ToJsonContent(request)
            };

            using var response = await _httpClient.SendAsync(requestMessage, ct);
            response.EnsureSuccessStatusCode();

            using var doc = JsonDocument.Parse(await response.Content.ReadAsStringAsync(ct));
            return doc.RootElement.GetProperty("message").GetProperty("content").GetString()?.Trim() ?? string.Empty;
        }, () => string.Empty, ct);
    }

    public async Task<string> DescribeVideoFramesAsync(List<VideoKeyframe> keyframes, CancellationToken ct = default)
    {
        if (keyframes == null || !keyframes.Any())
            return string.Empty;

        return await ExecuteWithRetriesAsync(async () =>
        {
            var base64Images = new List<string>();
            foreach (var kf in keyframes.OrderBy(k => k.Timestamp))
            {
                if (!string.IsNullOrWhiteSpace(kf.Base64Data))
                {
                    base64Images.Add(kf.Base64Data);
                }
            }

            var request = new
            {
                model = _settings.LocalOllama.VisionModel,
                messages = new object[]
                {
                    new 
                    { 
                        role = "user", 
                        content = "These are sequential frames from a video. Provide a chronological summary of what is happening visually.",
                        images = base64Images.ToArray()
                    }
                },
                stream = false
            };

            using var requestMessage = new HttpRequestMessage(HttpMethod.Post, "api/chat")
            {
                Content = ToJsonContent(request)
            };

            using var response = await _httpClient.SendAsync(requestMessage, ct);
            response.EnsureSuccessStatusCode();

            using var doc = JsonDocument.Parse(await response.Content.ReadAsStringAsync(ct));
            return doc.RootElement.GetProperty("message").GetProperty("content").GetString()?.Trim() ?? string.Empty;
        }, () => string.Empty, ct);
    }

    public Task<string> AnalyzeVideoFileAsync(string mp4FilePath, string prompt, CancellationToken ct = default)
    {
        throw new NotSupportedException("Ollama does not support native video file analysis. Use local whisper + keyframes.");
    }

    private async Task<T> ExecuteWithRetriesAsync<T>(Func<Task<T>> operation, Func<T> fallback, CancellationToken ct)
    {
        var maxAttempts = Math.Max(1, _settings.LocalOllama.MaxRetries);
        for (var attempt = 1; attempt <= maxAttempts; attempt++)
        {
            try
            {
                return await operation();
            }
            catch (Exception ex) when (attempt < maxAttempts && !ct.IsCancellationRequested)
            {
                _logger.LogWarning(ex, "Ollama API attempt {Attempt} failed. Retrying...", attempt);
                await Task.Delay(TimeSpan.FromSeconds(Math.Pow(2, attempt)), ct);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Ollama API failed after {MaxAttempts} attempts.", maxAttempts);
            }
        }
        return fallback();
    }

    private static StringContent ToJsonContent<T>(T request)
        => new(JsonSerializer.Serialize(request), Encoding.UTF8, "application/json");

    private static string Truncate(string value, int maxLength)
    {
        if (string.IsNullOrEmpty(value)) return value;
        return value.Length <= maxLength ? value : value[..maxLength];
    }
}
