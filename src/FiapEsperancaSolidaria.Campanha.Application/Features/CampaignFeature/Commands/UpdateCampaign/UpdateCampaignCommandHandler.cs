using FiapEsperancaSolidaria.Campanha.Application.Behaviors;
using FiapEsperancaSolidaria.Campanha.Application.DTOs;
using FiapEsperancaSolidaria.Campanha.Domain.Contracts.Cache;
using FiapEsperancaSolidaria.Campanha.Domain.Contracts.Repositories;
using FiapEsperancaSolidaria.Campanha.Domain.Exceptions;
using MediatR;

namespace FiapEsperancaSolidaria.Campanha.Application.Features.CampaignFeature.Commands.UpdateCampaign;

public class UpdateCampaignCommandHandler(
        ICampaignRepository campaignRepository,
        ICacheService cacheService
    ) : IRequestHandler<UpdateCampaignCommand, CampaignResponse>
{
    public async Task<CampaignResponse> Handle(UpdateCampaignCommand request, CancellationToken cancellationToken)
    {
        var campaign = await campaignRepository.GetByIdAsync(request.Id, cancellationToken)
            ?? throw new NotFoundException($"Campanha '{request.Id}' não encontrada.");

        campaign.Update(
            request.Title,
            request.Description,
            request.StartDate,
            request.EndDate,
            request.FinancialGoal,
            request.Image);

        campaign.ChangeStatus(request.Status);

        await campaignRepository.UpdateAsync(campaign, cancellationToken);

        await cacheService.RemoveAsync(CacheKeys.PublicCampaigns(), cancellationToken);
        await cacheService.RemoveAsync(CacheKeys.Campaign(campaign.CampaignId), cancellationToken);

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
