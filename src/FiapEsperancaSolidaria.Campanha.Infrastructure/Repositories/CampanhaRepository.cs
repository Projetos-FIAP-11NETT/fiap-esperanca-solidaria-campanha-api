using FiapEsperancaSolidaria.Campanha.Domain.Contracts.Repositories;
using FiapEsperancaSolidaria.Campanha.Domain.Enums;
using FiapEsperancaSolidaria.Campanha.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;

namespace FiapEsperancaSolidaria.Campanha.Infrastructure.Repositories;

public class CampanhaRepository : ICampanhaRepository
{
    private readonly AppDbContext _dbContext;

    public CampanhaRepository(AppDbContext dbContext)
    {
        _dbContext = dbContext;
    }

    public async Task<Domain.Entities.Campanha?> ObterPorIdAsync(Guid id, CancellationToken cancellationToken = default)
    {
        return await _dbContext.Campanhas
            .FirstOrDefaultAsync(c => c.Id == id, cancellationToken);
    }

    public async Task<IReadOnlyList<Domain.Entities.Campanha>> ListarAtivasAsync(CancellationToken cancellationToken = default)
    {
        return await _dbContext.Campanhas
            .Where(c => c.Status == StatusCampanha.Ativa)
            .AsNoTracking()
            .ToListAsync(cancellationToken);
    }

    public async Task AdicionarAsync(Domain.Entities.Campanha campanha, CancellationToken cancellationToken = default)
    {
        await _dbContext.Campanhas.AddAsync(campanha, cancellationToken);
        await _dbContext.SaveChangesAsync(cancellationToken);
    }

    public async Task AtualizarAsync(Domain.Entities.Campanha campanha, CancellationToken cancellationToken = default)
    {
        _dbContext.Campanhas.Update(campanha);
        await _dbContext.SaveChangesAsync(cancellationToken);
    }
}
