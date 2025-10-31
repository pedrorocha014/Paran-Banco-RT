using Core.Interfaces;
using FluentResults;
using Infrastructure.Data;
using Microsoft.EntityFrameworkCore.Storage;

namespace Infrastructure.UnitOfWork;

public class UnitOfWork : IUnitOfWork
{
    private readonly ApplicationDbContext _dbContext;

    public UnitOfWork(ApplicationDbContext dbContext)
    {
        _dbContext = dbContext;
    }

    public async Task<Result> SaveChangesAsync(CancellationToken cancellationToken = default)
    {
        try
        {
            await _dbContext.SaveChangesAsync(cancellationToken);
            return Result.Ok();
        }
        catch (Exception ex)
        {
            return Result.Fail(ex.Message);
        }
    }

    public async Task<Result<IDatabaseTransaction>> BeginTransactionAsync(CancellationToken cancellationToken = default)
    {
        try
        {
            var transaction = await _dbContext.Database.BeginTransactionAsync(cancellationToken);
            return Result.Ok<IDatabaseTransaction>(new DatabaseTransaction(transaction));
        }
        catch (Exception ex)
        {
            return Result.Fail<IDatabaseTransaction>(ex.Message);
        }
    }
}

public class DatabaseTransaction : IDatabaseTransaction
{
    private readonly IDbContextTransaction _transaction;

    public DatabaseTransaction(IDbContextTransaction transaction)
    {
        _transaction = transaction;
    }

    public async Task<Result> CommitAsync(CancellationToken cancellationToken = default)
    {
        try
        {
            await _transaction.CommitAsync(cancellationToken);
            return Result.Ok();
        }
        catch (Exception ex)
        {
            return Result.Fail(ex.Message);
        }
    }

    public async Task<Result> RollbackAsync(CancellationToken cancellationToken = default)
    {
        try
        {
            await _transaction.RollbackAsync(cancellationToken);
            return Result.Ok();
        }
        catch (Exception ex)
        {
            return Result.Fail(ex.Message);
        }
    }

    public async ValueTask DisposeAsync()
    {
        await _transaction.DisposeAsync();
    }
}

