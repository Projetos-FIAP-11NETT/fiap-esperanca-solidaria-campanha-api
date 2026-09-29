using FiapEsperancaSolidaria.Campanha.Application.DTOs;
using MediatR;

namespace FiapEsperancaSolidaria.Campanha.Application.Features.CampaignFeature.Queries.ListCampaigns;

public sealed record ListCampaignsQuery : IRequest<IReadOnlyList<CampaignResponse>>;
