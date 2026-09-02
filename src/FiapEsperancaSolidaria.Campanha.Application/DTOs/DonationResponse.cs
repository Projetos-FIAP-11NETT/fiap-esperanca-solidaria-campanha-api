using FiapEsperancaSolidaria.Campanha.Domain.Aggregates.CampaignAggregate;

namespace FiapEsperancaSolidaria.Campanha.Application.DTOs;

public record DonationResponse(
        Guid Id,
        Guid CampaignId,
        Guid DonorId,
        decimal Amount,
        PaymentMethod PaymentMethod, 
        DonationStatus Status,
        DateTime CreatedAt);