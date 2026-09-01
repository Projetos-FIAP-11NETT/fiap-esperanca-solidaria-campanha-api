namespace FiapEsperancaSolidaria.Campanha.Domain.Aggregates.CampaignAggregate;

public enum DonationStatus : byte
{
    Pending = 1,
    PaymentProcessing = 2,
    Approved = 3,
    Rejected = 4
}