using FiapEsperancaSolidaria.Campanha.Application.Behaviors;
using FiapEsperancaSolidaria.Campanha.Application.DTOs;
using MediatR;

namespace FiapEsperancaSolidaria.Campanha.Application.Features.CampanhaFeature.Queries.ObterCampanhaPorId;

public sealed record ObterCampanhaPorIdQuery(Guid Id)
    : IRequest<CampanhaResponse>, ICacheableQuery<CampanhaResponse>
{
    public string CacheKey => CacheKeys.Campanha(Id);
    public TimeSpan? Expiracao => TimeSpan.FromSeconds(60);
}
