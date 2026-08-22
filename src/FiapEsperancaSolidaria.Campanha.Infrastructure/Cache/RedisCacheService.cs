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

    public async Task<T?> ObterAsync<T>(string chave, CancellationToken cancellationToken = default) where T : class
    {
        var valorSerializado = await _distributedCache.GetStringAsync(chave, cancellationToken);

        return valorSerializado is null
            ? null
            : JsonSerializer.Deserialize<T>(valorSerializado);
    }

    public async Task DefinirAsync<T>(string chave, T valor, TimeSpan? expiracao = null, CancellationToken cancellationToken = default) where T : class
    {
        var opcoes = new DistributedCacheEntryOptions
        {
            AbsoluteExpirationRelativeToNow = expiracao ?? TimeSpan.FromMinutes(1)
        };

        var valorSerializado = JsonSerializer.Serialize(valor);

        await _distributedCache.SetStringAsync(chave, valorSerializado, opcoes, cancellationToken);
    }

    public Task RemoverAsync(string chave, CancellationToken cancellationToken = default)
        => _distributedCache.RemoveAsync(chave, cancellationToken);
}
