using System;
using System.Threading.Tasks;
using Microsoft.Extensions.Caching.Distributed;
using System.Text.Json;

namespace Cortex.Modules.Content.Services;

public class UserContext
{
    public double Latitude { get; set; }
    public double Longitude { get; set; }
    public string Timezone { get; set; } = "UTC";
    public DateTime LastUpdated { get; set; }
}

public interface IContextService
{
    Task UpdateUserContextAsync(Guid userId, double lat, double lon, string timezone);
    Task<UserContext?> GetUserContextAsync(Guid userId);
}

public class ContextService : IContextService
{
    private readonly IDistributedCache _cache;
    private const string CacheKeyPrefix = "UserContext_";

    public ContextService(IDistributedCache cache)
    {
        _cache = cache;
    }

    public async Task UpdateUserContextAsync(Guid userId, double lat, double lon, string timezone)
    {
        var context = new UserContext
        {
            Latitude = lat,
            Longitude = lon,
            Timezone = timezone,
            LastUpdated = DateTime.UtcNow
        };

        var options = new DistributedCacheEntryOptions
        {
            AbsoluteExpirationRelativeToNow = TimeSpan.FromHours(1) // Keep context valid for 1 hour without updates
        };

        var json = JsonSerializer.Serialize(context);
        await _cache.SetStringAsync($"{CacheKeyPrefix}{userId}", json, options);
    }

    public async Task<UserContext?> GetUserContextAsync(Guid userId)
    {
        var json = await _cache.GetStringAsync($"{CacheKeyPrefix}{userId}");
        if (string.IsNullOrEmpty(json))
            return null;

        return JsonSerializer.Deserialize<UserContext>(json);
    }
}
