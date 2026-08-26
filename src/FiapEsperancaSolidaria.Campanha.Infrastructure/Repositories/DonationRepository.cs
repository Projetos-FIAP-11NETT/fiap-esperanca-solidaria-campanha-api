using FiapEsperancaSolidaria.Campanha.Domain.Aggregates.DonationAggregate;
using FiapEsperancaSolidaria.Campanha.Domain.Contracts.Repositories;
using FiapEsperancaSolidaria.Campanha.Infrastructure.Data;
using FiapEsperancaSolidaria.Campanha.Infrastructure.Repositories.Generic;

namespace FiapEsperancaSolidaria.Campanha.Infrastructure.Repositories;

public class DonationRepository(AppDbContext dbContext)
    : Repository<Donation>(dbContext)
    , IDonationRepository
{
    private readonly AppDbContext _dbContext = dbContext;
}