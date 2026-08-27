using FiapEsperancaSolidaria.Campanha.Application.DTOs;
using FiapEsperancaSolidaria.Campanha.Domain.Contracts.Repositories;
using FiapEsperancaSolidaria.Campanha.Domain.Exceptions;
using MediatR;

namespace FiapEsperancaSolidaria.Campanha.Application.Features.CampaignFeature.Queries.GetCampaignById;

public class GetCampaignByIdQueryHandler(
        ICampaignRepository campaignRepository
    ) : IRequestHandler<GetCampaignByIdQuery, CampaignResponse>
{
    public async Task<CampaignResponse> Handle(GetCampaignByIdQuery request, CancellationToken cancellationToken)
    {
        var campaign = await campaignRepository.GetByIdAsync(request.Id, cancellationToken)
            ?? throw new NotFoundException($"Campanha '{request.Id}' não encontrada.");

        return new CampaignResponse(
            campaign.CampaignId,
            campaign.Title,
            campaign.Description,
            campaign.StartDate,
            campaign.EndDate,
            campaign.Image,
            campaign.FinancialGoal,
            campaign.Status.ToString(),
            campaign.TotalRaised);
    }
}
