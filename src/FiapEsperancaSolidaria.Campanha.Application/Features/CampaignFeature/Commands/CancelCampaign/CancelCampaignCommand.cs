using FiapEsperancaSolidaria.Campanha.Application.DTOs;
using MediatR;

namespace FiapEsperancaSolidaria.Campanha.Application.Features.CampaignFeature.Commands.CancelCampaign;

public sealed record CancelCampaignCommand(Guid Id) : IRequest<CampaignResponse>;
