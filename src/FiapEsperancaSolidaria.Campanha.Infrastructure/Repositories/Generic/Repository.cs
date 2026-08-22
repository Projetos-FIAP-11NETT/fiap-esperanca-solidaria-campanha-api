using FiapEsperancaSolidaria.Campanha.Domain.Contracts.Repositories.Generic;
using Microsoft.EntityFrameworkCore;

namespace FiapEsperancaSolidaria.Campanha.Infrastructure.Repositories.Generic;

public class Repository<T>
(
    DbContext dbContext
)
: IRepository<T> where T : class
{
    private readonly DbSet<T> _dbSet = dbContext.Set<T>();

    public async Task AddAsync(T entity) => await _dbSet.AddAsync(entity);

    public async Task AddRangeAsync(IEnumerable<T> entities) => await _dbSet.AddRangeAsync(entities);

    public void Update(T entity) => _dbSet.Update(entity);

    public void UpdateRange(IEnumerable<T> entities) => _dbSet.UpdateRange(entities);

    public void Remove(T item) => _dbSet.Remove(item);

    public void RemoveRange(IEnumerable<T> entities) => _dbSet.UpdateRange(entities);

    public async Task BeginTransaction() => await dbContext.Database.BeginTransactionAsync();

    public void CommitTransaction() => dbContext.Database.CommitTransactionAsync();

    public void RollbackTransaction() => dbContext.Database.RollbackTransaction();

    public async Task<bool> SaveChangesAsync(CancellationToken cancellationToken = default)
    {
        return await dbContext.SaveChangesAsync(cancellationToken) > 0;
    }
    public async Task<IEnumerable<T>> GetAllAsync() => await _dbSet.ToListAsync();
    public async Task<T?> GetByIdAsync(Guid? id) => await _dbSet.FindAsync(id);
}