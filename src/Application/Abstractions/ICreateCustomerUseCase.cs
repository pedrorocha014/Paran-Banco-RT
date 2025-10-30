namespace Application.Abstractions;

public interface ICreateCustomerUseCase
{
    Task<Guid> ExecuteAsync(string name, CancellationToken cancellationToken = default);
}


