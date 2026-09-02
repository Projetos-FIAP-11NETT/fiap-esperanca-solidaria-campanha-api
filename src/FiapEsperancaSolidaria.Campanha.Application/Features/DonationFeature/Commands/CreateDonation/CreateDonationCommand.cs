using FiapEsperancaSolidaria.Campanha.Application.DTOs;
using FiapEsperancaSolidaria.Campanha.Domain.Aggregates.CampaignAggregate;
using MediatR;

namespace FiapEsperancaSolidaria.Campanha.Application.Features.DonationFeature.Commands.CreateDonation;

public sealed record CreateDonationCommand(
    Guid CampaignId,
    Guid DonorId,
    decimal Amount,
    PaymentMethod PaymentMethod) : IRequest<DonationResponse>;