using System.Text.Json;
using FiapEsperancaSolidaria.Campanha.Domain.Contracts.Cache;
using Microsoft.Extensions.Caching.Distributed;

namespace FiapEsperancaSolidaria.Campanha.Infrastructure.Cache;

public class RedisCacheService : ICacheService
{
    private readonly IDistributedCache _distributedCache;

    public RedisCacheService(IDistributedCache distributedCache)
    {
        _distributedCache = distributedCache;
    }

    public async Task<T?> GetAsync<T>(string key, CancellationToken cancellationToken = default) where T : class
    {
        var serializedValue = await _distributedCache.GetStringAsync(key, cancellationToken);

        return serializedValue is null
            ? null
            : JsonSerializer.Deserialize<T>(serializedValue);
    }

    public async Task SetAsync<T>(string key, T value, TimeSpan? expiration = null, CancellationToken cancellationToken = default) where T : class
    {
        var options = new DistributedCacheEntryOptions
        {
            AbsoluteExpirationRelativeToNow = expiration ?? TimeSpan.FromMinutes(1)
        };

        var serializedValue = JsonSerializer.Serialize(value);

        await _distributedCache.SetStringAsync(key, serializedValue, options, cancellationToken);
    }

    public Task RemoveAsync(string key, CancellationToken cancellationToken = default)
        => _distributedCache.RemoveAsync(key, cancellationToken);
}
