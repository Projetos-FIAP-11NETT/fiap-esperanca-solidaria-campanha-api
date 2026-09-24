using FiapEsperancaSolidaria.Campanha.Domain.Aggregates.CampaignAggregate;
using FiapEsperancaSolidaria.Campanha.Domain.Contracts.Repositories;
using FiapEsperancaSolidaria.Campanha.Infrastructure.Data;
using FiapEsperancaSolidaria.Campanha.Infrastructure.Repositories.Generic;
using Microsoft.EntityFrameworkCore;

namespace FiapEsperancaSolidaria.Campanha.Infrastructure.Repositories;

public class DonationRepository(AppDbContext dbContext)
    : Repository<Donation>(dbContext)
    , IDonationRepository
{
    private readonly AppDbContext _dbContext = dbContext;

    public async Task<IReadOnlyList<Donation>> ListByDonorAsync(Guid donorId, CancellationToken cancellationToken = default)
    {
        return await _dbContext.Donations
            .Where(d => d.DonorId == donorId)
            .OrderByDescending(d => d.CreatedAt)
            .AsNoTracking()
            .ToListAsync(cancellationToken);
    }
}