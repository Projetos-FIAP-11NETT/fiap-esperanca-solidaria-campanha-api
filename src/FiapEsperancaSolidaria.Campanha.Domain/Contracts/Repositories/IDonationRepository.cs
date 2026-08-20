using FiapEsperancaSolidaria.Campanha.Domain.Aggregates.DonationAggregate;
using FiapEsperancaSolidaria.Campanha.Domain.Contracts.Repositories.Generic;

namespace FiapEsperancaSolidaria.Campanha.Domain.Contracts.Repositories;

public interface IDonationRepository : IRepository<Donation>
{
}