using MediatR;

namespace FiapEsperancaSolidaria.Campanha.Application.Features.CampaignFeature.Commands.UpdateCampaignStatuses;

public sealed record UpdateCampaignStatusesCommand : IRequest<int>;
