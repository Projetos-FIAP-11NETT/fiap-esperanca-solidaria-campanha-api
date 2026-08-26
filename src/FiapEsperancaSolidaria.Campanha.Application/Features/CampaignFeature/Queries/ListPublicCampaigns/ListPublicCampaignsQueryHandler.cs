using FiapEsperancaSolidaria.Campanha.Application.DTOs;
using FiapEsperancaSolidaria.Campanha.Domain.Contracts.Repositories;
using MediatR;

namespace FiapEsperancaSolidaria.Campanha.Application.Features.CampaignFeature.Queries.ListPublicCampaigns;

public class ListPublicCampaignsQueryHandler(
        ICampaignRepository campaignRepository
    ) : IRequestHandler<ListPublicCampaignsQuery, IReadOnlyList<PublicCampaignResponse>>
{
    public async Task<IReadOnlyList<PublicCampaignResponse>> Handle(
        ListPublicCampaignsQuery request,
        CancellationToken cancellationToken)
    {
        var campaigns = await campaignRepository.ListActiveAsync(request.Title, cancellationToken);

        return campaigns
            .Select(c => new PublicCampaignResponse(c.CampaignId, c.Title, c.FinancialGoal, c.TotalRaised))
            .ToList();
    }
}
