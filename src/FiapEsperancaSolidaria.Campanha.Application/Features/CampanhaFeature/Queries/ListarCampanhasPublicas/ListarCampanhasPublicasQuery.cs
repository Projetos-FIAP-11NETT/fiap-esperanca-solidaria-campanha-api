using FiapEsperancaSolidaria.Campanha.Application.Behaviors;
using FiapEsperancaSolidaria.Campanha.Application.DTOs;
using MediatR;

namespace FiapEsperancaSolidaria.Campanha.Application.Features.CampanhaFeature.Queries.ListarCampanhasPublicas;

public sealed record ListarCampanhasPublicasQuery(string? Titulo = null)
    : IRequest<IReadOnlyList<CampanhaPublicaResponse>>, ICacheableQuery<IReadOnlyList<CampanhaPublicaResponse>>
{
    public string CacheKey => CacheKeys.CampanhasPublicas(Titulo);
    public TimeSpan? Expiracao => TimeSpan.FromSeconds(30);
}
