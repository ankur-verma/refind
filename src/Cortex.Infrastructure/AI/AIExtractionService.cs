using Cortex.Application.Interfaces.Services;
using Cortex.Infrastructure.Settings;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using System.Net.Http.Headers;
using System.Security.Cryptography;
using System.Text;
using System.Text.Json;

namespace Cortex.Infrastructure.AI;

public class AIExtractionService : IAIExtractionService
{
    private const int EmbeddingDimensions = 1536;
    private const int MaxInputCharacters = 24000;

    private readonly HttpClient _httpClient;
    private readonly AIServiceSettings _settings;
    private readonly ILogger<AIExtractionService> _logger;

    public AIExtractionService(HttpClient httpClient, IOptions<AIServiceSettings> options, ILogger<AIExtractionService> logger)
    {
        _httpClient = httpClient;
        _settings = options.Value;
        _logger = logger;

        _httpClient.BaseAddress ??= new Uri("https://api.openai.com/v1/");
        _httpClient.Timeout = TimeSpan.FromSeconds(Math.Max(10, _settings.TimeoutSeconds));
        if (!string.IsNullOrWhiteSpace(_settings.OpenAIApiKey))
            _httpClient.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", _settings.OpenAIApiKey);
    }

    public async Task<string> GenerateSummaryAsync(string rawText, CancellationToken ct = default)
    {
        if (string.IsNullOrWhiteSpace(rawText))
            return "No readable text was extracted from this content.";

        if (!HasApiKey())
            return CreateLocalSummary(rawText);

        return await ExecuteWithRetriesAsync(async () =>
        {
            var request = new
            {
                model = _settings.CompletionModel,
                messages = new object[]
                {
                    new { role = "system", content = "Create a concise, useful summary for a saved learning item. Prefer concrete takeaways over hype." },
                    new { role = "user", content = Truncate(rawText, MaxInputCharacters) }
                },
                temperature = 0.2,
                max_tokens = 350
            };

            using var response = await _httpClient.PostAsync("chat/completions", ToJsonContent(request), ct);
            response.EnsureSuccessStatusCode();
            using var document = JsonDocument.Parse(await response.Content.ReadAsStringAsync(ct));
            return document.RootElement
                .GetProperty("choices")[0]
                .GetProperty("message")
                .GetProperty("content")
                .GetString()?
                .Trim() ?? CreateLocalSummary(rawText);
        }, () => CreateLocalSummary(rawText), ct);
    }

    public async Task<float[]> GenerateEmbeddingAsync(string text, CancellationToken ct = default)
    {
        if (string.IsNullOrWhiteSpace(text))
            return CreateLocalEmbedding("empty");

        if (!HasApiKey())
            return CreateLocalEmbedding(text);

        return await ExecuteWithRetriesAsync(async () =>
        {
            var request = new
            {
                model = _settings.EmbeddingModel,
                input = Truncate(text, MaxInputCharacters)
            };

            using var response = await _httpClient.PostAsync("embeddings", ToJsonContent(request), ct);
            response.EnsureSuccessStatusCode();
            using var document = JsonDocument.Parse(await response.Content.ReadAsStringAsync(ct));
            var values = document.RootElement.GetProperty("data")[0].GetProperty("embedding");
            var embedding = new float[values.GetArrayLength()];
            var index = 0;
            foreach (var value in values.EnumerateArray())
                embedding[index++] = value.GetSingle();
            return embedding;
        }, () => CreateLocalEmbedding(text), ct);
    }

    public async Task<List<(string Description, string Type, int Order)>> ExtractActionsAsync(string rawText, CancellationToken ct = default)
    {
        if (string.IsNullOrWhiteSpace(rawText))
            return new List<(string Description, string Type, int Order)>();

        if (!HasApiKey())
            return CreateLocalActions(rawText);

        return await ExecuteWithRetriesAsync(async () =>
        {
            var request = new
            {
                model = _settings.CompletionModel,
                messages = new object[]
                {
                    new
                    {
                        role = "system",
                        content = "Extract up to seven concrete learner actions from content. Return JSON only: {\"actions\":[{\"description\":\"...\",\"type\":\"Instruction|Tool\",\"order\":1}]}"
                    },
                    new { role = "user", content = Truncate(rawText, MaxInputCharacters) }
                },
                response_format = new { type = "json_object" },
                temperature = 0.1,
                max_tokens = 700
            };

            using var response = await _httpClient.PostAsync("chat/completions", ToJsonContent(request), ct);
            response.EnsureSuccessStatusCode();
            using var document = JsonDocument.Parse(await response.Content.ReadAsStringAsync(ct));
            var content = document.RootElement
                .GetProperty("choices")[0]
                .GetProperty("message")
                .GetProperty("content")
                .GetString();

            return ParseActions(content) ?? CreateLocalActions(rawText);
        }, () => CreateLocalActions(rawText), ct);
    }

    private async Task<T> ExecuteWithRetriesAsync<T>(Func<Task<T>> operation, Func<T> fallback, CancellationToken ct)
    {
        var maxAttempts = Math.Max(1, _settings.MaxRetries);
        for (var attempt = 1; attempt <= maxAttempts; attempt++)
        {
            try
            {
                return await operation();
            }
            catch (OperationCanceledException) when (ct.IsCancellationRequested)
            {
                throw;
            }
            catch (Exception ex) when (attempt < maxAttempts)
            {
                _logger.LogWarning(ex, "AI service attempt {Attempt} failed; retrying", attempt);
                await Task.Delay(TimeSpan.FromMilliseconds(250 * attempt), ct);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "AI service failed after {Attempts} attempts; using local fallback", maxAttempts);
            }
        }

        return fallback();
    }

    private bool HasApiKey() => !string.IsNullOrWhiteSpace(_settings.OpenAIApiKey);

    private static StringContent ToJsonContent<T>(T request)
        => new(JsonSerializer.Serialize(request), Encoding.UTF8, "application/json");

    private static string CreateLocalSummary(string rawText)
    {
        var sentences = rawText
            .Replace('\r', ' ')
            .Replace('\n', ' ')
            .Split('.', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries)
            .Where(s => s.Length > 20)
            .Take(3)
            .ToList();

        if (sentences.Count == 0)
            return Truncate(rawText.Trim(), 500);

        return string.Join(". ", sentences) + ".";
    }

    private static List<(string Description, string Type, int Order)> CreateLocalActions(string rawText)
    {
        var sentences = rawText
            .Replace('\r', ' ')
            .Replace('\n', ' ')
            .Split('.', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries)
            .Where(s => s.Length is >= 20 and <= 220)
            .Take(5)
            .Select((sentence, index) => (Description: sentence.Trim(), Type: "Instruction", Order: index + 1))
            .ToList();

        if (sentences.Count == 0)
            sentences.Add((Description: "Review the saved content and capture one useful takeaway.", Type: "Instruction", Order: 1));

        return sentences;
    }

    private static List<(string Description, string Type, int Order)>? ParseActions(string? content)
    {
        if (string.IsNullOrWhiteSpace(content))
            return null;

        try
        {
            using var document = JsonDocument.Parse(content);
            if (!document.RootElement.TryGetProperty("actions", out var actionsElement) || actionsElement.ValueKind != JsonValueKind.Array)
                return null;

            var actions = new List<(string Description, string Type, int Order)>();
            foreach (var action in actionsElement.EnumerateArray())
            {
                var description = action.TryGetProperty("description", out var descriptionElement)
                    ? descriptionElement.GetString()
                    : null;
                if (string.IsNullOrWhiteSpace(description))
                    continue;

                var type = action.TryGetProperty("type", out var typeElement)
                    ? typeElement.GetString()
                    : "Instruction";
                var order = action.TryGetProperty("order", out var orderElement) && orderElement.TryGetInt32(out var parsedOrder)
                    ? parsedOrder
                    : actions.Count + 1;

                actions.Add((description.Trim(), type == "Tool" ? "Tool" : "Instruction", order));
            }

            return actions;
        }
        catch (JsonException)
        {
            return null;
        }
    }

    private static float[] CreateLocalEmbedding(string text)
    {
        var embedding = new float[EmbeddingDimensions];
        var normalized = text.ToLowerInvariant();
        var tokens = normalized.Split(new[] { ' ', '\t', '\r', '\n', '.', ',', ';', ':', '!', '?', '/', '\\', '-', '_' }, StringSplitOptions.RemoveEmptyEntries);

        foreach (var token in tokens.DefaultIfEmpty(normalized))
        {
            var hash = SHA256.HashData(Encoding.UTF8.GetBytes(token));
            for (var i = 0; i < hash.Length; i += 2)
            {
                var index = ((hash[i] << 8) + hash[i + 1]) % EmbeddingDimensions;
                embedding[index] += 1f;
            }
        }

        var magnitude = Math.Sqrt(embedding.Sum(v => v * v));
        if (magnitude <= 0) return embedding;

        for (var i = 0; i < embedding.Length; i++)
            embedding[i] = (float)(embedding[i] / magnitude);

        return embedding;
    }

    private static string Truncate(string value, int maxLength)
        => value.Length <= maxLength ? value : value[..maxLength];
}
