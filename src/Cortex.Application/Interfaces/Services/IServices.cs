using Cortex.Application.DTOs.Auth;
using Cortex.SharedKernel;

namespace Cortex.Application.Interfaces.Services;

/// <summary>
/// Authentication service interface — handles login, registration, OAuth, and JWT tokens.
/// </summary>
public interface IAuthService
{
    Task<Result<AuthResponse>> RegisterAsync(RegisterRequest request, CancellationToken ct = default);
    Task<Result<AuthResponse>> LoginAsync(LoginRequest request, CancellationToken ct = default);
    Task<Result<AuthResponse>> OAuthLoginAsync(OAuthRequest request, CancellationToken ct = default);
    Task<Result<AuthResponse>> RefreshTokenAsync(RefreshTokenRequest request, CancellationToken ct = default);
}

/// <summary>
/// Content ingestion service — URL parsing and metadata extraction.
/// </summary>
public interface IContentIngestionService
{
    Task<Result<Guid>> IngestContentAsync(Guid userId, string url, CancellationToken ct = default);
}

/// <summary>
/// AI extraction service — summarization, embedding, and action extraction.
/// </summary>
public interface IAIExtractionService
{
    Task<string> GenerateSummaryAsync(string rawText, CancellationToken ct = default);
    Task<float[]> GenerateEmbeddingAsync(string text, CancellationToken ct = default);
    Task<List<(string Description, string Type, int Order)>> ExtractActionsAsync(string rawText, CancellationToken ct = default);
}

/// <summary>
/// Semantic search service — vector similarity queries.
/// </summary>
public interface ISearchService
{
    Task<Result<List<DTOs.Search.SearchResultResponse>>> SearchAsync(Guid userId, string query, int limit, CancellationToken ct = default);
}

/// <summary>
/// Drip system service — creates and manages micro-dosed learning tracks.
/// </summary>
public interface IDripService
{
    Task<Result<Guid>> CreateTrackAsync(Guid userId, Guid contentItemId, int totalDays, CancellationToken ct = default);
    Task<Result> AdvanceStepAsync(Guid stepId, CancellationToken ct = default);
}

/// <summary>
/// Cache service abstraction — Redis caching layer.
/// </summary>
public interface ICacheService
{
    Task<T?> GetAsync<T>(string key, CancellationToken ct = default);
    Task SetAsync<T>(string key, T value, TimeSpan? expiration = null, CancellationToken ct = default);
    Task RemoveAsync(string key, CancellationToken ct = default);
    Task RemoveByPrefixAsync(string keyPrefix, CancellationToken ct = default);
    Task<bool> ExistsAsync(string key, CancellationToken ct = default);
}

/// <summary>
/// Message broker abstraction — RabbitMQ publish/subscribe.
/// </summary>
public interface IMessageBroker
{
    Task PublishAsync<T>(string queueName, T message, CancellationToken ct = default);
}

/// <summary>
/// Notification service — push notifications and email digests.
/// </summary>
public interface INotificationService
{
    Task SendPushNotificationAsync(string pushToken, string title, string body, CancellationToken ct = default);
    Task SendEmailDigestAsync(string email, string subject, string htmlBody, CancellationToken ct = default);
}

/// <summary>
/// File storage abstraction — MinIO/blob storage for hero images.
/// </summary>
public interface IFileStorageService
{
    Task<string> UploadAsync(string fileName, Stream content, string contentType, CancellationToken ct = default);
    Task<Stream?> DownloadAsync(string filePath, CancellationToken ct = default);
    Task DeleteAsync(string filePath, CancellationToken ct = default);
}
