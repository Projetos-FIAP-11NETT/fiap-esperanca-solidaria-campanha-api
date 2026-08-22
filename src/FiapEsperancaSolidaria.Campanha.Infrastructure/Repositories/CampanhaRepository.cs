using FiapEsperancaSolidaria.Campanha.Domain.Contracts.Repositories;
using FiapEsperancaSolidaria.Campanha.Domain.Enums;
using FiapEsperancaSolidaria.Campanha.Domain.Exceptions;
using FiapEsperancaSolidaria.Campanha.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;
using Npgsql;

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

    public async Task<IReadOnlyList<Domain.Entities.Campanha>> ListarAtivasAsync(string? titulo = null, CancellationToken cancellationToken = default)
    {
        var query = _dbContext.Campanhas
            .Where(c => c.Status == StatusCampanha.Ativa);

        if (!string.IsNullOrWhiteSpace(titulo))
        {
            var padrao = $"%{EscaparCoringasLike(titulo.Trim())}%";
            query = query.Where(c => EF.Functions.ILike(c.Titulo, padrao, @"\"));
        }

        return await query
            .AsNoTracking()
            .ToListAsync(cancellationToken);
    }

    private static string EscaparCoringasLike(string valor) =>
        valor.Replace(@"\", @"\\").Replace("%", @"\%").Replace("_", @"\_");

    public async Task<bool> ExisteComTituloAsync(string titulo, Guid? idExcluido = null, CancellationToken cancellationToken = default)
    {
        var query = _dbContext.Campanhas
            .Where(c => c.Titulo.ToLower() == titulo.ToLower());

        if (idExcluido is not null)
            query = query.Where(c => c.Id != idExcluido);

        return await query.AnyAsync(cancellationToken);
    }

    public async Task AdicionarAsync(Domain.Entities.Campanha campanha, CancellationToken cancellationToken = default)
    {
        await _dbContext.Campanhas.AddAsync(campanha, cancellationToken);
        await SalvarAsync(cancellationToken);
    }

    public async Task AtualizarAsync(Domain.Entities.Campanha campanha, CancellationToken cancellationToken = default)
    {
        _dbContext.Campanhas.Update(campanha);
        await SalvarAsync(cancellationToken);
    }

    private async Task SalvarAsync(CancellationToken cancellationToken)
    {
        try
        {
            await _dbContext.SaveChangesAsync(cancellationToken);
        }
        catch (DbUpdateException ex) when (ex.InnerException is PostgresException { SqlState: PostgresErrorCodes.UniqueViolation })
        {
            // Rede de segurança contra corrida entre a checagem do validador e o INSERT/UPDATE
            // (o índice único em lower("Titulo") é quem garante a integridade de fato).
            throw new BusinessException("Já existe uma campanha com esse título.");
        }
    }
}
