using Cortex.Modules.Auth.Services;
using Cortex.Infrastructure.AI;
using Cortex.Infrastructure.Caching;
using Cortex.Infrastructure.Messaging;
using Microsoft.Extensions.Logging;
using StackExchange.Redis;
using System.Text.Json;

namespace Cortex.Infrastructure.Caching;

/// <summary>
/// Redis-backed cache service implementation.
/// </summary>
public class RedisCacheService : ICacheService
{
    private readonly IConnectionMultiplexer _redis;
    private readonly ILogger<RedisCacheService> _logger;

    public RedisCacheService(IConnectionMultiplexer redis, ILogger<RedisCacheService> logger)
    {
        _redis = redis;
        _logger = logger;
    }

    public async Task<T?> GetAsync<T>(string key, CancellationToken ct = default)
    {
        try
        {
            var db = _redis.GetDatabase();
            var value = await db.StringGetAsync(key);
            if (value.IsNullOrEmpty) return default;

            var options = new JsonSerializerOptions { ReferenceHandler = System.Text.Json.Serialization.ReferenceHandler.Preserve };
            return JsonSerializer.Deserialize<T>(value.ToString(), options);
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Failed to read cached value for key: {Key}", key);
            return default;
        }
    }

    public async Task SetAsync<T>(string key, T value, TimeSpan? expiration = null, CancellationToken ct = default)
    {
        try
        {
            var db = _redis.GetDatabase();
            var options = new JsonSerializerOptions { ReferenceHandler = System.Text.Json.Serialization.ReferenceHandler.Preserve };
            var serialized = JsonSerializer.Serialize(value, options);
            await db.StringSetAsync(key, serialized, expiration ?? TimeSpan.FromMinutes(30));
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Failed to set cached value for key: {Key}", key);
        }
    }

    public async Task RemoveAsync(string key, CancellationToken ct = default)
    {
        try
        {
            var db = _redis.GetDatabase();
            await db.KeyDeleteAsync(key);
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Failed to remove cached value for key: {Key}", key);
        }
    }

    public async Task RemoveByPrefixAsync(string keyPrefix, CancellationToken ct = default)
    {
        try
        {
            var endpoints = _redis.GetEndPoints();
            if (endpoints.Length == 0) return;

            var server = _redis.GetServer(endpoints[0]);
            var keys = server.Keys(pattern: $"{keyPrefix}*").ToArray();
            if (keys.Length == 0) return;

            await _redis.GetDatabase().KeyDeleteAsync(keys);
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Failed to remove cached values with prefix: {KeyPrefix}", keyPrefix);
        }
    }

    public async Task<bool> ExistsAsync(string key, CancellationToken ct = default)
    {
        try
        {
            var db = _redis.GetDatabase();
            return await db.KeyExistsAsync(key);
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Failed to check cached value for key: {Key}", key);
            return false;
        }
    }
}
