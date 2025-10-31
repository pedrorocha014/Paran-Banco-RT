using Application.Abstractions;
using Core.CustomerAggregate;
using Core.Interfaces;
using FluentResults;
using MassTransit;
using Core.Messaging;
using Core.Messaging.Contracts;

namespace Application.UseCases.Customers;

public class CreateCustomerUseCase(IGenericRepository<Customer> repository, ISendEndpointProvider sendEndpointProvider) : ICreateCustomerUseCase
{
    public async Task<Result<Guid>> ExecuteAsync(string name, CancellationToken cancellationToken = default)
    {
        var now = DateTime.UtcNow;
        var customer = new Customer
        {
            Id = Guid.NewGuid(),
            Name = name,
            CreatedAt = now,
            UpdatedAt = now
        };

        var addResult = await repository.AddAsync(customer, cancellationToken);
        if (addResult.IsFailed)
        {
            return Result.Fail(addResult.Errors);
        }

        var saveResult = await repository.SaveChangesAsync(cancellationToken);
        if (saveResult.IsFailed)
        {
            return Result.Fail(saveResult.Errors);
        }

        var endpoint = await sendEndpointProvider.GetSendEndpoint(new Uri($"queue:{MessagingConstants.CustomerCreatedQueue}"));
        await endpoint.Send(new CustomerCreated(customer.Id, customer.Name, customer.CreatedAt), cancellationToken);

        return Result.Ok(customer.Id);
    }
}


