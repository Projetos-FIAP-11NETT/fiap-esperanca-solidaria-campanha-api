using FiapEsperancaSolidaria.Campanha.Application.Behaviors;
using FiapEsperancaSolidaria.Campanha.Domain.Aggregates.CampaignAggregate;
using FiapEsperancaSolidaria.Campanha.Domain.Contracts.Cache;
using FiapEsperancaSolidaria.Campanha.Domain.Contracts.Repositories;
using MediatR;

namespace FiapEsperancaSolidaria.Campanha.Application.Features.CampaignFeature.Commands.UpdateCampaignStatuses;

public class UpdateCampaignStatusesCommandHandler(
        ICampaignRepository campaignRepository,
        ICacheService cacheService
    ) : IRequestHandler<UpdateCampaignStatusesCommand, int>
{
    public async Task<int> Handle(UpdateCampaignStatusesCommand request, CancellationToken cancellationToken)
    {
        var today = DateTime.UtcNow.Date;
        var campaigns = await campaignRepository.ListPendingStatusUpdateAsync(today, cancellationToken);

        if (campaigns.Count == 0)
            return 0;

        foreach (var campaign in campaigns)
        {
            if (campaign.Status == CampaignStatus.Scheduled)
                campaign.Activate();
            else if (campaign.Status == CampaignStatus.Active)
                campaign.Complete();
        }

        await campaignRepository.SaveChangesAsync(cancellationToken);

        await cacheService.RemoveAsync(CacheKeys.PublicCampaigns(), cancellationToken);
        foreach (var campaign in campaigns)
            await cacheService.RemoveAsync(CacheKeys.Campaign(campaign.CampaignId), cancellationToken);

        return campaigns.Count;
    }
}
