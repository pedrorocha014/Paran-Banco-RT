using FluentResults;

namespace Core.Interfaces;

public interface IUnitOfWork
{
    Task<Result> SaveChangesAsync(CancellationToken cancellationToken = default);
    Task<Result<IDatabaseTransaction>> BeginTransactionAsync(CancellationToken cancellationToken = default);
}

public interface IDatabaseTransaction : IAsyncDisposable
{
    Task<Result> CommitAsync(CancellationToken cancellationToken = default);
    Task<Result> RollbackAsync(CancellationToken cancellationToken = default);
}

