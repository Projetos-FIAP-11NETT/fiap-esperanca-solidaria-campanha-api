using FiapEsperancaSolidaria.Campanha.Domain.Abstractions;

namespace FiapEsperancaSolidaria.Campanha.Domain.Aggregates.DonationAggregate;

public class Donation : IAggregateRoot
{
    public Guid Id { get; private set; }
    public Guid CampaignId { get; private set; }
    public Guid DonorId { get; private set; }
    public decimal Amount { get; private set; }
    public PaymentMethod PaymentMethod { get; private set; }
    public DonationStatus DonationStatus { get; private set; }
    public DateTime CreatedAt { get; private set; }

    protected Donation() { }

    public Donation(Guid campaignId, Guid donorId, decimal amount, PaymentMethod paymentMethod)
    {
        Id = Guid.NewGuid();
        CampaignId = campaignId;
        DonorId = donorId;
        Amount = amount;
        PaymentMethod = paymentMethod;
        DonationStatus = DonationStatus.Pending;
        CreatedAt = DateTime.UtcNow;
    }
}