using Cortex.Modules.Auth.Services;
using Cortex.Infrastructure.AI;
using Cortex.Infrastructure.Caching;
using Cortex.Infrastructure.Messaging;
using Cortex.Infrastructure.Settings;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using System.Net.Http.Headers;
using System.Security.Cryptography;
using System.Text;
using System.Text.Json;

namespace Cortex.Infrastructure.AI;

public class GeminiExtractionService : IAIExtractionService
{
    private const int EmbeddingDimensions = 1536;
    private const int MaxInputCharacters = 24000;

    private readonly HttpClient _httpClient;
    private readonly AIServiceSettings _settings;
    private readonly ILogger<GeminiExtractionService> _logger;

    public GeminiExtractionService(HttpClient httpClient, IOptions<AIServiceSettings> options, ILogger<GeminiExtractionService> logger)
    {
        _httpClient = httpClient;
        _settings = options.Value;
        _logger = logger;

        _httpClient.BaseAddress ??= new Uri(_settings.Gemini.EndpointUrl);
        _httpClient.Timeout = TimeSpan.FromSeconds(Math.Max(10, _settings.Gemini.TimeoutSeconds));
        // Do not set DefaultRequestHeaders.Authorization here, inject it per request instead.
    }

    public async Task<string> GenerateSummaryAsync(string rawText, CancellationToken ct = default)
    {
        if (string.IsNullOrWhiteSpace(rawText))
            return "No readable text was extracted from this content.";

        if (!CanUseApi())
            return CreateLocalSummary(rawText);

        return await ExecuteWithRetriesAsync(async () =>
        {
            var request = new
            {
                model = _settings.Gemini.CompletionModel,
                messages = new object[]
                {
                    new { role = "system", content = "Create a concise, useful summary for a saved learning item. Prefer concrete takeaways over hype." },
                    new { role = "user", content = Truncate(rawText, MaxInputCharacters) }
                },
                temperature = 0.2,
                max_tokens = 350
            };

            using var requestMessage = new HttpRequestMessage(HttpMethod.Post, "chat/completions")
            {
                Content = ToJsonContent(request)
            };

            if (!string.IsNullOrWhiteSpace(_settings.Gemini.OpenAIApiKey) && _settings.Gemini.OpenAIApiKey != "ollama")
            {
                requestMessage.Headers.Authorization = new AuthenticationHeaderValue("Bearer", _settings.Gemini.OpenAIApiKey);
            }

            using var response = await _httpClient.SendAsync(requestMessage, ct);
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

        if (!CanUseApi())
            return CreateLocalEmbedding(text);

        return await ExecuteWithRetriesAsync(async () =>
        {
            var isGemini = _settings.Gemini.EndpointUrl.Contains("generativelanguage.googleapis.com");
            
            if (isGemini)
            {
                var request = new
                {
                    model = "models/" + _settings.Gemini.EmbeddingModel,
                    content = new
                    {
                        parts = new[] { new { text = Truncate(text, MaxInputCharacters) } }
                    },
                    outputDimensionality = EmbeddingDimensions
                };

                var baseUrl = _settings.Gemini.EndpointUrl.Replace("/openai/", "/");
                baseUrl = baseUrl.TrimEnd('/');
                var nativeUrl = $"{baseUrl}/models/{_settings.Gemini.EmbeddingModel}:embedContent?key={_settings.Gemini.OpenAIApiKey}";

                // Send without the Bearer token as Gemini uses the query string
                using var requestMessage = new HttpRequestMessage(HttpMethod.Post, nativeUrl)
                {
                    Content = ToJsonContent(request)
                };
                
                // Do NOT set requestMessage.Headers.Authorization for native Gemini API.

                using var response = await _httpClient.SendAsync(requestMessage, ct);
                response.EnsureSuccessStatusCode();
                using var document = JsonDocument.Parse(await response.Content.ReadAsStringAsync(ct));
                var values = document.RootElement.GetProperty("embedding").GetProperty("values");
                var embedding = new float[values.GetArrayLength()];
                var index = 0;
                foreach (var value in values.EnumerateArray())
                    embedding[index++] = value.GetSingle();
                return embedding;
            }
            else
            {
                var request = new
                {
                    model = _settings.Gemini.EmbeddingModel,
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
            }
        }, () => CreateLocalEmbedding(text), ct);
    }

    public async Task<List<(string Description, string Type, int Order)>> ExtractActionsAsync(string rawText, CancellationToken ct = default)
    {
        if (string.IsNullOrWhiteSpace(rawText))
            return new List<(string Description, string Type, int Order)>();

        if (!CanUseApi())
            return CreateLocalActions(rawText);

        return await ExecuteWithRetriesAsync(async () =>
        {
            var request = new
            {
                model = _settings.Gemini.CompletionModel,
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

    public async Task<string> GetChatCompletionAsync(string systemMessage, string userMessage, CancellationToken ct = default)
    {
        if (string.IsNullOrWhiteSpace(userMessage))
            return "No user prompt was provided.";

        if (!CanUseApi())
            return "AI service is offline (no API key configured).";

        return await ExecuteWithRetriesAsync(async () =>
        {
            var request = new
            {
                model = _settings.Gemini.CompletionModel,
                messages = new object[]
                {
                    new { role = "system", content = systemMessage },
                    new { role = "user", content = Truncate(userMessage, MaxInputCharacters) }
                },
                temperature = 0.3,
                max_tokens = 2000
            };

            using var requestMessage = new HttpRequestMessage(HttpMethod.Post, "chat/completions")
            {
                Content = ToJsonContent(request)
            };

            if (!string.IsNullOrWhiteSpace(_settings.Gemini.OpenAIApiKey) && _settings.Gemini.OpenAIApiKey != "ollama")
            {
                requestMessage.Headers.Authorization = new AuthenticationHeaderValue("Bearer", _settings.Gemini.OpenAIApiKey);
            }

            using var response = await _httpClient.SendAsync(requestMessage, ct);
            response.EnsureSuccessStatusCode();
            using var document = JsonDocument.Parse(await response.Content.ReadAsStringAsync(ct));
            return document.RootElement
                .GetProperty("choices")[0]
                .GetProperty("message")
                .GetProperty("content")
                .GetString()?
                .Trim() ?? "Failed to generate AI response.";
        }, () => "Failed to generate AI response due to API connection errors.", ct);
    }

    public async Task<string> DescribeVideoFramesAsync(List<VideoKeyframe> keyframes, CancellationToken ct = default)
    {
        if (keyframes is null || keyframes.Count == 0)
            return "No video frames were extracted for visual analysis.";

        if (!CanUseApi())
            return "AI visual description service is offline (no API key configured).";

        return await ExecuteWithRetriesAsync(async () =>
        {
            var contentList = new List<object>
            {
                new 
                { 
                    type = "text", 
                    text = "Analyze these chronological keyframe images extracted from a saved video tutorial/content and generate a detailed visual timeline explaining what is happening on screen.\nIdentify slide titles, bullet points, code block details (file name, class/method, code snippets), UI controls, and visual illustrations. Provide a concise, clear chronological list of visual descriptions with timestamps corresponding to the frames."
                }
            };

            foreach (var frame in keyframes)
            {
                contentList.Add(new { type = "text", text = $"Frame at {frame.Timestamp}:" });
                contentList.Add(new
                {
                    type = "image_url",
                    image_url = new
                    {
                        url = $"data:image/jpeg;base64,{frame.Base64Data}"
                    }
                });
            }

            var request = new
            {
                model = _settings.Gemini.CompletionModel,
                messages = new object[]
                {
                    new { role = "user", content = contentList.ToArray() }
                },
                temperature = 0.2,
                max_tokens = 1500
            };

            using var requestMessage = new HttpRequestMessage(HttpMethod.Post, "chat/completions")
            {
                Content = ToJsonContent(request)
            };

            if (!string.IsNullOrWhiteSpace(_settings.Gemini.OpenAIApiKey) && _settings.Gemini.OpenAIApiKey != "ollama")
            {
                requestMessage.Headers.Authorization = new AuthenticationHeaderValue("Bearer", _settings.Gemini.OpenAIApiKey);
            }

            using var response = await _httpClient.SendAsync(requestMessage, ct);
            response.EnsureSuccessStatusCode();
            using var document = JsonDocument.Parse(await response.Content.ReadAsStringAsync(ct));
            return document.RootElement
                .GetProperty("choices")[0]
                .GetProperty("message")
                .GetProperty("content")
                .GetString()?
                .Trim() ?? "No visual descriptions could be generated.";
        }, () => "Failed to generate visual timeline from video frames.", ct);
    }

    public async Task<string> AnalyzeVideoFileAsync(string mp4FilePath, string prompt, CancellationToken ct = default)
    {
        if (string.IsNullOrWhiteSpace(mp4FilePath) || !File.Exists(mp4FilePath))
            return "Video file not found.";

        var isGemini = _settings.Gemini.EndpointUrl.Contains("generativelanguage.googleapis.com");
        if (!isGemini)
            return "Video file analysis is currently only supported via native Gemini File API.";

        return await ExecuteWithRetriesAsync(async () =>
        {
            var baseUrl = _settings.Gemini.EndpointUrl.Replace("/openai/", "/");
            baseUrl = baseUrl.TrimEnd('/');

            // 1. Upload file
            var uploadUrl = $"https://generativelanguage.googleapis.com/upload/v1beta/files?key={_settings.Gemini.OpenAIApiKey}";
            using var fileStream = File.OpenRead(mp4FilePath);
            using var fileContent = new StreamContent(fileStream);
            fileContent.Headers.ContentType = new MediaTypeHeaderValue("video/mp4");
            
            using var uploadResponse = await _httpClient.PostAsync(uploadUrl, fileContent, ct);
            uploadResponse.EnsureSuccessStatusCode();
            using var uploadDoc = JsonDocument.Parse(await uploadResponse.Content.ReadAsStringAsync(ct));
            var fileNode = uploadDoc.RootElement.GetProperty("file");
            var fileUri = fileNode.GetProperty("uri").GetString();
            var fileName = fileNode.GetProperty("name").GetString();

            if (string.IsNullOrWhiteSpace(fileUri) || string.IsNullOrWhiteSpace(fileName))
                return "Failed to parse uploaded file URI from Gemini.";

            try
            {
                // 2. Poll until state == ACTIVE
                var isActive = false;
                var pollAttempts = 0;
                while (!isActive && pollAttempts < 60) // Up to 10 minutes wait
                {
                    pollAttempts++;
                    var pollUrl = $"{baseUrl}/{fileName}?key={_settings.Gemini.OpenAIApiKey}";
                    using var pollResponse = await _httpClient.GetAsync(pollUrl, ct);
                    if (pollResponse.IsSuccessStatusCode)
                    {
                        using var pollDoc = JsonDocument.Parse(await pollResponse.Content.ReadAsStringAsync(ct));
                        var state = pollDoc.RootElement.GetProperty("state").GetString();
                        if (state == "ACTIVE")
                        {
                            isActive = true;
                            break;
                        }
                        else if (state == "FAILED")
                        {
                            return "Gemini video processing failed.";
                        }
                    }
                    await Task.Delay(TimeSpan.FromSeconds(10), ct);
                }

                if (!isActive)
                    return "Video processing timed out while waiting for Gemini to become ACTIVE.";

                // 3. Generate Content
                var request = new
                {
                    contents = new[]
                    {
                        new
                        {
                            parts = new object[]
                            {
                                new { fileData = new { mimeType = "video/mp4", fileUri = fileUri } },
                                new { text = prompt }
                            }
                        }
                    }
                };

                var generateUrl = $"{baseUrl}/models/{_settings.Gemini.CompletionModel}:generateContent?key={_settings.Gemini.OpenAIApiKey}";
                using var generateResponse = await _httpClient.PostAsync(generateUrl, ToJsonContent(request), ct);
                generateResponse.EnsureSuccessStatusCode();
                using var generateDoc = JsonDocument.Parse(await generateResponse.Content.ReadAsStringAsync(ct));
                var content = generateDoc.RootElement
                    .GetProperty("candidates")[0]
                    .GetProperty("content")
                    .GetProperty("parts")[0]
                    .GetProperty("text")
                    .GetString();

                return content?.Trim() ?? "Failed to extract content from video.";
            }
            finally
            {
                // 4. Clean up file on Gemini
                try
                {
                    var deleteUrl = $"{baseUrl}/{fileName}?key={_settings.Gemini.OpenAIApiKey}";
                    using var deleteResponse = await _httpClient.DeleteAsync(deleteUrl, ct);
                }
                catch (Exception ex)
                {
                    _logger.LogWarning(ex, "Failed to delete file {FileName} from Gemini", fileName);
                }
            }
        }, () => "Failed to generate AI response from video file.", ct);
    }


    private async Task<T> ExecuteWithRetriesAsync<T>(Func<Task<T>> operation, Func<T> fallback, CancellationToken ct)
    {
        var maxAttempts = Math.Max(1, _settings.Gemini.MaxRetries);
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

    private bool CanUseApi() => !string.IsNullOrWhiteSpace(_settings.Gemini.OpenAIApiKey) || _settings.Gemini.EndpointUrl != "https://api.openai.com/v1/";

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
