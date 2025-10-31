using FluentResults;

namespace Application.Abstractions;

public interface ICreateCustomerUseCase
{
    Task<Result<Guid>> ExecuteAsync(string name, CancellationToken cancellationToken = default);
}


