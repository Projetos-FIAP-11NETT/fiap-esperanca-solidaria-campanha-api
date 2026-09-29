using FiapEsperancaSolidaria.Campanha.Domain.Aggregates.CampaignAggregate;

namespace FiapEsperancaSolidaria.Campanha.Application.DTOs;

public record DonationReceiptResponse(
    Guid DonationId,
    Guid CampaignId,
    string CampaignTitle,
    string CampaignStatus,
    decimal Amount,
    PaymentMethod PaymentMethod,
    DonationStatus Status,
    DateTime CreatedAt);
