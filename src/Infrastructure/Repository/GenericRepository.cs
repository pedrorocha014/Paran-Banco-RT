using Core.Interfaces;
using FluentResults;

namespace Infrastructure.Repository;

public class GenericRepository<T> : IGenericRepository<T> where T : class
{
    private readonly Data.ApplicationDbContext _dbContext;

    public GenericRepository(Data.ApplicationDbContext dbContext)
    {
        _dbContext = dbContext;
    }

    public async Task<Result> AddAsync(T entity, CancellationToken cancellationToken = default)
    {
        try
        {
            await _dbContext.Set<T>().AddAsync(entity, cancellationToken);
            return Result.Ok();
        }
        catch (Exception ex)
        {
            return Result.Fail(ex.Message);
        }
    }

    public async Task<Result> AddRangeAsync(IEnumerable<T> entities, CancellationToken cancellationToken = default)
    {
        try
        {
            await _dbContext.Set<T>().AddRangeAsync(entities, cancellationToken);
            return Result.Ok();
        }
        catch (Exception ex)
        {
            return Result.Fail(ex.Message);
        }
    }

    public async Task<Result> SaveChangesAsync(CancellationToken cancellationToken = default)
    {
        try
        {
            var rowsAffected = await _dbContext.SaveChangesAsync(cancellationToken);
            return Result.Ok();
        }
        catch (Exception ex)
        {
            return Result.Fail(ex.Message);
        }
    }

    public Result Update(T entity)
    {
        try
        {
            _dbContext.Set<T>().Update(entity);
            return Result.Ok();
        }
        catch (Exception ex)
        {
            return Result.Fail(ex.Message);
        }
    }

    public async Task<Result<T>> GetByIdAsync(Guid id, CancellationToken cancellationToken = default)
    {
        try
        {
            var entity = await _dbContext.Set<T>().FindAsync([id], cancellationToken);
            if (entity == null)
            {
                return Result.Fail<T>($"Entity with id {id} not found.");
            }
            return Result.Ok(entity);
        }
        catch (Exception ex)
        {
            return Result.Fail<T>(ex.Message);
        }
    }
}


