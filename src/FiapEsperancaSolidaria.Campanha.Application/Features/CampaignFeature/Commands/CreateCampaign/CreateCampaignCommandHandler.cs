using FiapEsperancaSolidaria.Campanha.Application.Behaviors;
using FiapEsperancaSolidaria.Campanha.Application.DTOs;
using FiapEsperancaSolidaria.Campanha.Domain.Aggregates.CampaignAggregate;
using FiapEsperancaSolidaria.Campanha.Domain.Contracts.Cache;
using FiapEsperancaSolidaria.Campanha.Domain.Contracts.Repositories;
using MediatR;

namespace FiapEsperancaSolidaria.Campanha.Application.Features.CampaignFeature.Commands.CreateCampaign;

public class CreateCampaignCommandHandler(
        ICampaignRepository campaignRepository,
        ICacheService cacheService
    ) : IRequestHandler<CreateCampaignCommand, CampaignResponse>
{
    public async Task<CampaignResponse> Handle(CreateCampaignCommand request, CancellationToken cancellationToken)
    {
        var campaign = Campaign.Create(
            request.Title,
            request.Description,
            request.StartDate,
            request.EndDate,
            request.FinancialGoal,
            request.Image);

        await campaignRepository.AddAsync(campaign, cancellationToken);

        await cacheService.RemoveAsync(CacheKeys.PublicCampaigns(), cancellationToken);

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
