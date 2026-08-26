namespace FiapEsperancaSolidaria.Campanha.Domain.Contracts.Repositories;

public interface ICampaignRepository
{
    Task<Entities.Campaign?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default);
    Task<IReadOnlyList<Entities.Campaign>> ListActiveAsync(string? title = null, CancellationToken cancellationToken = default);
    Task<bool> ExistsWithTitleAsync(string title, Guid? excludedId = null, CancellationToken cancellationToken = default);
    Task AddAsync(Entities.Campaign campaign, CancellationToken cancellationToken = default);
    Task UpdateAsync(Entities.Campaign campaign, CancellationToken cancellationToken = default);
}
