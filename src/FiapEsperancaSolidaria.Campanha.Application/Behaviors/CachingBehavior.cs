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
        var cachedValue = await _cacheService.GetAsync<TResponse>(request.CacheKey, cancellationToken);
        if (cachedValue is not null)
            return cachedValue;

        var response = await next(cancellationToken);

        await _cacheService.SetAsync(request.CacheKey, response, request.Expiration, cancellationToken);

        return response;
    }
}
