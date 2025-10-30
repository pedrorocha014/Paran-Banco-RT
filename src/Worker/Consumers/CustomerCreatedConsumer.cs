using Core.Messaging.Contracts;
using MassTransit;

namespace Worker.Consumers;

public class CustomerCreatedConsumer : IConsumer<CustomerCreated>
{
    public async Task Consume(ConsumeContext<CustomerCreated> context)
    {
        await Task.CompletedTask;
    }
}


