using FiapEsperancaSolidaria.Campanha.Domain.Contracts.Repositories;
using FiapEsperancaSolidaria.Campanha.Domain.Enums;
using FiapEsperancaSolidaria.Campanha.Domain.Exceptions;
using FiapEsperancaSolidaria.Campanha.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;
using Npgsql;

namespace FiapEsperancaSolidaria.Campanha.Infrastructure.Repositories;

public class CampaignRepository : ICampaignRepository
{
    private readonly AppDbContext _dbContext;

    public CampaignRepository(AppDbContext dbContext)
    {
        _dbContext = dbContext;
    }

    public async Task<Domain.Entities.Campaign?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default)
    {
        return await _dbContext.Campaigns
            .FirstOrDefaultAsync(c => c.CampaignId == id, cancellationToken);
    }

    public async Task<IReadOnlyList<Domain.Entities.Campaign>> ListActiveAsync(string? title = null, CancellationToken cancellationToken = default)
    {
        var query = _dbContext.Campaigns
            .Where(c => c.Status == CampaignStatus.Active);

        if (!string.IsNullOrWhiteSpace(title))
        {
            var pattern = $"%{EscapeLikeWildcards(title.Trim())}%";
            query = query.Where(c => EF.Functions.ILike(c.Title, pattern, @"\"));
        }

        return await query
            .AsNoTracking()
            .ToListAsync(cancellationToken);
    }

    private static string EscapeLikeWildcards(string value) =>
        value.Replace(@"\", @"\\").Replace("%", @"\%").Replace("_", @"\_");

    public async Task<bool> ExistsWithTitleAsync(string title, Guid? excludedId = null, CancellationToken cancellationToken = default)
    {
        var query = _dbContext.Campaigns
            .Where(c => c.Title.ToLower() == title.ToLower());

        if (excludedId is not null)
            query = query.Where(c => c.CampaignId != excludedId);

        return await query.AnyAsync(cancellationToken);
    }

    public async Task AddAsync(Domain.Entities.Campaign campaign, CancellationToken cancellationToken = default)
    {
        await _dbContext.Campaigns.AddAsync(campaign, cancellationToken);
        await SaveAsync(cancellationToken);
    }

    public async Task UpdateAsync(Domain.Entities.Campaign campaign, CancellationToken cancellationToken = default)
    {
        // Não chama Campaigns.Update(campaign): a entidade já vem rastreada do GetByIdAsync
        // (mesmo DbContext/escopo). Doações novas, porém, têm Id gerado no construtor
        // (Guid.NewGuid()) — como a chave já não é o valor padrão, o DetectChanges do EF
        // não consegue diferenciar "entidade nova" de "entidade existente" só pela navegação
        // e classifica como Modified (gera UPDATE em vez de INSERT). Marca explicitamente
        // como Added qualquer Donation ainda não rastreada antes de salvar.
        foreach (var donation in campaign.Donations)
        {
            if (_dbContext.Entry(donation).State == EntityState.Detached)
                _dbContext.Entry(donation).State = EntityState.Added;
        }

        await SaveAsync(cancellationToken);
    }

    private async Task SaveAsync(CancellationToken cancellationToken)
    {
        try
        {
            await _dbContext.SaveChangesAsync(cancellationToken);
        }
        catch (DbUpdateException ex) when (ex.InnerException is PostgresException { SqlState: PostgresErrorCodes.UniqueViolation })
        {
            // Rede de segurança contra corrida entre a checagem do validador e o INSERT/UPDATE
            // (o índice único em lower("Title") é quem garante a integridade de fato).
            throw new BusinessException("Já existe uma campanha com esse título.");
        }
    }
}
