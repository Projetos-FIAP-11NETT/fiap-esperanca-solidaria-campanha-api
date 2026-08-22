namespace FiapEsperancaSolidaria.Campanha.Application.Behaviors;

public interface ICacheableQuery<TResponse> where TResponse : class
{
    string CacheKey { get; }
    TimeSpan? Expiracao { get; }
}
