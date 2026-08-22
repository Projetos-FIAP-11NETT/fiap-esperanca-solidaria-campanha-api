namespace FiapEsperancaSolidaria.Campanha.Domain.Contracts.Repositories;

public interface ICampanhaRepository
{
    Task<Entities.Campanha?> ObterPorIdAsync(Guid id, CancellationToken cancellationToken = default);
    Task<IReadOnlyList<Entities.Campanha>> ListarAtivasAsync(string? titulo = null, CancellationToken cancellationToken = default);
    Task<bool> ExisteComTituloAsync(string titulo, Guid? idExcluido = null, CancellationToken cancellationToken = default);
    Task AdicionarAsync(Entities.Campanha campanha, CancellationToken cancellationToken = default);
    Task AtualizarAsync(Entities.Campanha campanha, CancellationToken cancellationToken = default);
}
