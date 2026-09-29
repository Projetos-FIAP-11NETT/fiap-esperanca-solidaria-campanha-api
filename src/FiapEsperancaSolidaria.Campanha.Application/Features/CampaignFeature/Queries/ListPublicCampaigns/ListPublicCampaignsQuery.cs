using FiapEsperancaSolidaria.Campanha.Application.Behaviors;
using FiapEsperancaSolidaria.Campanha.Application.DTOs;
using MediatR;

namespace FiapEsperancaSolidaria.Campanha.Application.Features.CampaignFeature.Queries.ListPublicCampaigns;

public sealed record ListPublicCampaignsQuery(string? Title = null)
    : IRequest<IReadOnlyList<PublicCampaignResponse>>, ICacheableQuery<IReadOnlyList<PublicCampaignResponse>>
{
    public string CacheKey => CacheKeys.PublicCampaigns(Title);
    public TimeSpan? Expiration => TimeSpan.FromSeconds(30);
}
