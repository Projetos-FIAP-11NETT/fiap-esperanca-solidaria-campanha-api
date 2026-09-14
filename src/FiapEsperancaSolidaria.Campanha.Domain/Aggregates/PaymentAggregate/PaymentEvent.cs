using FiapEsperancaSolidaria.Campanha.Domain.Abstractions;

namespace FiapEsperancaSolidaria.Campanha.Domain.Aggregates.PaymentAggregate;

public class PaymentEvent : IAggregateRoot
{
    public Guid PaymentEventId { get; private set; }

    public Guid DonationId { get; private set; }

    public PaymentEventType PaymentEventType { get; private set; }

    public string Observation { get; private set; } = string.Empty;

    public DateTime CreateAt { get; private set; }

    public PaymentEvent(
        Guid paymentEventId,
        Guid donationId,
        PaymentEventType paymentEventType,
        string observation,
        DateTime createAt)
    {
        PaymentEventId = paymentEventId;
        DonationId = donationId;
        PaymentEventType = paymentEventType;
        Observation = observation;
        CreateAt = createAt;
    }

    private PaymentEvent()
    {
    }
}