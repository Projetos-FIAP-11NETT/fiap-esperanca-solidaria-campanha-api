using FiapEsperancaSolidaria.Campanha.Application.DTOs;
using FiapEsperancaSolidaria.Campanha.Domain.Aggregates.CampaignAggregate;
using MediatR;

namespace FiapEsperancaSolidaria.Campanha.Application.Features.CampaignFeature.Commands.UpdateCampaign;

public sealed record UpdateCampaignCommand(
    Guid Id,
    string Title,
    string Description,
    DateTime StartDate,
    DateTime EndDate,
    decimal FinancialGoal,
    string? Image,
    CampaignStatus Status) : IRequest<CampaignResponse>;
