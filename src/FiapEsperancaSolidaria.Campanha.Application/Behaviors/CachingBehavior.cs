using FiapEsperancaSolidaria.Campanha.Domain.Contracts.Cache;
using MediatR;

namespace FiapEsperancaSolidaria.Campanha.Application.Behaviors;

public class CachingBehavior<TRequest, TResponse> : IPipelineBehavior<TRequest, TResponse>
    where TRequest : notnull, ICacheableQuery<TResponse>
    where TResponse : class
{
    private readonly ICacheService _cacheService;

    public CachingBehavior(ICacheService cacheService)
    {
        _cacheService = cacheService;
    }

    public async Task<TResponse> Handle(
        TRequest request,
        RequestHandlerDelegate<TResponse> next,
        CancellationToken cancellationToken)
    {
        var valorEmCache = await _cacheService.ObterAsync<TResponse>(request.CacheKey, cancellationToken);
        if (valorEmCache is not null)
            return valorEmCache;

        var resposta = await next(cancellationToken);

        await _cacheService.DefinirAsync(request.CacheKey, resposta, request.Expiracao, cancellationToken);

        return resposta;
    }
}
