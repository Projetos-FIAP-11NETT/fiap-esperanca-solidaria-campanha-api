using FiapEsperancaSolidaria.Campanha.Application.DTOs;
using FiapEsperancaSolidaria.Campanha.Domain.Contracts.Repositories;
using MediatR;

namespace FiapEsperancaSolidaria.Campanha.Application.Features.CampaignFeature.Queries.ListCampaigns;

public class ListCampaignsQueryHandler(
        ICampaignRepository campaignRepository
    ) : IRequestHandler<ListCampaignsQuery, IReadOnlyList<CampaignResponse>>
{
    public async Task<IReadOnlyList<CampaignResponse>> Handle(
        ListCampaignsQuery request,
        CancellationToken cancellationToken)
    {
        var campaigns = await campaignRepository.GetAllAsync();

        return campaigns
            .OrderByDescending(c => c.CreatedAt)
            .Select(c => new CampaignResponse(
                c.CampaignId,
                c.Title,
                c.Description,
                c.StartDate,
                c.EndDate,
                c.Image,
                c.FinancialGoal,
                c.Status.ToString(),
                c.TotalRaised))
            .ToList();
    }
}
