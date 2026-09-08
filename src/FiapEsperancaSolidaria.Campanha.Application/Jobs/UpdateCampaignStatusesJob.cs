using FiapEsperancaSolidaria.Campanha.Application.Features.CampaignFeature.Commands.UpdateCampaignStatuses;
using MediatR;

namespace FiapEsperancaSolidaria.Campanha.Application.Jobs;

public class UpdateCampaignStatusesJob(IMediator mediator)
{
    public Task RunAsync(CancellationToken cancellationToken) =>
        mediator.Send(new UpdateCampaignStatusesCommand(), cancellationToken);
}
