using FiapEsperancaSolidaria.Campanha.Domain.Aggregates.CampaignAggregate;

namespace FiapEsperancaSolidaria.Campanha.Domain.Contracts.Notifications;

public interface IDonationCreatedNotification
{
    Task PublishAsync(Guid donationId, CancellationToken cancellationToken = default);
}