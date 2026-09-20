using FiapEsperancaSolidaria.Campanha.Domain.Aggregates.CampaignAggregate;
using FiapEsperancaSolidaria.Campanha.Domain.Contracts.Repositories.Generic;

namespace FiapEsperancaSolidaria.Campanha.Domain.Contracts.Repositories;

public interface IDonationRepository : IRepository<Donation>
{
    Task<IReadOnlyList<Donation>> ListByDonorAsync(Guid donorId, CancellationToken cancellationToken = default);
}