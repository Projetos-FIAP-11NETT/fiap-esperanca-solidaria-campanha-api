namespace FiapEsperancaSolidaria.Campanha.Domain.Contracts.Cache;

public interface ICacheService
{
    Task<T?> ObterAsync<T>(string chave, CancellationToken cancellationToken = default) where T : class;
    Task DefinirAsync<T>(string chave, T valor, TimeSpan? expiracao = null, CancellationToken cancellationToken = default) where T : class;
    Task RemoverAsync(string chave, CancellationToken cancellationToken = default);
}
