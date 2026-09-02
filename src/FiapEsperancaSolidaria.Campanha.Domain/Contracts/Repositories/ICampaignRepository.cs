using FiapEsperancaSolidaria.Campanha.Domain.Aggregates.CampaignAggregate;
using FiapEsperancaSolidaria.Campanha.Domain.Contracts.Repositories.Generic;

namespace FiapEsperancaSolidaria.Campanha.Domain.Contracts.Repositories;

public interface ICampaignRepository : IRepository<Campaign>
{
    Task<Campaign?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default);
    Task<IReadOnlyList<Campaign>> ListActiveAsync(string? title = null, CancellationToken cancellationToken = default);
    Task<bool> ExistsWithTitleAsync(string title, Guid? excludedId = null, CancellationToken cancellationToken = default);
    Task AddAsync(Campaign campaign, CancellationToken cancellationToken = default);
    Task UpdateAsync(Campaign campaign, CancellationToken cancellationToken = default);
}
