using Application.Abstractions;
using Core.CustomerAggregate;
using Core.Interfaces;
using MassTransit;
using Core.Messaging;
using Core.Messaging.Contracts;

namespace Application.UseCases.Customers;

public class CreateCustomerUseCase(IGenericRepository<Customer> repository, ISendEndpointProvider sendEndpointProvider) : ICreateCustomerUseCase
{
    public async Task<Guid> ExecuteAsync(string name, CancellationToken cancellationToken = default)
    {
        var now = DateTime.UtcNow;
        var customer = new Customer
        {
            Id = Guid.NewGuid(),
            Name = name,
            CreatedAt = now,
            UpdatedAt = now
        };

        await repository.AddAsync(customer, cancellationToken);
        var rows = await repository.SaveChangesAsync(cancellationToken);
        if (rows > 0)
        {
            var endpoint = await sendEndpointProvider.GetSendEndpoint(new Uri($"queue:{MessagingConstants.CustomerCreatedQueue}"));
            await endpoint.Send(new CustomerCreated(customer.Id, customer.Name, customer.CreatedAt), cancellationToken);
        }
        return customer.Id;
    }
}


