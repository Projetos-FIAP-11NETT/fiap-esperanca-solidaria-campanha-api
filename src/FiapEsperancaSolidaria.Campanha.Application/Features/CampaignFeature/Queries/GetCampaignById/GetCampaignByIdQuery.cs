using FiapEsperancaSolidaria.Campanha.Application.DTOs;
using MediatR;

namespace FiapEsperancaSolidaria.Campanha.Application.Features.CampaignFeature.Queries.GetCampaignById;

// Sem cache de propósito: o doacao-work credita TotalRaised direto no Postgres
// (não passa pelo campanha-api), então não há como invalidar um cache aqui
// quando isso acontece. É uma busca por PK, barata o suficiente pra não
// precisar de cache — ao contrário de ListPublicCampaignsQuery, que continua
// cacheada (30s). Ver ICacheableQuery: sem essa interface, CachingBehavior
// simplesmente não entra no pipeline desta query.
public sealed record GetCampaignByIdQuery(Guid Id) : IRequest<CampaignResponse>;
