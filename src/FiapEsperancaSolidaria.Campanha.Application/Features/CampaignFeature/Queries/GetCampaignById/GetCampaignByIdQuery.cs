using FiapEsperancaSolidaria.Campanha.Application.Behaviors;
using FiapEsperancaSolidaria.Campanha.Application.DTOs;
using MediatR;

namespace FiapEsperancaSolidaria.Campanha.Application.Features.CampaignFeature.Queries.GetCampaignById;

public sealed record GetCampaignByIdQuery(Guid Id)
    : IRequest<CampaignResponse>, ICacheableQuery<CampaignResponse>
{
    public string CacheKey => CacheKeys.Campaign(Id);
    public TimeSpan? Expiration => TimeSpan.FromSeconds(60);
}
