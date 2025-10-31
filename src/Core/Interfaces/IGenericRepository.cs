namespace Core.Interfaces;

public interface IGenericRepository<T> where T : class
{
    Task AddAsync(T entity, CancellationToken cancellationToken = default);
    Task<int> SaveChangesAsync(CancellationToken cancellationToken = default);
    void Update(T entity);
}


