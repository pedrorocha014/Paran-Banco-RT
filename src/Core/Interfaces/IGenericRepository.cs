using FluentResults;

namespace Core.Interfaces;

public interface IGenericRepository<T> where T : class
{
    Task<Result> AddAsync(T entity, CancellationToken cancellationToken = default);
    Task<Result> AddRangeAsync(IEnumerable<T> entities, CancellationToken cancellationToken = default);
    Task<Result> SaveChangesAsync(CancellationToken cancellationToken = default);
    Result Update(T entity);
    Task<Result<T>> GetByIdAsync(Guid id, CancellationToken cancellationToken = default);
}


