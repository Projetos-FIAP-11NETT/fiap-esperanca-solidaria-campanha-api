namespace FiapEsperancaSolidaria.Campanha.Domain.Contracts.Repositories.Generic;

public interface IRepository<T> where T : class
{
    Task AddAsync(T entity);
    Task AddRangeAsync(IEnumerable<T> entities);

    void Update(T entity);
    void UpdateRange(IEnumerable<T> entities);

    void Remove(T item);
    void RemoveRange(IEnumerable<T> entities);

    Task BeginTransaction();
    void RollbackTransaction();
    void CommitTransaction();

    Task<bool> SaveChangesAsync(CancellationToken cancellationToken = default);

    Task<IEnumerable<T>> GetAllAsync();
    Task<T?> GetByIdAsync(Guid? id);
}