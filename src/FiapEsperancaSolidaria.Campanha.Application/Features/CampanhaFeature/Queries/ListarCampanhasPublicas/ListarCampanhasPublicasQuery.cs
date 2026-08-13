using FiapEsperancaSolidaria.Campanha.Application.DTOs;
using MediatR;

namespace FiapEsperancaSolidaria.Campanha.Application.Features.CampanhaFeature.Queries.ListarCampanhasPublicas;

public sealed record ListarCampanhasPublicasQuery : IRequest<IReadOnlyList<CampanhaPublicaResponse>>;
