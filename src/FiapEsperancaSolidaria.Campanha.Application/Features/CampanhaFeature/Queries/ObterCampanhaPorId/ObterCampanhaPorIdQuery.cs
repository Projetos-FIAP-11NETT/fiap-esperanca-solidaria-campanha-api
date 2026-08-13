using FiapEsperancaSolidaria.Campanha.Application.DTOs;
using MediatR;

namespace FiapEsperancaSolidaria.Campanha.Application.Features.CampanhaFeature.Queries.ObterCampanhaPorId;

public sealed record ObterCampanhaPorIdQuery(Guid Id) : IRequest<CampanhaResponse>;
